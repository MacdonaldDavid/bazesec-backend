using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Text;
using BazeSec.Models;
using BazeSec.Services;

namespace BazeSec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only Admin can access these endpoints
    public class QrCodeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly QrCryptoService _crypto;

        public QrCodeController(IConfiguration config, IWebHostEnvironment env, QrCryptoService crypto)
        {
            _config = config;
            _env = env;
            _crypto = crypto;
        }

        // Keep locations in sync with Key management
        private static readonly List<string> AllowedLocations = new()
        {
            "FrontGate",
            "Block A",
            "Block B",
            "Block C",
            "Block D",
            "Block E",
            "Block F"
        };

        private bool IsValidLocation(string location) =>
            AllowedLocations.Contains(location, StringComparer.OrdinalIgnoreCase);

        private string GetQrFolderPath()
        {
            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var qrPath = Path.Combine(root, "qr");
            if (!Directory.Exists(qrPath))
                Directory.CreateDirectory(qrPath);
            return qrPath;
        }

        private string GetQrFileName(string location)
        {
            // e.g. "Block A" -> "Block A.png" (you can change to replace spaces with underscores if you like)
            return $"{location}.png";
        }

        private string GetPublicQrUrl(string fileName)
        {
            // e.g. http://localhost:5108/qr/Block%20A.png
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/qr/{Uri.EscapeDataString(fileName)}";
        }

       

        /// <summary>
        /// Generate or regenerate a QR code for a specific location.
        /// </summary>
        [HttpPost("generate/{location}")]
        public IActionResult GenerateForLocation(string location)
        {
            if (!IsValidLocation(location))
                return BadRequest(new { message = "Invalid location." });

            // Read frontend base URL
            var frontendBase = _config["Frontend:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrEmpty(frontendBase))
                return StatusCode(500, new { message = "Frontend:BaseUrl is not configured." });

            var encodedLocation = Uri.EscapeDataString(location);

            var token = _crypto.GenerateToken(location);
            var qrPayload = $"{frontendBase}/keys/scan?payload={Uri.EscapeDataString(token)}";


            // Generate QR using QRCoder
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(data);
            var pngBytes = pngQr.GetGraphic(20);

            // Save QR file
            var qrFolder = GetQrFolderPath();
            var fileName = GetQrFileName(location);
            var filePath = Path.Combine(qrFolder, fileName);
            System.IO.File.WriteAllBytes(filePath, pngBytes);

            // Save timestamp file
            var timestampFile = Path.Combine(qrFolder, $"{location}.txt");
            System.IO.File.WriteAllText(timestampFile, DateTime.UtcNow.ToString("o"));

            // Return result
            var publicUrl = GetPublicQrUrl(fileName);

            return Ok(new
            {
                message = "QR code generated successfully.",
                location,
                qrUrl = publicUrl,
                lastGenerated = DateTime.UtcNow.ToString("o"),
                payload = qrPayload
            });
        }

        /// <summary>
        /// List all locations and whether a QR exists.
        /// </summary>
        [HttpGet]
        public IActionResult GetAll()
        {
            var qrFolder = GetQrFolderPath();

            var items = AllowedLocations.Select(loc =>
            {
                var fileName = GetQrFileName(loc);
                var filePath = Path.Combine(qrFolder, fileName);
                var exists = System.IO.File.Exists(filePath);

                // Timestamp file
                var timestampFile = Path.Combine(qrFolder, $"{loc}.txt");
                string lastGenerated = null;

                if (System.IO.File.Exists(timestampFile))
                    lastGenerated = System.IO.File.ReadAllText(timestampFile);

                var url = exists ? GetPublicQrUrl(fileName) : null;

                return new
                {
                    location = loc,
                    exists,
                    qrUrl = url,
                    lastGenerated
                };
            });

            return Ok(new
            {
                message = "QR code info retrieved successfully.",
                data = items
            });
        }


        /// <summary>
        /// Get a single location's QR info.
        /// </summary>
        [HttpGet("{location}")]
        public IActionResult GetForLocation(string location)
        {
            if (!IsValidLocation(location))
                return BadRequest(new { message = "Invalid location." });

            var qrFolder = GetQrFolderPath();
            var fileName = GetQrFileName(location);
            var filePath = Path.Combine(qrFolder, fileName);
            var exists = System.IO.File.Exists(filePath);

            // Timestamp file
            var timestampFile = Path.Combine(qrFolder, $"{location}.txt");
            string lastGenerated = null;
            if (System.IO.File.Exists(timestampFile))
                lastGenerated = System.IO.File.ReadAllText(timestampFile);

            var url = exists ? GetPublicQrUrl(fileName) : null;

            return Ok(new
            {
                location,
                exists,
                qrUrl = url,
                lastGenerated
            });
        }
    }
}
