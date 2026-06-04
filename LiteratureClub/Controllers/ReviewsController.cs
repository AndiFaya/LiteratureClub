using LiteratureClub.Data;
using LiteratureClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiteratureClub.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /Reviews/Leave/{transactionId}
        // Shows the review form to the buyer
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Leave(int transactionId)
        {
            var userId = _userManager.GetUserId(User)!;

            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();

            // Only the buyer can leave a review
            if (transaction.BuyerId != userId)
                return Forbid();

            // Prevent self-review (buyer and seller must be different people)
            if (transaction.SellerId == userId)
            {
                TempData["Error"] = "You cannot review yourself.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Only allow reviews on completed transactions
            if (transaction.Status != TransactionStatus.Completed)
            {
                TempData["Error"] = "You can only review a seller after the transaction is fully completed.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Prevent duplicate reviews
            var alreadyReviewed = await _context.SellerReviews
                .AnyAsync(r => r.TransactionId == transactionId &&
                               r.ReviewerId == userId);
            if (alreadyReviewed)
            {
                TempData["Info"] = "You have already reviewed this seller.";
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Transaction = transaction;
            return View();
        }

        // POST /Reviews/Submit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int transactionId, int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User)!;

            var transaction = await _context.Transactions
                .Include(t => t.Listing)
                .Include(t => t.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();
            if (transaction.BuyerId != userId) return Forbid();

            // Prevent self-review
            if (transaction.SellerId == userId)
            {
                TempData["Error"] = "You cannot review yourself.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (transaction.Status != TransactionStatus.Completed)
            {
                TempData["Error"] = "Transaction is not completed yet.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Validate rating range
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Please select a rating between 1 and 5 stars.";
                return RedirectToAction("Leave", new { transactionId });
            }

            // Prevent duplicate
            var alreadyReviewed = await _context.SellerReviews
                .AnyAsync(r => r.TransactionId == transactionId &&
                               r.ReviewerId == userId);
            if (alreadyReviewed)
            {
                TempData["Info"] = "You have already reviewed this seller.";
                return RedirectToAction("Index", "Dashboard");
            }

            var review = new SellerReview
            {
                TransactionId = transactionId,
                ReviewerId = userId,
                SellerId = transaction.SellerId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment)
                    ? null
                    : comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Thank you! Your review for {transaction.Seller.DisplayUsername} has been submitted.";
            return RedirectToAction("Index", "Dashboard");
        }

        //  GET /Reviews/SellerProfile/{sellerId}
        // Public profile showing a seller's reviews and average rating
        [HttpGet]
        public async Task<IActionResult> SellerProfile(string sellerId)
        {
            var seller = await _context.Users
                .Include(u => u.Campus)
                .FirstOrDefaultAsync(u => u.Id == sellerId);

            if (seller == null) return NotFound();

            var reviews = await _context.SellerReviews
                .Include(r => r.Reviewer)
                .Include(r => r.Transaction).ThenInclude(t => t.Listing)
                .Where(r => r.SellerId == sellerId && !r.IsFlagged)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var activeListings = await _context.Listings
                .Where(l => l.SellerId == sellerId &&
                            l.Status == ListingStatus.Available)
                .CountAsync();

            var totalSales = await _context.Transactions
                .CountAsync(t => t.SellerId == sellerId &&
                                 t.Status == TransactionStatus.Completed);

            ViewBag.Seller = seller;
            ViewBag.Reviews = reviews;
            ViewBag.ActiveListings = activeListings;
            ViewBag.TotalSales = totalSales;
            ViewBag.AverageRating = reviews.Count > 0
                ? reviews.Average(r => r.Rating)
                : 0.0;

            // Rating breakdown,how many of each star
            ViewBag.RatingBreakdown = Enumerable.Range(1, 5)
                .Reverse()
                .Select(star => new
                {
                    Star = star,
                    Count = reviews.Count(r => r.Rating == star),
                    Pct = reviews.Count > 0
                        ? (int)Math.Round(
                            reviews.Count(r => r.Rating == star) * 100.0
                            / reviews.Count)
                        : 0
                })
                .ToList();

            return View();
        }
    }
}
