using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using BazeSec.Models;
using System.Collections.Generic;
using System.IO;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔐 Require JWT for all actions
    public class LostFoundController : ControllerBase
    {
        private readonly string _conn;

        public LostFoundController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection");
        }

        // ============================
        // GET ALL or FILTERED ITEMS
        // ============================
        [HttpGet]
        public IActionResult GetItems([FromQuery] string? status = null, [FromQuery] string? q = null)
        {
            List<LostFoundItem> items = new();
            using var connection = new MySqlConnection(_conn);
            connection.Open();

            string query = "SELECT * FROM lost_found_items WHERE 1=1";

            if (!string.IsNullOrEmpty(status))
                query += " AND status = @status";

            if (!string.IsNullOrEmpty(q))
                query += " AND (title LIKE @q OR description LIKE @q)";

            using var cmd = new MySqlCommand(query, connection);

            if (!string.IsNullOrEmpty(status))
                cmd.Parameters.AddWithValue("@status", status);

            if (!string.IsNullOrEmpty(q))
                cmd.Parameters.AddWithValue("@q", "%" + q + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new LostFoundItem
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Description = reader.GetString("description"),
                    Category = reader["category"]?.ToString(),
                    ReporterContact = reader["reporter_contact"]?.ToString(),
                    Images = JsonConvert.DeserializeObject<List<string>>(reader["images"].ToString() ?? "[]"),
                    Status = reader.GetString("status"),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }

            return Ok(items);
        }

        // ===============================
        // REPORT NEW ITEM (WITH IMAGES)
        // ===============================
        [HttpPost("report")]
        public IActionResult ReportItem(
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] string category,
            [FromForm] string reporterContact,
            [FromForm] List<IFormFile> images)
        {
            List<string> savedImages = new();

            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadPath);

            foreach (var image in images)
            {
                string fileName = $"{Guid.NewGuid()}_{image.FileName}";
                string fullPath = Path.Combine(uploadPath, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                image.CopyTo(stream);

                savedImages.Add($"/uploads/{fileName}");
            }

            using var connection = new MySqlConnection(_conn);
            connection.Open();

            string query = @"INSERT INTO lost_found_items 
                (title, description, category, reporter_contact, images, status) 
                VALUES (@title, @description, @category, @reporterContact, @images, 'Reported')";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@category", category);
            cmd.Parameters.AddWithValue("@reporterContact", reporterContact);
            cmd.Parameters.AddWithValue("@images", JsonConvert.SerializeObject(savedImages));

            cmd.ExecuteNonQuery();

            return Ok(new { message = "Item reported successfully" });
        }

        // ============================
        // CLAIM ITEM (Security/Admin)
        // ============================
        [HttpPatch("{id}/claim")]
        [Authorize(Roles = "Admin,Security")] // 👮‍♂️ Students cannot claim items
        public IActionResult MarkAsClaimed(int id)
        {
            return UpdateStatus(id, "Claimed");
        }

        // =============================
        // RETURN ITEM (Security/Admin)
        // =============================
        [HttpPatch("{id}/return")]
        [Authorize(Roles = "Admin,Security")] // 👮‍♂️ Only Security/Admin
        public IActionResult MarkAsReturned(int id)
        {
            return UpdateStatus(id, "Returned");
        }

        // DELETE /api/lostfound/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteItem(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_conn);
                connection.Open();

                // Get item (we need to delete images too)
                string selectQuery = "SELECT images FROM lost_found_items WHERE id = @id";
                using var selectCmd = new MySqlCommand(selectQuery, connection);
                selectCmd.Parameters.AddWithValue("@id", id);

                var result = selectCmd.ExecuteScalar();
                if (result == null)
                    return NotFound(new { message = "Item not found" });

                var images = JsonConvert.DeserializeObject<List<string>>(result.ToString());

                // Delete from DB
                string deleteQuery = "DELETE FROM lost_found_items WHERE id = @id";
                using var deleteCmd = new MySqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.AddWithValue("@id", id);
                deleteCmd.ExecuteNonQuery();

                // Delete images from disk
                foreach (var img in images)
                {
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                return Ok(new { message = "Item deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // ============================
        // REUSABLE STATUS HELPER
        // ============================
        private IActionResult UpdateStatus(int id, string newStatus)
        {
            try
            {
                using var connection = new MySqlConnection(_conn);
                connection.Open();

                string query = "UPDATE lost_found_items SET status = @status WHERE id = @id";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", newStatus);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    return NotFound(new { message = "Item not found" });

                return Ok(new { message = $"Item marked as {newStatus}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
