using System.Security.Cryptography;
using System.Text;

namespace LiteratureClub.Services
{
    public class PayFastService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PayFastService> _logger;

        // Read from appsettings.json
        private string MerchantId => _config["PayFast:MerchantId"] ?? "";
        private string MerchantKey => _config["PayFast:MerchantKey"] ?? "";
        private string Passphrase => _config["PayFast:Passphrase"] ?? "";
        private string SandboxUrl => _config["PayFast:SandboxUrl"]
                                   ?? "https://sandbox.payfast.co.za/eng/process";

        public PayFastService(
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PayFastService> logger)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public Dictionary<string, string> BuildPaymentData(
            int transactionId, string buyerFirstName, string buyerLastName,
            string buyerEmail, decimal amount, string itemName)
        {
            var request = _httpContextAccessor.HttpContext!.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // Field ORDER matches PayFast docs — do not reorder
            var data = new Dictionary<string, string>
            {
                ["merchant_id"] = MerchantId,
                ["merchant_key"] = MerchantKey,
                ["return_url"] = $"{baseUrl}/Transactions/PaymentReturn/{transactionId}",
                ["cancel_url"] = $"{baseUrl}/Transactions/PaymentCancel/{transactionId}",
                ["notify_url"] = $"{baseUrl}/Transactions/ItnCallback",
                ["name_first"] = buyerFirstName.Trim(),
                ["name_last"] = buyerLastName.Trim(),
                ["email_address"] = buyerEmail.Trim(),
                ["m_payment_id"] = transactionId.ToString(),
                ["amount"] = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                ["item_name"] = (itemName.Length > 100 ? itemName[..100] : itemName).Trim(),
            };

            data["signature"] = GenerateSignature(data);

            _logger.LogInformation("=== PayFast Fields ===");
            foreach (var kv in data)
                _logger.LogInformation("  {K} = {V}", kv.Key, kv.Value);

            return data;
        }

        // Expose the raw signature string for the diagnostic view
        public string GetSignatureString(Dictionary<string, string> data)
        {
            var parts = data
                .Where(kv => kv.Key != "signature" &&
                             !string.IsNullOrEmpty(kv.Value?.Trim()))
                .Select(kv => $"{kv.Key}={PhpUrlEncode(kv.Value.Trim())}");

            var s = string.Join("&", parts);
            if (!string.IsNullOrEmpty(Passphrase))
                s += $"&passphrase={PhpUrlEncode(Passphrase.Trim())}";
            return s;
        }

        public bool VerifyItn(IFormCollection form)
        {
            var parts = new List<string>();
            foreach (var key in form.Keys.Where(k => k != "signature"))
            {
                var val = form[key].ToString().Trim();
                if (!string.IsNullOrEmpty(val))
                    parts.Add($"{key}={PhpUrlEncode(val)}");
            }

            var paramString = string.Join("&", parts);
            if (!string.IsNullOrEmpty(Passphrase))
                paramString += $"&passphrase={PhpUrlEncode(Passphrase.Trim())}";

            var expected = Md5(paramString);
            var received = form["signature"].ToString();
            _logger.LogInformation("ITN — expected: {E}  received: {R}", expected, received);
            return string.Equals(expected, received, StringComparison.OrdinalIgnoreCase);
        }

        public string GetSandboxUrl() => SandboxUrl;

        private string GenerateSignature(Dictionary<string, string> data)
        {
            // IMPORTANT: only encode VALUES, not keys (matches PHP SDK exactly)
            var parts = data
                .Where(kv => kv.Key != "signature" &&
                             !string.IsNullOrEmpty(kv.Value?.Trim()))
                .Select(kv => $"{kv.Key}={PhpUrlEncode(kv.Value.Trim())}");

            var s = string.Join("&", parts);
            if (!string.IsNullOrEmpty(Passphrase))
                s += $"&passphrase={PhpUrlEncode(Passphrase.Trim())}";

            _logger.LogInformation("Signature input: {S}", s);
            return Md5(s);
        }

        // Replicates PHP urlencode() exactly:
        //   safe set: A-Z a-z 0-9 - _ .
        //   space → +
        //   all else → %XX  (WebUtility.UrlEncode leaves ! * ( ) ' unencoded;
        //                     PHP encodes them — this is the bug in your old code)
        private static string PhpUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length * 3);
            foreach (char c in value)
            {
                if (c == ' ')
                    sb.Append('+');
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9') ||
                         c == '-' || c == '_' || c == '.')
                    sb.Append(c);
                else
                    foreach (var b in Encoding.UTF8.GetBytes(c.ToString()))
                        sb.Append('%').Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        private static string Md5(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}