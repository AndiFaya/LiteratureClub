using LiteratureClub.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LiteratureClub.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        private string ApiKey => _config["SendGrid:ApiKey"] ?? "";
        private string SenderEmail => _config["SendGrid:SenderEmail"] ?? "somasharelitclub@gmail.com";
        private string SenderName => _config["SendGrid:SenderName"] ?? "LiteratureClub";

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        //Registration email verification link
        public async Task<bool> SendEmailVerificationAsync(
            ApplicationUser user, string verificationLink)
        {
            var subject = "Verify your LiteratureClub email address";

            var html = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
  <tr><td align='center' style='padding:40px 16px'>
    <table width='520' cellpadding='0' cellspacing='0'
           style='background:#fff;border-radius:8px;overflow:hidden;
                  box-shadow:0 2px 8px rgba(0,0,0,.08)'>
      <!-- Header -->
      <tr><td style='background:#0d6efd;padding:28px 32px;text-align:center'>
        <span style='font-size:32px'>📚</span>
        <h1 style='color:#fff;margin:8px 0 0;font-size:22px'>LiteratureClub</h1>
      </td></tr>
      <!-- Body -->
      <tr><td style='padding:36px 32px'>
        <h2 style='margin-top:0;color:#1a1a2e'>
          Welcome, {user.DisplayUsername}!
        </h2>
        <p style='color:#555;line-height:1.6'>
          Thanks for registering on LiteratureClub — your university textbook marketplace.
          Please click the button below to verify your email address and activate
          your account.
        </p>
        <div style='text-align:center;margin:32px 0'>
          <a href='{verificationLink}'
             style='background:#0d6efd;color:#fff;text-decoration:none;
                    padding:14px 32px;border-radius:6px;font-weight:700;
                    font-size:16px;display:inline-block'>
            Verify My Email Address
          </a>
        </div>
        <p style='color:#888;font-size:13px;line-height:1.6'>
          This link expires in <strong>24 hours</strong>.<br/>
          If you didn't create a LiteratureClub account, you can safely ignore this email.
        </p>
        <p style='color:#888;font-size:12px;word-break:break-all'>
          Or copy this link into your browser:<br/>
          <a href='{verificationLink}' style='color:#0d6efd'>{verificationLink}</a>
        </p>
      </td></tr>
      <!-- Footer -->
      <tr><td style='background:#f8f9fa;padding:20px 32px;text-align:center;
                     color:#adb5bd;font-size:12px;border-top:1px solid #e9ecef'>
        LiteratureClub · Student Textbook Marketplace<br/>
        somasharelitclub@gmail.com
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

            var text = $"Welcome to LiteratureClub, {user.DisplayUsername}!\n\n" +
                       $"Verify your email by visiting:\n{verificationLink}\n\n" +
                       "This link expires in 24 hours.";

            return await SendAsync(user.Email!, subject, html, text);
        }

        //Payment confirmed
        public async Task<bool> SendVerificationCodeAsync(Transaction transaction)
        {
            var subject = $"LiteratureClub — Your pickup code for \"{transaction.Listing.Title}\"";

            var html = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
  <tr><td align='center' style='padding:40px 16px'>
    <table width='520' cellpadding='0' cellspacing='0'
           style='background:#fff;border-radius:8px;overflow:hidden;
                  box-shadow:0 2px 8px rgba(0,0,0,.08)'>
      <tr><td style='background:#198754;padding:28px 32px;text-align:center'>
        <span style='font-size:32px'>✅</span>
        <h1 style='color:#fff;margin:8px 0 0;font-size:22px'>Payment Confirmed!</h1>
      </td></tr>
      <tr><td style='padding:36px 32px'>
        <p style='color:#555;line-height:1.6'>
          Hi <strong>{transaction.Buyer.DisplayUsername}</strong>,<br/><br/>
          Your payment of <strong>R {transaction.Amount:N2}</strong> for
          <strong>{transaction.Listing.Title}</strong> has been received.
        </p>
        <p style='color:#555'>
          When you meet the seller to collect your book, show them this code:
        </p>
        <!-- Code box -->
        <div style='text-align:center;margin:28px 0'>
          <div style='display:inline-block;background:#f8f9fa;
                      border:2px solid #0d6efd;border-radius:10px;
                      padding:20px 40px'>
            <div style='color:#6c757d;font-size:13px;margin-bottom:10px;
                        letter-spacing:1px;text-transform:uppercase'>
              Verification Code
            </div>
            <div style='font-size:42px;font-weight:700;
                        letter-spacing:12px;color:#198754;
                        font-family:monospace'>
              {transaction.VerificationCode}
            </div>
          </div>
        </div>
        <!-- Warning -->
        <table width='100%' cellpadding='0' cellspacing='0'
               style='background:#fff3cd;border-left:4px solid #ffc107;
                      border-radius:4px;margin-bottom:24px'>
          <tr><td style='padding:14px 16px;color:#856404;font-size:14px'>
            <strong>⚠ Important:</strong> Only show this code <em>after</em>
            you have the book in hand. The seller enters it to complete the
            transaction and receive payment.
          </td></tr>
        </table>
        <p style='color:#555;font-size:14px'>
          <strong>Seller:</strong> {transaction.Seller.DisplayUsername}<br/>
          Meet at a safe, public campus pickup point.
        </p>
      </td></tr>
      <tr><td style='background:#f8f9fa;padding:20px 32px;text-align:center;
                     color:#adb5bd;font-size:12px;border-top:1px solid #e9ecef'>
        LiteratureClub · Student Textbook Marketplace
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

            var text = $"Payment confirmed!\n\n" +
                       $"Book: {transaction.Listing.Title}\n" +
                       $"Amount: R {transaction.Amount:N2}\n" +
                       $"Your verification code: {transaction.VerificationCode}\n\n" +
                       "Show this code to the seller only after you have the book.";

            return await SendAsync(transaction.Buyer.Email!, subject, html, text);
        }

        //Transaction complete, receipt to buyer and seller
        public async Task<bool> SendReceiptEmailAsync(Receipt receipt, Transaction transaction)
        {
            //Buyer receipt
            var buyerSubject = $"LiteratureClub Receipt — {receipt.TextbookTitle}";
            var pickupRow = string.IsNullOrEmpty(receipt.PickupPointName) ? "" :
                $"<tr><td style='padding:6px 0;color:#6c757d'>Pickup point</td>" +
                $"<td style='text-align:right'>{receipt.PickupPointName}</td></tr>";
            var isbnRow = string.IsNullOrEmpty(receipt.ISBN) ? "" :
                $"<tr><td style='padding:6px 0;color:#6c757d'>ISBN</td>" +
                $"<td style='text-align:right'>{receipt.ISBN}</td></tr>";

            var buyerHtml = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
  <tr><td align='center' style='padding:40px 16px'>
    <table width='520' cellpadding='0' cellspacing='0'
           style='background:#fff;border-radius:8px;overflow:hidden;
                  box-shadow:0 2px 8px rgba(0,0,0,.08)'>
      <tr><td style='background:#198754;padding:28px 32px;text-align:center'>
        <span style='font-size:32px'>🎉</span>
        <h1 style='color:#fff;margin:8px 0 0;font-size:22px'>Purchase Complete!</h1>
      </td></tr>
      <tr><td style='padding:36px 32px'>
        <p style='color:#555'>Hi <strong>{transaction.Buyer.DisplayUsername}</strong>,
          your purchase is confirmed. Here is your receipt:</p>
        <!-- Receipt card -->
        <table width='100%' cellpadding='0' cellspacing='0'
               style='background:#f8f9fa;border-radius:8px;padding:24px;margin:20px 0'>
          <tr><td>
            <h3 style='margin:0 0 16px;padding-bottom:12px;
                       border-bottom:1px solid #dee2e6;color:#1a1a2e'>
              Receipt #RCP-{receipt.Id:D6}
            </h3>
            <table width='100%' cellpadding='0' cellspacing='0'
                   style='font-size:14px'>
              <tr><td style='padding:6px 0;color:#6c757d'>Book</td>
                  <td style='text-align:right;font-weight:700'>{receipt.TextbookTitle}</td></tr>
              <tr><td style='padding:6px 0;color:#6c757d'>Author</td>
                  <td style='text-align:right'>{receipt.TextbookAuthor}</td></tr>
              {isbnRow}
              <tr><td style='padding:6px 0;color:#6c757d'>Seller</td>
                  <td style='text-align:right'>{receipt.SellerName}</td></tr>
              {pickupRow}
              <tr><td style='padding:6px 0;color:#6c757d'>Date</td>
                  <td style='text-align:right'>{receipt.IssuedAt:dd MMM yyyy}</td></tr>
              <tr style='border-top:2px solid #dee2e6'>
                <td style='padding:14px 0 0;font-weight:700;font-size:16px'>Total Paid</td>
                <td style='padding:14px 0 0;font-weight:700;font-size:18px;
                           text-align:right;color:#198754'>R {receipt.AmountPaid:N2}</td>
              </tr>
            </table>
          </td></tr>
        </table>
        <p style='color:#adb5bd;font-size:12px'>
          Transaction ref: TXN-{transaction.Id:D6}
        </p>
      </td></tr>
      <tr><td style='background:#f8f9fa;padding:20px 32px;text-align:center;
                     color:#adb5bd;font-size:12px;border-top:1px solid #e9ecef'>
        LiteratureClub · Student Textbook Marketplace
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

            var buyerText = $"Purchase complete!\n\nReceipt #RCP-{receipt.Id:D6}\n" +
                            $"Book: {receipt.TextbookTitle}\nAmount: R {receipt.AmountPaid:N2}\n" +
                            $"Transaction: TXN-{transaction.Id:D6}";

            //Seller notification
            var sellerSubject = $"LiteratureClub — Sale complete: {receipt.TextbookTitle}";
            var sellerHtml = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
  <tr><td align='center' style='padding:40px 16px'>
    <table width='520' cellpadding='0' cellspacing='0'
           style='background:#fff;border-radius:8px;overflow:hidden;
                  box-shadow:0 2px 8px rgba(0,0,0,.08)'>
      <tr><td style='background:#0d6efd;padding:28px 32px;text-align:center'>
        <span style='font-size:32px'>💰</span>
        <h1 style='color:#fff;margin:8px 0 0;font-size:22px'>Sale Complete!</h1>
      </td></tr>
      <tr><td style='padding:36px 32px'>
        <p style='color:#555;line-height:1.6'>
          Hi <strong>{transaction.Seller.DisplayUsername}</strong>,<br/><br/>
          Your sale of <strong>{receipt.TextbookTitle}</strong> to
          <strong>{transaction.Buyer.DisplayUsername}</strong> has been completed.
        </p>
        <div style='background:#d1e7dd;border-radius:8px;padding:20px;
                    text-align:center;margin:24px 0'>
          <div style='color:#0f5132;font-size:13px;margin-bottom:6px'>
            Earnings added to your balance
          </div>
          <div style='color:#0f5132;font-size:32px;font-weight:700'>
            R {receipt.AmountPaid:N2}
          </div>
        </div>
        <p style='color:#adb5bd;font-size:12px'>
          Transaction ref: TXN-{transaction.Id:D6}
        </p>
      </td></tr>
      <tr><td style='background:#f8f9fa;padding:20px 32px;text-align:center;
                     color:#adb5bd;font-size:12px;border-top:1px solid #e9ecef'>
        LiteratureClub · Student Textbook Marketplace
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

            var sellerText = $"Sale complete!\n\nBook: {receipt.TextbookTitle}\n" +
                             $"Earnings: R {receipt.AmountPaid:N2}\n" +
                             $"Transaction: TXN-{transaction.Id:D6}";

            var buyerOk = await SendAsync(transaction.Buyer.Email!, buyerSubject, buyerHtml, buyerText);
            var sellerOk = await SendAsync(transaction.Seller.Email!, sellerSubject, sellerHtml, sellerText);
            return buyerOk && sellerOk;
        }

        //SendGrid send
        private async Task<bool> SendAsync(
            string toEmail, string subject, string htmlBody, string textBody)
        {
            //unconfigured API key
            if (string.IsNullOrWhiteSpace(ApiKey) ||
                ApiKey == "YOUR_SENDGRID_API_KEY_HERE")
            {
                _logger.LogWarning(
                    "SendGrid API key not configured. Email NOT sent → {To} | {Subject}",
                    toEmail, subject);
                return false;
            }

            try
            {
                var client = new SendGridClient(ApiKey);
                var from = new EmailAddress(SenderEmail, SenderName);
                var to = new EmailAddress(toEmail);
                var message = MailHelper.CreateSingleEmail(
                    from, to, subject, textBody, htmlBody);

                var response = await client.SendEmailAsync(message);

                var success = (int)response.StatusCode >= 200 &&
                              (int)response.StatusCode < 300;

                if (success)
                    _logger.LogInformation(
                        "SendGrid: email sent → {To} | {Subject} | HTTP {Code}",
                        toEmail, subject, (int)response.StatusCode);
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError(
                        "SendGrid: failed → {To} | {Subject} | HTTP {Code} | {Body}",
                        toEmail, subject, (int)response.StatusCode, body);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SendGrid exception → {To} | {Subject}", toEmail, subject);
                return false;
            }
        }
    }
}