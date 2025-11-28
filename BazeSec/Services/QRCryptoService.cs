using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BazeSec.Services
{
    public class QrCryptoService
    {
        private readonly string _secret;

        public QrCryptoService(IConfiguration config)
        {
            _secret = config["QR:SecretKey"]
                ?? throw new Exception("QR:SecretKey missing in appsettings.json");
        }

        private string Sign(string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secret);
            var msgBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(msgBytes);

            return Convert.ToBase64String(hash);
        }

        public string GenerateToken(string location)
        {
            var payload = new
            {
                location,
                ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nonce = Guid.NewGuid().ToString("N")
            };

            string json = JsonSerializer.Serialize(payload);

            string signature = Sign(json);

            string combined = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                            + "." +
                              Convert.ToBase64String(Encoding.UTF8.GetBytes(signature));

            return combined;
        }

        public bool TryValidate(string token, out string location)
        {
            location = null;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 2) return false;

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                var sig = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));

                // Verify signature
                var expectedSig = Sign(json);
                if (expectedSig != sig) return false;

                // Deserialize and extract location
                var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                location = payload["location"].ToString();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
