using System.Net;
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

        public Dictionary<string, string> BuildPaymentData(
            int transactionId, string buyerFirstName, string buyerLastName,
            string buyerEmail, decimal amount, string itemName)
        {
            var request = _httpContextAccessor.HttpContext!.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            var data = new Dictionary<string, string>
            {
                ["merchant_id"] = MerchantId,
                ["merchant_key"] = MerchantKey,
                ["return_url"] = $"{baseUrl}/Transactions/PaymentReturn/{transactionId}",
                ["cancel_url"] = $"{baseUrl}/Transactions/PaymentCancel/{transactionId}",
                ["notify_url"] = $"{baseUrl}/Transactions/ItnCallback",
                ["name_first"] = buyerFirstName,
                ["name_last"] = buyerLastName,
                ["email_address"] = buyerEmail,
                ["m_payment_id"] = transactionId.ToString(),
                ["amount"] = amount.ToString("F2"),
                ["item_name"] = itemName.Length > 100 ? itemName[..100] : itemName,
            };

            data["signature"] = GenerateSignature(data);
            _logger.LogDebug("PayFast signature generated for TXN {Id}", transactionId);
            return data;
        }

        public bool VerifyItn(IFormCollection form)
        {
            var sb = new StringBuilder();
            foreach (var key in form.Keys.Where(k => k != "signature"))
            {
                var val = form[key].ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    if (sb.Length > 0) sb.Append('&');
                    sb.Append($"{PhpUrlEncode(key)}={PhpUrlEncode(val)}");
                }
            }
            sb.Append($"&passphrase={PhpUrlEncode(Passphrase)}");

            var expected = ComputeMd5(sb.ToString());
            var received = form["signature"].ToString();
            _logger.LogDebug("ITN — expected: {E}  received: {R}", expected, received);
            return string.Equals(expected, received, StringComparison.OrdinalIgnoreCase);
        }

        public string GetSandboxUrl() => SandboxUrl;

        private string GenerateSignature(Dictionary<string, string> data)
        {
            var sb = new StringBuilder();
            foreach (var kv in data.Where(kv => kv.Key != "signature" && !string.IsNullOrEmpty(kv.Value)))
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append($"{PhpUrlEncode(kv.Key)}={PhpUrlEncode(kv.Value)}");
            }
            sb.Append($"&passphrase={PhpUrlEncode(Passphrase)}");
            return ComputeMd5(sb.ToString());
        }

        private static string PhpUrlEncode(string value) =>
            WebUtility.UrlEncode(value) ?? string.Empty;

        private static string ComputeMd5(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}