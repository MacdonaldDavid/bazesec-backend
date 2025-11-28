using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using BazeSec.Services;
using BazeSec.DTOs;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeyController : ControllerBase
    {
        private readonly KeyService _service;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly QrCryptoService _crypto;

        public KeyController(KeyService service, IConfiguration config, IWebHostEnvironment env, QrCryptoService crypto)
        {
            _service = service;
            _config = config;
            _env = env;
            _crypto = crypto;
        }
        private (int userId, string name, string role) GetCurrentUser()
        {
            var idClaim = User.FindFirst("id")?.Value;
            int.TryParse(idClaim, out int uid);

            var username = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var role = User.FindFirst("role")?.Value ?? "Unknown";

            return (uid, username ?? email ?? "Unknown", role);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllGroupedAsync();
            return Ok(new { message = "Keys retrieved successfully", data });
        }

        // Fetch Keys
        [HttpGet("location/{location}")]
        public async Task<IActionResult> GetByLocation(string location)
        {
            var keys = await _service.GetByLocationAsync(location);

            if (keys == null || keys.Count == 0)
                return NotFound(new { message = "No keys found for this location." });

            return Ok(new
            {
                message = "Keys retrieved successfully",
                data = keys
            });
        }

        // GET a single key by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var key = await _service.GetByIdAsync(id);

            if (key == null)
                return NotFound(new { message = "Key not found" });

            return Ok(new { data = key });
        }


        // CREATE Key
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] KeyCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var key = await _service.CreateAsync(dto);
            if (key == null)
                return BadRequest(new { message = "Invalid location" });

            return Ok(new
            {
                message = "Key created successfully",
                data = key
            });
        }

        // UPDATE Key
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] KeyUpdateDTO dto)
        {
            var updated = await _service.UpdateAsync(id, dto);

            if (updated == null)
                return NotFound(new { message = "Key not found or invalid location" });

            return Ok(new
            {
                message = "Key updated successfully",
                data = updated
            });
        }

        [HttpPost("{id}/checkout")]
        public async Task<IActionResult> Checkout(int id, [FromBody] QrScanDTO dto)
        {
            if (!_crypto.TryValidate(dto.Payload, out string scannedLocation))
                return BadRequest(new { message = "Invalid or tampered QR code." });

            var (userId, name, role) = GetCurrentUser();

            try
            {
                var key = await _service.CheckoutAsync(
                    id,
                    scannedLocation,
                    userId,
                    name,
                    role
                );

                if (key == null)
                    return NotFound(new { message = "Key not found" });

                return Ok(new
                {
                    message = "Key successfully checked out",
                    data = key
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> Checkin(int id, [FromBody] QrScanDTO dto)
        {
            if (!_crypto.TryValidate(dto.Payload, out string scannedLocation))
                return BadRequest(new { message = "Invalid or tampered QR code." });

            var (userId, name, role) = GetCurrentUser();

            try
            {
                var key = await _service.CheckinAsync(
                    id,
                    scannedLocation,
                    userId,
                    name,
                    role
                );

                if (key == null)
                    return NotFound(new { message = "Key not found" });

                return Ok(new
                {
                    message = "Key successfully returned",
                    data = key
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE Key
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);

            if (!success)
                return NotFound(new { message = "Key not found" });

            return Ok(new { message = "Key deleted successfully" });
        }

    }
}
