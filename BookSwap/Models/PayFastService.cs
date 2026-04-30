using System.Security.Cryptography;
using System.Text;

namespace BookSwap.Services
{
    public class PayFastService
    {
        private const string MerchantId = "10048004";
        private const string MerchantKey = "20612d2htvq35";
        private const string Passphrase = "SomSOM-26BAR";
        private const string SandboxUrl = "https://sandbox.payfast.co.za/eng/process";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PayFastService> _logger;

        public PayFastService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<PayFastService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ── Build ordered payment data dict ────────────────────────────────
        public Dictionary<string, string> BuildPaymentData(
            int transactionId,
            string buyerFirstName,
            string buyerLastName,
            string buyerEmail,
            decimal amount,
            string itemName)
        {
            var request = _httpContextAccessor.HttpContext!.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // Field ORDER matches PayFast docs exactly — do not reorder
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
                ["amount"] = amount.ToString("0.00"),  // must be 2 decimal places
                ["item_name"] = (itemName.Length > 100 ? itemName[..100] : itemName).Trim(),
            };

            data["signature"] = GenerateSignature(data);

            // Log every field so you can verify in Output window
            _logger.LogDebug("=== PayFast payment data for TXN {Id} ===", transactionId);
            foreach (var kv in data)
                _logger.LogDebug("  {Key} = {Value}", kv.Key, kv.Value);

            return data;
        }

        // ── Verify ITN callback from PayFast ───────────────────────────────
        public bool VerifyItn(IFormCollection form)
        {
            var parts = new List<string>();
            foreach (var key in form.Keys.Where(k => k != "signature"))
            {
                var val = form[key].ToString().Trim();
                if (!string.IsNullOrEmpty(val))
                    parts.Add($"{key}={PhpUrlEncode(val)}");
            }

            var paramString = string.Join("&", parts)
                            + $"&passphrase={PhpUrlEncode(Passphrase.Trim())}";

            var expected = Md5(paramString);
            var received = form["signature"].ToString();

            _logger.LogDebug("ITN verify — expected: {E}  received: {R}", expected, received);
            return string.Equals(expected, received, StringComparison.OrdinalIgnoreCase);
        }

        public string GetSandboxUrl() => SandboxUrl;

        // ── Signature — mirrors PayFast PHP SDK exactly ────────────────────
        //
        //  PHP SDK:
        //    foreach ($data as $key => $val) {
        //        if ($val !== '') {
        //            $pfOutput .= $key .'='. urlencode(trim($val)) .'&';
        //        }
        //    }
        //    $getString = substr($pfOutput, 0, -1);          // remove trailing &
        //    $getString .= '&passphrase='. urlencode(trim($passPhrase));
        //    return md5($getString);
        //
        private string GenerateSignature(Dictionary<string, string> data)
        {
            var parts = data
                .Where(kv => kv.Key != "signature" &&
                             !string.IsNullOrEmpty(kv.Value?.Trim()))
                .Select(kv => $"{kv.Key}={PhpUrlEncode(kv.Value.Trim())}");

            var paramString = string.Join("&", parts)
                            + $"&passphrase={PhpUrlEncode(Passphrase.Trim())}";

            _logger.LogDebug("Signature input string: {S}", paramString);
            return Md5(paramString);
        }

        // ── PHP urlencode() equivalent ─────────────────────────────────────
        // PHP urlencode safe set: A-Z  a-z  0-9  -  _  .
        // Space → +
        // Everything else → %XX  (uppercase hex, per PHP)
        // NOTE: This differs from .NET's WebUtility.UrlEncode which leaves
        //       ! * ( ) ' unencoded — PHP DOES encode those.
        private static string PhpUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length * 2);
            foreach (char c in value)
            {
                if (c == ' ')
                {
                    sb.Append('+');
                }
                else if ((c >= 'A' && c <= 'Z') ||
                         (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9') ||
                         c == '-' || c == '_' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    // Encode as UTF-8 bytes, each byte as %XX
                    foreach (var b in Encoding.UTF8.GetBytes(c.ToString()))
                        sb.Append('%').Append(b.ToString("X2"));
                }
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