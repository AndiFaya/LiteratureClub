using BookSwap.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace BookSwap.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        private string SmtpHost => _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        private int SmtpPort => int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
        private string SenderEmail => _config["EmailSettings:SenderEmail"] ?? "somasharelitclub@gmail.com";
        private string SenderName => _config["EmailSettings:SenderName"] ?? "Literature Club";
        private string SmtpPassword => _config["EmailSettings:SmtpPassword"] ?? "Th3LitClub123";

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── 1. Registration verification link ──────────────────────────────
        public async Task<bool> SendEmailVerificationAsync(
            ApplicationUser user, string verificationLink)
        {
            var subject = "Verify your BookSwap email address";
            var body = $@"
<div style='font-family:sans-serif;max-width:520px;margin:auto'>
  <h2 style='color:#0d6efd'>📚 Welcome to BookSwap, {user.DisplayUsername}!</h2>
  <p>You're almost done. Click below to verify your email and activate your account.</p>
  <div style='text-align:center;margin:32px 0'>
    <a href='{verificationLink}'
       style='background:#0d6efd;color:#fff;padding:14px 28px;
              border-radius:6px;text-decoration:none;font-weight:600;font-size:16px'>
      Verify Email Address
    </a>
  </div>
  <p style='color:#6c757d;font-size:13px'>
    This link expires in 24 hours.<br/>
    If you didn't create a BookSwap account, you can safely ignore this email.
  </p>
  <hr style='border:none;border-top:1px solid #dee2e6'/>
  <p style='color:#adb5bd;font-size:12px;text-align:center'>
    BookSwap · Student Textbook Marketplace
  </p>
</div>";

            return await SendAsync(user.Email!, subject, body);
        }

        // ── 2. Verification code after payment confirmed ───────────────────
        public async Task<bool> SendVerificationCodeAsync(Transaction transaction)
        {
            var subject = $"BookSwap — Your pickup code for \"{transaction.Listing.Title}\"";
            var body = $@"
<div style='font-family:sans-serif;max-width:520px;margin:auto'>
  <h2 style='color:#0d6efd'>📚 Payment Confirmed!</h2>
  <p>Hi <strong>{transaction.Buyer.DisplayUsername}</strong>,</p>
  <p>
    Your payment of <strong>R {transaction.Amount:N2}</strong> for
    <strong>{transaction.Listing.Title}</strong> has been received.
  </p>
  <p>Show this code to the seller when you collect your book:</p>
  <div style='text-align:center;margin:32px 0'>
    <div style='display:inline-block;background:#f8f9fa;border:2px solid #0d6efd;
                border-radius:8px;padding:18px 40px'>
      <div style='color:#6c757d;font-size:13px;margin-bottom:8px'>Verification Code</div>
      <div style='font-size:40px;font-weight:700;letter-spacing:10px;color:#198754'>
        {transaction.VerificationCode}
      </div>
    </div>
  </div>
  <div style='background:#fff3cd;border-left:4px solid #ffc107;
              padding:12px 16px;border-radius:4px;margin-bottom:24px'>
    <strong>⚠ Important:</strong> Only show this code after you have the book in hand.
    The seller enters it to release your payment to them.
  </div>
  <p>Seller: <strong>{transaction.Seller.DisplayUsername}</strong></p>
  <hr style='border:none;border-top:1px solid #dee2e6'/>
  <p style='color:#adb5bd;font-size:12px;text-align:center'>BookSwap · Student Textbook Marketplace</p>
</div>";

            return await SendAsync(transaction.Buyer.Email!, subject, body);
        }

        // ── 3. Receipt emails to buyer and seller ──────────────────────────
        public async Task<bool> SendReceiptEmailAsync(Receipt receipt, Transaction transaction)
        {
            var buyerBody = $@"
<div style='font-family:sans-serif;max-width:520px;margin:auto'>
  <h2 style='color:#198754'>📚 Purchase Complete!</h2>
  <p>Hi <strong>{transaction.Buyer.DisplayUsername}</strong>, here is your receipt:</p>
  <div style='background:#f8f9fa;border-radius:8px;padding:24px;margin:24px 0'>
    <h3 style='margin-top:0;border-bottom:1px solid #dee2e6;padding-bottom:12px'>
      Receipt #RCP-{receipt.Id:D6}
    </h3>
    <table style='width:100%;font-size:14px;border-collapse:collapse'>
      <tr><td style='padding:6px 0;color:#6c757d'>Book</td>
          <td style='text-align:right;font-weight:600'>{receipt.TextbookTitle}</td></tr>
      <tr><td style='padding:6px 0;color:#6c757d'>Author</td>
          <td style='text-align:right'>{receipt.TextbookAuthor}</td></tr>
      {(string.IsNullOrEmpty(receipt.ISBN) ? "" :
        $"<tr><td style='padding:6px 0;color:#6c757d'>ISBN</td><td style='text-align:right'>{receipt.ISBN}</td></tr>")}
      <tr><td style='padding:6px 0;color:#6c757d'>Seller</td>
          <td style='text-align:right'>{receipt.SellerName}</td></tr>
      {(string.IsNullOrEmpty(receipt.PickupPointName) ? "" :
        $"<tr><td style='padding:6px 0;color:#6c757d'>Pickup point</td><td style='text-align:right'>{receipt.PickupPointName}</td></tr>")}
      <tr><td style='padding:6px 0;color:#6c757d'>Date</td>
          <td style='text-align:right'>{receipt.IssuedAt:dd MMM yyyy}</td></tr>
      <tr style='border-top:2px solid #dee2e6'>
        <td style='padding:12px 0 0;font-weight:700;font-size:16px'>Total Paid</td>
        <td style='padding:12px 0 0;font-weight:700;font-size:16px;
                   text-align:right;color:#198754'>R {receipt.AmountPaid:N2}</td>
      </tr>
    </table>
  </div>
  <p style='color:#6c757d;font-size:12px'>Transaction ref: TXN-{transaction.Id:D6}</p>
  <hr style='border:none;border-top:1px solid #dee2e6'/>
  <p style='color:#adb5bd;font-size:12px;text-align:center'>BookSwap · Student Textbook Marketplace</p>
</div>";

            var sellerBody = $@"
<div style='font-family:sans-serif;max-width:520px;margin:auto'>
  <h2 style='color:#198754'>📚 Sale Complete!</h2>
  <p>Hi <strong>{transaction.Seller.DisplayUsername}</strong>,</p>
  <p>
    Your sale of <strong>{receipt.TextbookTitle}</strong> to 
    <strong>{transaction.Buyer.DisplayUsername}</strong> is complete.
    <strong>R {receipt.AmountPaid:N2}</strong> has been added to your earnings balance.
  </p>
  <p style='color:#6c757d;font-size:12px'>Transaction ref: TXN-{transaction.Id:D6}</p>
  <hr style='border:none;border-top:1px solid #dee2e6'/>
  <p style='color:#adb5bd;font-size:12px;text-align:center'>BookSwap · Student Textbook Marketplace</p>
</div>";

            var buyerOk = await SendAsync(transaction.Buyer.Email!, $"BookSwap Receipt — {receipt.TextbookTitle}", buyerBody);
            var sellerOk = await SendAsync(transaction.Seller.Email!, $"BookSwap — Sale complete: {receipt.TextbookTitle}", sellerBody);
            return buyerOk && sellerOk;
        }

        // ── Core SMTP send ─────────────────────────────────────────────────
        private async Task<bool> SendAsync(string toEmail, string subject, string htmlBody)
        {
            // Guard: skip sending if credentials aren't configured
            if (string.IsNullOrWhiteSpace(SmtpPassword) ||
                SmtpPassword == "Th3LitClub123" ||
                string.IsNullOrWhiteSpace(SenderEmail))
            {
                _logger.LogWarning(
                    "Email NOT sent — SMTP credentials not configured in appsettings.json. " +
                    "To: {To} | Subject: {Subject}", toEmail, subject);
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SenderName, SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(SenderEmail, SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent → {To}: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email → {To}: {Subject}", toEmail, subject);
                return false;
            }
        }
    }
}