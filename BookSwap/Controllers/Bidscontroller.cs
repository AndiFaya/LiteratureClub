using BookSwap.Data;
using BookSwap.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Controllers
{
    [Authorize]
    public class BidsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BidsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── POST: /Bids/Place ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(int listingId, decimal offerAmount,
            int quantity, string? message)
        {
            var listing = await _context.Listings
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;

            if (listing.SellerId == userId)
            {
                TempData["Error"] = "You cannot bid on your own listing.";
                return RedirectToAction("Detail", "Listings", new { id = listingId });
            }

            if (!listing.IsOpenForBidding || listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing is not open for bidding.";
                return RedirectToAction("Detail", "Listings", new { id = listingId });
            }

            if (listing.BidExpiresAt.HasValue && listing.BidExpiresAt < DateTime.UtcNow)
            {
                TempData["Error"] = "The bidding period for this listing has expired.";
                return RedirectToAction("Detail", "Listings", new { id = listingId });
            }

            if (offerAmount <= 0)
            {
                TempData["Error"] = "Please enter a valid offer amount.";
                return RedirectToAction("Detail", "Listings", new { id = listingId });
            }

            var bid = new Bid
            {
                ListingId = listingId,
                BidderId = userId,
                OfferAmount = offerAmount,
                Quantity = quantity < 1 ? 1 : quantity,
                Message = message,
                Status = BidStatus.Pending,
                ExpiresAt = listing.BidExpiresAt ?? DateTime.UtcNow.AddDays(7)
            };

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your bid has been submitted. The seller will be in touch.";
            return RedirectToAction("Detail", "Listings", new { id = listingId });
        }

        // ── POST: /Bids/Accept ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int bidId)
        {
            var bid = await _context.Bids
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;

            if (bid.Listing.SellerId != userId) return Forbid();

            if (bid.Status != BidStatus.Pending)
            {
                TempData["Error"] = "This bid is no longer pending.";
                return RedirectToAction("Detail", "Listings", new { id = bid.ListingId });
            }

            // Accept this bid and reject all others on the same listing
            var otherBids = await _context.Bids
                .Where(b => b.ListingId == bid.ListingId &&
                            b.Id != bidId &&
                            b.Status == BidStatus.Pending)
                .ToListAsync();

            foreach (var other in otherBids)
            {
                other.Status = BidStatus.Rejected;
                other.UpdatedAt = DateTime.UtcNow;
            }

            bid.Status = BidStatus.Accepted;
            bid.UpdatedAt = DateTime.UtcNow;

            // Mark listing as UnderOffer
            bid.Listing.Status = ListingStatus.UnderOffer;
            bid.Listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Redirect buyer to payment initiation
            TempData["Success"] = "Bid accepted. The buyer will now be prompted to pay.";
            return RedirectToAction("InitiateFromBid", "Transactions",
                new { bidId = bid.Id });
        }

        // ── POST: /Bids/Reject ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int bidId)
        {
            var bid = await _context.Bids
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (bid.Listing.SellerId != userId) return Forbid();

            bid.Status = BidStatus.Rejected;
            bid.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Bid rejected.";

            var returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        // ── POST: /Bids/Withdraw ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int bidId)
        {
            var bid = await _context.Bids
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (bid.BidderId != userId) return Forbid();

            if (bid.Status != BidStatus.Pending)
            {
                TempData["Error"] = "Only pending bids can be withdrawn.";
                return RedirectToAction("Index", "Dashboard");
            }

            bid.Status = BidStatus.Withdrawn;
            bid.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Bid withdrawn.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}