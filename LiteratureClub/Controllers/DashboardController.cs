using LiteratureClub.Data;
using LiteratureClub.Models;
using LiteratureClub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiteratureClub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var user = await _context.Users
                .Include(u => u.Campus)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            // ── Reviews ────────────────────────────────────────────────────
            var reviews = await _context.SellerReviews
                .Where(r => r.SellerId == userId)
                .ToListAsync();

            // ── My listings ────────────────────────────────────────────────
            var myListings = await _context.Listings
                .Include(l => l.Bids)
                .Where(l => l.SellerId == userId && l.Status != ListingStatus.Removed)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // ── Purchases ──────────────────────────────────────────────────
            var myPurchases = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Seller)
                .Include(t => t.Receipt)
                .Where(t => t.BuyerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // ── Sales ──────────────────────────────────────────────────────
            var mySales = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Buyer)
                .Include(t => t.Receipt)
                .Where(t => t.SellerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // ── Watchlist ──────────────────────────────────────────────────
            var watchlist = await _context.WatchlistItems
                .Include(w => w.Listing).ThenInclude(l => l.Seller)
                .Include(w => w.Listing).ThenInclude(l => l.Category)
                .Include(w => w.Listing).ThenInclude(l => l.CourseCode)
                .Where(w => w.UserId == userId &&
                            w.Listing.Status == ListingStatus.Available)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            // ── Incoming bids on my listings ───────────────────────────────
            var incomingBids = await _context.Bids
                .Include(b => b.Listing)
                .Include(b => b.Bidder)
                .Where(b => b.Listing.SellerId == userId &&
                            b.Status == BidStatus.Pending)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // ── Unread messages ────────────────────────────────────────────
            var unreadMessages = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            var vm = new DashboardViewModel
            {
                DisplayUsername   = user.DisplayUsername,
                FullName          = $"{user.FirstName} {user.LastName}",
                Email             = user.Email ?? string.Empty,
                Campus            = $"{user.Campus.University} – {user.Campus.Name}",
                EarningsBalance   = user.EarningsBalance,
                AverageRating     = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0,
                TotalReviews      = reviews.Count,

                ActiveListingsCount = myListings.Count(l => l.Status == ListingStatus.Available),
                TotalSalesCount     = mySales.Count,
                TotalPurchasesCount = myPurchases.Count,
                PendingBidsCount    = incomingBids.Count,
                WatchlistCount      = watchlist.Count,
                UnreadMessagesCount = unreadMessages,

                MyListings = myListings.Select(l => new DashboardListingRow
                {
                    Id        = l.Id,
                    Title     = l.Title,
                    Price     = l.Price,
                    Status    = l.Status,
                    BidCount  = l.Bids.Count(b => b.Status == BidStatus.Pending),
                    CreatedAt = l.CreatedAt
                }).ToList(),

                MyPurchases = myPurchases.Select(t => new DashboardTransactionRow
                {
                    Id                  = t.Id,
                    BookTitle           = t.Listing.Title,
                    Amount              = t.Amount,
                    Status              = t.Status,
                    OtherPartyUsername  = t.Seller.DisplayUsername,
                    CreatedAt           = t.CreatedAt,
                    HasReceipt          = t.Receipt != null
                }).ToList(),

                MySales = mySales.Select(t => new DashboardTransactionRow
                {
                    Id                  = t.Id,
                    BookTitle           = t.Listing.Title,
                    Amount              = t.Amount,
                    Status              = t.Status,
                    OtherPartyUsername  = t.Buyer.DisplayUsername,
                    CreatedAt           = t.CreatedAt,
                    HasReceipt          = t.Receipt != null
                }).ToList(),

                Watchlist = watchlist.Select(w => new ListingCardViewModel
                {
                    Id               = w.Listing.Id,
                    Title            = w.Listing.Title,
                    Author           = w.Listing.Author,
                    Edition          = w.Listing.Edition,
                    Price            = w.Listing.Price,
                    Condition        = w.Listing.Condition,
                    Format           = w.Listing.Format,
                    ImagePath        = w.Listing.ImagePath,
                    SellerUsername   = w.Listing.Seller.DisplayUsername,
                    CategoryName     = w.Listing.Category.Name,
                    CourseCode       = w.Listing.CourseCode.Code,
                    IsOpenForBidding = w.Listing.IsOpenForBidding,
                    CreatedAt        = w.Listing.CreatedAt
                }).ToList(),

                IncomingBids = incomingBids.Select(b => new DashboardBidRow
                {
                    BidId           = b.Id,
                    ListingId       = b.ListingId,
                    ListingTitle    = b.Listing.Title,
                    BidderUsername  = b.Bidder.DisplayUsername,
                    OfferAmount     = b.OfferAmount,
                    Quantity        = b.Quantity,
                    Status          = b.Status,
                    ExpiresAt       = b.ExpiresAt
                }).ToList()
            };

            return View(vm);
        }
    }
}
