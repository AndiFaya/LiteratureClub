using LiteratureClub.Data;
using LiteratureClub.Models;
using LiteratureClub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiteratureClub.Controllers
{
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ListingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: /Listings
        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? categoryId,
            int? courseCodeId,
            BookCondition? condition,
            decimal? maxPrice,
            string sortBy = "newest",
            int pageNumber = 1,
            int pageSize = 6)
        {
            // Enforce reasonable bounds
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Include(l => l.CourseCode)
                .Where(l => l.Status == ListingStatus.Available);

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(l => l.Title.Contains(searchTerm) || l.Author.Contains(searchTerm) || l.ISBN.Contains(searchTerm));

            if (categoryId.HasValue) query = query.Where(l => l.CategoryId == categoryId);
            if (courseCodeId.HasValue) query = query.Where(l => l.CourseCodeId == courseCodeId);
            if (condition.HasValue) query = query.Where(l => l.Condition == condition);
            if (maxPrice.HasValue) query = query.Where(l => l.Price <= maxPrice);

            // Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(l => l.Price),
                "price_desc" => query.OrderByDescending(l => l.Price),
                "oldest" => query.OrderBy(l => l.CreatedAt),
                _ => query.OrderByDescending(l => l.CreatedAt)
            };

            var totalRecords = await query.CountAsync();
            var listings = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new ListingIndexViewModel
            {
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                CourseCodeId = courseCodeId,
                Condition = condition,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                Categories = await GetCategoryOptionsAsync(),
                CourseCodes = await GetCourseCodeOptionsAsync(),
                Listings = listings.Select(l => new ListingCardViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    Author = l.Author,
                    Edition = l.Edition,
                    Price = l.Price,
                    Condition = l.Condition,
                    Format = l.Format,
                    ImagePath = l.ImagePath,
                    SellerUsername = l.Seller.DisplayUsername,
                    CategoryName = l.Category.Name,
                    CourseCode = l.CourseCode.Code,
                    IsOpenForBidding = l.IsOpenForBidding,
                    CreatedAt = l.CreatedAt
                }).ToList()
            };

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Seller).ThenInclude(s => s.Campus)
                .Include(l => l.Category)
                .Include(l => l.CourseCode)
                .Include(l => l.Bids).ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var reviews = await _context.SellerReviews.Where(r => r.SellerId == listing.SellerId).ToListAsync();
            var isWatchlisted = currentUserId != null && await _context.WatchlistItems.AnyAsync(w => w.UserId == currentUserId && w.ListingId == id);

            var vm = new ListingDetailViewModel
            {
                // ... (your mapping is already perfect here)
                Bids = listing.Bids.OrderByDescending(b => b.CreatedAt).Select(b => new BidSummaryViewModel { /* mapping */ }).ToList()
            };

            // Filling in the seller stats logic you already had
            vm.SellerRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            vm.SellerReviewCount = reviews.Count;

            return View(vm);
        }

        // --- HELPER METHODS (Private & Clean) ---

        private async Task<List<DropdownOption>> GetCategoryOptionsAsync()
        {
            return await _context.TextbookCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new DropdownOption { Id = c.Id, Label = c.Name })
                .ToListAsync();
        }

        private async Task<List<DropdownOption>> GetCourseCodeOptionsAsync()
        {
            return await _context.CourseCodes
                .Where(c => c.IsActive)
                .OrderBy(c => c.Code)
                .Select(c => new DropdownOption { Id = c.Id, Label = $"{c.Code} – {c.CourseName}" })
                .ToListAsync();
        }

        // ... Create, Edit, Delete, ToggleWatchlist logic (These are fine as is!)
    }
}