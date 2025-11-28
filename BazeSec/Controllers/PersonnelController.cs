using Microsoft.AspNetCore.Mvc;
using BazeSec.Services;
using BazeSec.DTOs;
using BazeSec.Models;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonnelController : ControllerBase
    {
        private readonly PersonnelService _service;

        public PersonnelController(PersonnelService service)
        {
            _service = service;
        }

        // ======================================================
        // GET ALL + SEARCH (uses SQL-level filtering in service)
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? q,
            [FromQuery] string? status)
        {
            var people = await _service.SearchAsync(q, status);

            // Group by guardpost safely
            var grouped = people
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Guardpost) ? "Unassigned" : p.Guardpost)
                .ToDictionary(g => g.Key, g => g.ToList());

            return Ok(new
            {
                message = "Personnel retrieved successfully",
                data = grouped
            });
        }

        // ======================================================
        // GET BY ID
        // ======================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var person = await _service.GetByIdAsync(id);
            if (person == null)
                return NotFound(new { message = "Personnel not found" });

            return Ok(person);
        }

        // ======================================================
        // CREATE
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PersonnelCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var person = await _service.CreateAsync(dto);

            return Ok(new
            {
                message = "Personnel created successfully",
                data = person
            });
        }

        // ======================================================
        // UPDATE
        // ======================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PersonnelUpdateDTO dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = "Personnel not found" });

            return Ok(new
            {
                message = "Personnel updated successfully",
                data = updated
            });
        }

        // ======================================================
        // DELETE
        // ======================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Personnel not found" });

            return Ok(new { message = "Personnel deleted successfully" });
        }
    }
}
