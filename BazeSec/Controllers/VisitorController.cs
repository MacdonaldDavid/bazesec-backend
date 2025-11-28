using System;
using System.Linq;
using System.Threading.Tasks;
using BazeSec.Data;
using BazeSec.DTOs.Visitors;
using BazeSec.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitorController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Random _rng = new Random();

        public VisitorController(ApplicationDbContext db)
        {
            _db = db;
        }

        // --------- PUBLIC: REQUEST VISIT (PENDING APPROVAL) ---------
        [HttpPost("request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestVisit([FromBody] VisitorCheckInRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data", errors = ModelState });

            // Basic validation
            if (string.IsNullOrWhiteSpace(req.FullName) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Purpose) ||
                string.IsNullOrWhiteSpace(req.Reason))
            {
                return BadRequest(new { message = "Full name, email, purpose, and reason are required." });
            }

            if (req.Reason.Length < 10)
            {
                return BadRequest(new { message = "Reason should be at least 10 characters long." });
            }

            var purpose = req.Purpose?.Trim();

            // Extra validation based on purpose
            if (purpose.Equals("VisitStaff", StringComparison.OrdinalIgnoreCase) ||
                purpose.Equals("VisitStudent", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(req.RecipientPhone))
                    return BadRequest(new { message = "Staff/Student phone number is required." });

                if (!IsElevenDigitNumber(req.RecipientPhone))
                    return BadRequest(new { message = "Staff/Student phone number must be 11 digits." });
            }

            if (purpose.Equals("DeliverParcel", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(req.RecipientName) && string.IsNullOrWhiteSpace(req.RecipientPhone))
                {
                    return BadRequest(new { message = "Recipient name or phone is required for parcel delivery." });
                }
            }

            // Duplicate prevention: same email can't have active visit
            var activeVisit = await _db.Visitors
                .FirstOrDefaultAsync(v =>
                    v.Email == req.Email &&
                    (v.Status == "PendingApproval" || v.Status == "CheckedIn"));

            if (activeVisit != null)
            {
                return BadRequest(new
                {
                    message = "You already have an active or pending visit. Please contact an officer if this is an error."
                });
            }

            var visitor = new Visitor
            {
                FullName = req.FullName.Trim(),
                Email = req.Email.Trim(),
                Phone = req.Phone?.Trim(),
                Purpose = purpose,
                Reason = req.Reason.Trim(),
                RecipientName = req.RecipientName?.Trim(),
                RecipientPhone = req.RecipientPhone?.Trim(),
                Department = req.Department?.Trim(),
                Status = "PendingApproval",
                CreatedAt = DateTime.UtcNow,
                VisitorCode = GenerateVisitorCode()
            };

            _db.Visitors.Add(visitor);
            await _db.SaveChangesAsync();

            var response = MapToResponse(visitor);

            return Ok(new { message = "Visit request submitted and pending approval.", data = response });
        }

        // --------- PUBLIC: GET VISITOR STATUS BY ID ---------
        [HttpGet("status/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus(int id)
        {
            var visitor = await _db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { message = "Visitor not found." });

            var response = MapToResponse(visitor);
            return Ok(new { data = response });
        }

        // (Optional) Get status by VisitorCode - nice for QR flows later
        [HttpGet("status/code/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatusByCode(string code)
        {
            var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.VisitorCode == code);
            if (visitor == null)
                return NotFound(new { message = "Visitor not found." });

            var response = MapToResponse(visitor);
            return Ok(new { data = response });
        }

        // --------- PUBLIC: CHECKOUT ---------
        [HttpPatch("checkout/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout(int id)
        {
            var visitor = await _db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { message = "Visitor not found." });

            if (visitor.Status != "CheckedIn")
            {
                return BadRequest(new { message = "Only checked-in visitors can be checked out." });
            }

            visitor.Status = "CheckedOut";
            visitor.CheckOutTime = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var response = MapToResponse(visitor);
            return Ok(new { message = "Checked out successfully.", data = response });
        }

        // --------- ADMIN: LIST VISITORS WITH FILTERS ---------
        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ListVisitors(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _db.Visitors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(v =>
                    v.FullName.ToLower().Contains(search) ||
                    v.Email.ToLower().Contains(search) ||
                    (v.Phone != null && v.Phone.ToLower().Contains(search)));
            }

            if (from.HasValue)
                query = query.Where(v => v.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(v => v.CreatedAt <= to.Value);

            query = query.OrderByDescending(v => v.CreatedAt);

            var visitors = await query.ToListAsync();
            var data = visitors.Select(MapToResponse).ToList();

            return Ok(new { data });
        }

        // --------- ADMIN: APPROVE VISIT ---------
        [HttpPatch("admin/{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveVisit(int id, [FromBody] VisitorApproveRequest req)
        {
            var visitor = await _db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { message = "Visitor not found." });

            if (visitor.Status != "PendingApproval")
            {
                return BadRequest(new { message = "Only pending visits can be approved." });
            }

            visitor.Status = "CheckedIn";
            visitor.ApprovalTime = DateTime.UtcNow;
            visitor.CheckInTime = DateTime.UtcNow;
            // You can store ApprovalNote somewhere later if you add a field

            await _db.SaveChangesAsync();

            var response = MapToResponse(visitor);
            return Ok(new { message = "Visitor approved and checked in.", data = response });
        }

        // --------- ADMIN: REJECT VISIT ---------
        [HttpPatch("admin/{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectVisit(int id, [FromBody] VisitorRejectRequest req)
        {
            var visitor = await _db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { message = "Visitor not found." });

            if (visitor.Status != "PendingApproval")
            {
                return BadRequest(new { message = "Only pending visits can be rejected." });
            }

            visitor.Status = "Rejected";
            visitor.RejectionTime = DateTime.UtcNow;
            visitor.RejectionComment = req?.RejectionComment;

            await _db.SaveChangesAsync();

            var response = MapToResponse(visitor);
            return Ok(new { message = "Visitor request rejected.", data = response });
        }

        // --------- HELPERS ---------
        private string GenerateVisitorCode()
        {
            // e.g. VIS-20251119-12345
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var rand = _rng.Next(10000, 99999);
            return $"VIS-{stamp}-{rand}";
        }

        private bool IsElevenDigitNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.Length != 11) return false;
            return input.All(char.IsDigit);
        }

        private VisitorResponse MapToResponse(Visitor v)
        {
            return new VisitorResponse
            {
                Id = v.Id,
                VisitorCode = v.VisitorCode,
                FullName = v.FullName,
                Email = v.Email,
                Phone = v.Phone,
                Purpose = v.Purpose,
                Reason = v.Reason,
                RecipientName = v.RecipientName,
                RecipientPhone = v.RecipientPhone,
                Department = v.Department,
                Status = v.Status,
                CreatedAt = v.CreatedAt,
                CheckInTime = v.CheckInTime,
                CheckOutTime = v.CheckOutTime,
                ApprovalTime = v.ApprovalTime,
                RejectionTime = v.RejectionTime,
                RejectionComment = v.RejectionComment
            };
        }
    }
}