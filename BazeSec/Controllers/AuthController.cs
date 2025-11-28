using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _conn;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _conn = config.GetConnectionString("DefaultConnection");
        }

        public class LoginRequest
        {
            public string Identifier { get; set; }   // Email or Student/Staff ID
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            Console.WriteLine("\n------ LOGIN DEBUG START ------");
            Console.WriteLine("Raw identifier received: " + (request?.Identifier ?? "<null>"));
            Console.WriteLine("Raw password received: " + (request?.Password != null ? "<<<RECEIVED>>>" : "<null>"));

            if (request == null || string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
            {
                Console.WriteLine("Request binding failed or missing fields.");
                Console.WriteLine("------ LOGIN DEBUG END ------\n");
                return BadRequest(new { message = "Identifier and password are required." });
            }

            // Trim identifier to avoid accidental spaces
            var identifier = request.Identifier.Trim();
            var password = request.Password;

            Console.WriteLine("Trimmed identifier: '" + identifier + "'");

            try
            {
                using var conn = new MySqlConnection(_conn);
                conn.Open();

                string sql = @"
                    SELECT id, username, email, password_hash, role,
                           first_name, last_name, full_name
                    FROM users
                    WHERE email = @identifier OR username = @identifier
                    LIMIT 1";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@identifier", identifier);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    Console.WriteLine("No user row returned for identifier: '" + identifier + "'");
                    Console.WriteLine("------ LOGIN DEBUG END ------\n");
                    return Unauthorized(new { message = "Invalid ID/Email or Password" });
                }

                // Safely read columns (handle possible DBNull)
                int id = reader["id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["id"]);
                string username = reader["username"] == DBNull.Value ? null : reader["username"].ToString();
                string email = reader["email"] == DBNull.Value ? null : reader["email"].ToString();
                string role = reader["role"] == DBNull.Value ? "Student" : reader["role"].ToString();
                string hash = reader["password_hash"] == DBNull.Value ? null : reader["password_hash"].ToString();

                string? firstName = reader["first_name"] == DBNull.Value ? null : reader["first_name"].ToString();
                string? lastName = reader["last_name"] == DBNull.Value ? null : reader["last_name"].ToString();
                string? dbFullName = reader["full_name"] == DBNull.Value ? null : reader["full_name"].ToString();

                // Building a nice display name (full_name > first+last > username > email)
                string fullName = !string.IsNullOrWhiteSpace(dbFullName)
                    ? dbFullName
                    : $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = !string.IsNullOrWhiteSpace(username) ? username : email;

                Console.WriteLine($"DB returned -> id: {id}, username: '{username}', email: '{email}', role: '{role}'");
                Console.WriteLine("password_hash (raw): '" + (hash ?? "<null>") + "'");
                Console.WriteLine("password_hash length: " + (hash?.Length.ToString() ?? "null"));

                if (string.IsNullOrEmpty(hash))
                {
                    Console.WriteLine("Password hash is empty or null in DB for this user.");
                    Console.WriteLine("------ LOGIN DEBUG END ------\n");
                    return Unauthorized(new { message = "Invalid ID/Email or Password" });
                }

                bool valid = false;
                try
                {
                    valid = BCrypt.Net.BCrypt.Verify(password, hash);
                }
                catch (Exception exVerify)
                {
                    Console.WriteLine("BCrypt.Verify threw exception: " + exVerify.Message);
                    Console.WriteLine("------ LOGIN DEBUG END ------\n");
                    return StatusCode(500, new { message = "Server error during password verification", error = exVerify.Message });
                }

                Console.WriteLine("BCrypt.Verify returned: " + valid);

                if (!valid)
                {
                    Console.WriteLine("Password verification failed.");
                    Console.WriteLine("------ LOGIN DEBUG END ------\n");
                    return Unauthorized(new { message = "Invalid ID/Email or Password" });
                }

                // Generate token (same as before)
                var token = GenerateJwtToken(id, username, email, role, fullName);

                Console.WriteLine("Login successful for id: " + id);
                Console.WriteLine("------ LOGIN DEBUG END ------\n");

                return Ok(new
                {
                    message = "Login successful",
                    token,
                    user = new
                    {
                        id,
                        username,
                        email,
                        role,
                        first_name = firstName,
                        last_name = lastName,
                        full_name = fullName
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in Login: " + ex.Message);
                Console.WriteLine("------ LOGIN DEBUG END ------\n");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }


        // ============================
        // JWT TOKEN GENERATOR METHOD
        // ============================

        private string GenerateJwtToken(int id, string username, string email, string role, string fullName)
        {
            var claims = new[]
            {
                new Claim("id", id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, email ?? string.Empty),
                new Claim("role", role ?? "Student"),
                new Claim("full_name", fullName ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
