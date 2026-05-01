using LiteratureClub.Data;
using LiteratureClub.Models;
using LiteratureClub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiteratureClub.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PayFastService _payFast;
        private readonly EmailService _email;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            PayFastService payFast,
            EmailService email,
            ILogger<TransactionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _payFast = payFast;
            _email = email;
            _logger = logger;
        }

        //POST /Transactions/Initiate  (Buy Now)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Initiate(int listingId)
        {
            var listing = await _context.Listings
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return NotFound();

            var buyerId = _userManager.GetUserId(User)!;

            if (listing.SellerId == buyerId)
            {
                TempData["Error"] = "You cannot buy your own listing.";
                return RedirectToAction("Detail", "Listings", new { id = listingId });
            }

            if (listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing is no longer available.";
                return RedirectToAction("Detail", "Listings", new { id = listingId });
            }

            return await CreateTransactionAndRedirect(listing, buyerId, listing.Price);
        }

        // GET (seller accepts bid)
        [HttpGet]
        public async Task<IActionResult> InitiateFromBid(int bidId)
        {
            var bid = await _context.Bids
                .Include(b => b.Listing).ThenInclude(l => l.Seller)
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (bid.Listing.SellerId != userId) return Forbid();

            if (bid.Status != BidStatus.Accepted)
            {
                TempData["Error"] = "This bid has not been accepted.";
                return RedirectToAction("Index", "Dashboard");
            }

            return await CreateTransactionAndRedirect(
                bid.Listing, bid.BidderId, bid.OfferAmount);
        }

        // ── GET (PayFast redirect page)
        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (transaction.BuyerId != userId) return Forbid();

            if (transaction.Status != TransactionStatus.PaymentPending)
            {
                TempData["Info"] = "This transaction has already been processed.";
                return RedirectToAction("Detail", new { id });
            }

            var buyer = transaction.Buyer;
            var paymentData = _payFast.BuildPaymentData(
                transactionId: transaction.Id,
                buyerFirstName: buyer.FirstName,
                buyerLastName: buyer.LastName,
                buyerEmail: buyer.Email!,
                amount: transaction.Amount,
                itemName: $"LiteratureClub: {transaction.Listing.Title}"
            );

            ViewBag.PayFastUrl = _payFast.GetSandboxUrl();
            ViewBag.PaymentData = paymentData;
            ViewBag.Transaction = transaction;
            return View();
        }

        // PayFast redirects the BROWSER here after payment.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentReturn(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();

            // Only confirm if still pending
            if (transaction.Status == TransactionStatus.PaymentPending)
            {
                await ConfirmPaymentAsync(transaction,
                    paymentReference: $"PF-RETURN-{id}");
            }

            TempData["Success"] = "Payment successful! Your verification code has been emailed to you.";
            return RedirectToAction("Detail", new { id });
        }

        //GET Transactions/PaymentCancel
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCancel(int id)
        {
            TempData["Error"] = "Payment was cancelled. You can try again from your dashboard.";
            return RedirectToAction("Detail", new { id });
        }

        //POST /Transactions/ItnCallback  (PayFast server-to-server)
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiForgeryToken]
        public async Task<IActionResult> ItnCallback()
        {
            _logger.LogInformation("ITN received from PayFast.");

            if (!_payFast.VerifyItn(Request.Form))
            {
                _logger.LogWarning("ITN signature verification failed.");
                return BadRequest("Invalid ITN signature.");
            }

            var paymentStatus = Request.Form["payment_status"].ToString();
            _logger.LogInformation("ITN payment_status: {Status}", paymentStatus);

            if (!int.TryParse(Request.Form["m_payment_id"], out var transactionId))
                return BadRequest("Missing payment ID.");

            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();

            if (paymentStatus == "COMPLETE" &&
                transaction.Status == TransactionStatus.PaymentPending)
            {
                await ConfirmPaymentAsync(transaction,
                    paymentReference: Request.Form["pf_payment_id"].ToString());
            }

            return Ok();
        }

        //GET /Transactions/Detail/
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .Include(t => t.PickupPoint)
                .Include(t => t.Receipt)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (transaction.BuyerId != userId && transaction.SellerId != userId)
                return Forbid();

            ViewBag.IsBuyer = transaction.BuyerId == userId;
            return View(transaction);
        }

        //POST /Transactions/VerifyExchange
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyExchange(int transactionId, string code)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (transaction.SellerId != userId) return Forbid();

            if (transaction.Status != TransactionStatus.PaymentConfirmed)
            {
                TempData["Error"] = "Payment has not been confirmed yet.";
                return RedirectToAction("Detail", new { id = transactionId });
            }

            if (!string.Equals(transaction.VerificationCode, code.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Incorrect verification code. Please ask the buyer to show their code.";
                return RedirectToAction("Detail", new { id = transactionId });
            }

            // Complete the transaction
            transaction.IsVerified = true;
            transaction.VerifiedAt = DateTime.UtcNow;
            transaction.Status = TransactionStatus.Completed;
            transaction.UpdatedAt = DateTime.UtcNow;

            // Credit seller
            var seller = await _context.Users.FindAsync(transaction.SellerId);
            if (seller != null)
            {
                seller.EarningsBalance += transaction.Amount;
                seller.UpdatedAt = DateTime.UtcNow;
            }

            // Generate receipt
            var receipt = new Receipt
            {
                TransactionId = transaction.Id,
                TextbookTitle = transaction.Listing.Title,
                TextbookAuthor = transaction.Listing.Author,
                ISBN = transaction.Listing.ISBN,
                AmountPaid = transaction.Amount,
                SellerName = $"{transaction.Seller.FirstName} {transaction.Seller.LastName}",
                BuyerName = $"{transaction.Buyer.FirstName} {transaction.Buyer.LastName}",
                IssuedAt = DateTime.UtcNow,
                PickupPointName = transaction.PickupPoint?.Name,
                EmailSent = false
            };

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            // Send receipt emails to both parties
            await _email.SendReceiptEmailAsync(receipt, transaction);

            receipt.EmailSent = true;
            receipt.EmailSentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Exchange verified! Transaction complete. A receipt has been emailed to both parties.";
            return RedirectToAction("Receipt", new { id = transactionId });
        }

        //GET /Transactions/Receipt
        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Receipt)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .Include(t => t.Listing)
                .Include(t => t.PickupPoint)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null || transaction.Receipt == null)
                return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (transaction.BuyerId != userId && transaction.SellerId != userId)
                return Forbid();

            return View(transaction.Receipt);
        }

        //Shared: confirm payment, generate code, email buyer
        private async Task ConfirmPaymentAsync(
            Transaction transaction, string paymentReference)
        {
            var code = GenerateVerificationCode();

            transaction.Status = TransactionStatus.PaymentConfirmed;
            transaction.VerificationCode = code;
            transaction.PaymentReference = paymentReference;
            transaction.UpdatedAt = DateTime.UtcNow;

            transaction.Listing.Status = ListingStatus.Sold;
            transaction.Listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Transaction {Id} confirmed. Code: {Code}", transaction.Id, code);

            await _email.SendVerificationCodeAsync(transaction);
        }

        private async Task<IActionResult> CreateTransactionAndRedirect(
            Listing listing, string buyerId, decimal amount)
        {
            var transaction = new Transaction
            {
                ListingId = listing.Id,
                BuyerId = buyerId,
                SellerId = listing.SellerId,
                Amount = amount,
                Status = TransactionStatus.PaymentPending
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction("Pay", new { id = transaction.Id });
        }

        private static string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new byte[6];
            System.Security.Cryptography.RandomNumberGenerator.Fill(rng);
            return new string(rng.Select(b => chars[b % chars.Length]).ToArray());
        }
    }

    internal class IgnoreAntiForgeryTokenAttribute : Attribute
    {
    }
}


