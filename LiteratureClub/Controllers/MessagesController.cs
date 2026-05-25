using LiteratureClub.Data;
using LiteratureClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiteratureClub.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /Messages/Thread/{transactionId}
        // Shows the full conversation between buyer and seller for a transaction
        [HttpGet]
        public async Task<IActionResult> Thread(int transactionId)
        {
            var userId = _userManager.GetUserId(User)!;

            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();

            // Only buyer or seller may view this thread
            if (transaction.BuyerId != userId && transaction.SellerId != userId)
                return Forbid();

            // Load all messages for this transaction
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.TransactionId == transactionId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark all unread messages sent to the current user as read
            var unread = messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .ToList();

            foreach (var m in unread)
                m.IsRead = true;

            if (unread.Any())
                await _context.SaveChangesAsync();

            var otherParty = transaction.BuyerId == userId
                ? transaction.Seller
                : transaction.Buyer;

            ViewBag.Transaction = transaction;
            ViewBag.Messages = messages;
            ViewBag.CurrentUserId = userId;
            ViewBag.OtherParty = otherParty;
            ViewBag.IsBuyer = transaction.BuyerId == userId;

            return View();
        }

        //POST /Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int transactionId, string content)
        {
            var userId = _userManager.GetUserId(User)!;

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Thread), new { transactionId });
            }

            if (content.Trim().Length > 2000)
            {
                TempData["Error"] = "Message cannot exceed 2000 characters.";
                return RedirectToAction(nameof(Thread), new { transactionId });
            }

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();

            if (transaction.BuyerId != userId && transaction.SellerId != userId)
                return Forbid();

            // Determine receiver (the other party)
            var receiverId = transaction.BuyerId == userId
                ? transaction.SellerId
                : transaction.BuyerId;

            var message = new Message
            {
                TransactionId = transactionId,
                SenderId = userId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Thread), new { transactionId });
        }

        //GET /Messages/Inbox
        // Lists all active message threads for the current user
        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var userId = _userManager.GetUserId(User)!;

            // Get all transactions where the user is buyer or seller that have at least one message, ordered by most recent message
            var threads = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Seller)
                .Include(t => t.Messages)
                .Where(t =>
                    (t.BuyerId == userId || t.SellerId == userId) &&
                    t.Status != TransactionStatus.PaymentPending &&
                    t.Status != TransactionStatus.Refunded)
                .OrderByDescending(t =>
                    t.Messages.Any()
                        ? t.Messages.Max(m => m.SentAt)
                        : t.CreatedAt)
                .ToListAsync();

            var threadVms = threads.Select(t =>
            {
                var lastMessage = t.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                var unreadCount = t.Messages.Count(m => m.ReceiverId == userId && !m.IsRead);
                var otherParty = t.BuyerId == userId ? t.Seller : t.Buyer;

                return new MessageThreadViewModel
                {
                    TransactionId = t.Id,
                    BookTitle = t.Listing.Title,
                    OtherPartyName = otherParty.DisplayUsername,
                    LastMessagePreview = lastMessage == null ? null
                        : lastMessage.Content.Length > 80
                            ? lastMessage.Content[..80] + "…"
                            : lastMessage.Content,
                    LastMessageAt = lastMessage?.SentAt,
                    LastMessageWasMine = lastMessage?.SenderId == userId,
                    UnreadCount = unreadCount,
                    TransactionStatus = t.Status
                };
            }).ToList();

            return View(threadVms);
        }
    }

    //ViewModel for inbox thread list
    public class MessageThreadViewModel
    {
        public int TransactionId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        public string? LastMessagePreview { get; set; }   // null = no messages yet
        public DateTime? LastMessageAt { get; set; }   // null = no messages yet
        public bool? LastMessageWasMine { get; set; }
        public int UnreadCount { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public bool HasMessages => LastMessagePreview != null;
    }
}