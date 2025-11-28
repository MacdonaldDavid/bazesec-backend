using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using BazeSec.DTOs.Emergencies;
using BazeSec.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]  // => /api/Emergency
    public class EmergencyController : ControllerBase
    {
        private readonly EmergencyAlertService _service;

        public EmergencyController(EmergencyAlertService service)
        {
            _service = service;
        }

        // Helper to get current user info from JWT
        private (int userId, string name) GetCurrentUser()
        {
            var idClaim = User.FindFirst("id")?.Value;
            int.TryParse(idClaim, out int uid);

            var username = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

            return (uid, username ?? email ?? "Unknown");
        }

        // -------------------------------
        // ADMIN: CREATE EMERGENCY ALERT
        // POST /api/Emergency
        // -------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateEmergencyAlertDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (userId, name) = GetCurrentUser();

            var alert = await _service.CreateAsync(dto, userId, name);

            return Ok(new
            {
                message = "Emergency alert created.",
                data = alert
            });
        }

        // -------------------------------
        // ALL USERS: GET LATEST ACTIVE ALERT
        // GET /api/Emergency/latest
        // -------------------------------
        [HttpGet("latest")]
        [Authorize]   // any logged-in user
        public async Task<IActionResult> GetLatest()
        {
            var alert = await _service.GetLatestActiveAsync();

            // Frontend can treat null as "no active emergency"
            return Ok(new { data = alert });
        }

        // -------------------------------
        // ALL USERS: LIST ALL ALERTS
        // GET /api/Emergency
        // -------------------------------
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var alerts = await _service.GetAllAsync();
            return Ok(new { data = alerts });
        }

        // -------------------------------
        // ALL USERS: GET BY ID
        // GET /api/Emergency/{id}
        // -------------------------------
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var alert = await _service.GetByIdAsync(id);
            if (alert == null)
                return NotFound(new { message = "Emergency alert not found." });

            return Ok(new { data = alert });
        }

        // -------------------------------
        // ADMIN: RESOLVE ALERT
        // PATCH /api/Emergency/{id}/resolve
        // -------------------------------
        [HttpPatch("{id:int}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveEmergencyRequest request)
        {
            var alert = await _service.GetByIdAsync(id);
            if (alert == null)
                return NotFound(new { message = "Emergency alert not found." });

            var (adminId, adminName) = GetCurrentUser();

            await _service.ResolveAsync(alert, adminId, adminName, request?.ResolutionNote);

            return Ok(new { message = "Emergency alert marked as resolved." });
        }

        // DELETE /api/Emergency/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();

            return Ok(new { message = "Emergency deleted" });
        }

    }
}
