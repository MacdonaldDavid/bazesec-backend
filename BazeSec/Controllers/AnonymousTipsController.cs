using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BazeSec.Services;
using BazeSec.DTOs.AnonymousTips;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnonymousTipsController : ControllerBase
    {
        private readonly AnonymousTipService _service;

        public AnonymousTipsController(AnonymousTipService service)
        {
            _service = service;
        }

        // POST: api/AnonymousTips
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTip(CreateAnonymousTipDto dto)
        {
            var reporterId = int.Parse(User.FindFirst("id").Value);
            var reporterRole = User.FindFirst("role").Value;

            var tip = await _service.CreateTipAsync(dto, reporterId, reporterRole);

            return Ok(new { message = "Tip submitted successfully.", data = tip.Id });
        }

        // GET: api/AnonymousTips/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var tips = await _service.GetAllAsync();
            return Ok(new { data = tips });
        }

        // GET: api/AnonymousTips/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var tip = await _service.GetByIdAsync(id);
            if (tip == null)
                return NotFound(new { message = "Tip not found." });

            return Ok(new { data = tip });
        }

        // PATCH: api/AnonymousTips/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTipStatusDto dto)
        {
            var tip = await _service.GetByIdAsync(id);
            if (tip == null)
                return NotFound(new { message = "Tip not found." });

            var handlerId = int.Parse(User.FindFirst("id").Value);
            var handlerName = User.FindFirst("username").Value;

            await _service.UpdateStatusAsync(
                tip,
                dto.Status,
                dto.Priority,
                dto.InternalNotes,
                handlerId,
                handlerName
            );

            return Ok(new { message = "Tip updated successfully." });
        }
    }

    public class UpdateTipStatusDto
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public string InternalNotes { get; set; }
    }
}
