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
                
                Bids = listing.Bids.OrderByDescending(b => b.CreatedAt).Select(b => new BidSummaryViewModel { /* mapping */ }).ToList()
            };

            
            vm.SellerRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            vm.SellerReviewCount = reviews.Count;

            return View(vm);
        }

        // --- HELPER METHODS ---

        private async Task<List<DropdownOption>> GetCategoryOptionsAsync()
        {
            return await _context.TextbookCategories
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.SellerId != currentUserId) return Forbid();

            // Block if listing is no longer Available
            if (listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing can no longer be edited — a bid has been accepted or a transaction is in progress.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var vm = new ListingFormViewModel
            {
                Id = listing.Id,
                ISBN = listing.ISBN,
                Title = listing.Title,
                Author = listing.Author,
                Edition = listing.Edition,
                PublicationYear = listing.PublicationYear,
                Publisher = listing.Publisher,
                Condition = listing.Condition,
                ConditionDescription = listing.ConditionDescription,
                Format = listing.Format,
                Price = listing.Price,
                CategoryId = listing.CategoryId,
                CourseCodeId = listing.CourseCodeId,
                IsOpenForBidding = listing.IsOpenForBidding,
                BidExpiresAt = listing.BidExpiresAt,
                ExistingImagePath = listing.ImagePath,
                Categories = await GetCategoryOptionsAsync(),
                CourseCodes = await GetCourseCodeOptionsAsync()
            };

            return View(vm);
        }

        //POST /Listings/Edit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ListingFormViewModel vm)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.SellerId != currentUserId) return Forbid();

            // Block if listing is no longer Available
            if (listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing can no longer be edited — a bid has been accepted or a transaction is in progress.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (vm.Condition == BookCondition.Used &&
                string.IsNullOrWhiteSpace(vm.ConditionDescription))
            {
                ModelState.AddModelError("ConditionDescription",
                    "Please describe the condition for used books.");
            }

            if (vm.IsOpenForBidding && vm.BidExpiresAt == null)
            {
                ModelState.AddModelError("BidExpiresAt",
                    "Please set a bid expiry date.");
            }

            if (!ModelState.IsValid)
            {
                vm.ExistingImagePath = listing.ImagePath;
                vm.Categories = await GetCategoryOptionsAsync();
                vm.CourseCodes = await GetCourseCodeOptionsAsync();
                return View(vm);
            }

            // Replace image only if a new one was uploaded
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                DeleteImage(listing.ImagePath);
                listing.ImagePath = await SaveImageAsync(vm.ImageFile);
            }

            listing.ISBN = vm.ISBN;
            listing.Title = vm.Title;
            listing.Author = vm.Author;
            listing.Edition = vm.Edition;
            listing.PublicationYear = vm.PublicationYear;
            listing.Publisher = vm.Publisher;
            listing.Condition = vm.Condition;
            listing.ConditionDescription = vm.ConditionDescription;
            listing.Format = vm.Format;
            listing.Price = vm.Price;
            listing.CategoryId = vm.CategoryId;
            listing.CourseCodeId = vm.CourseCodeId;
            listing.IsOpenForBidding = vm.IsOpenForBidding;
            listing.BidExpiresAt = vm.BidExpiresAt;
            listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Listing updated successfully.";
            return RedirectToAction(nameof(Detail), new { id = listing.Id });
        }

        //POST /Listings/Delete
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.SellerId != currentUserId) return Forbid();

            // Block if listing is no longer Available
            if (listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing cannot be removed — a bid has been accepted or a transaction is in progress.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            listing.Status = ListingStatus.Removed;
            listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Listing removed.";
            return RedirectToAction(nameof(Index));
        }

        //POST /Listings/ToggleWatchlist
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWatchlist(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var existing = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ListingId == id);

            if (existing != null)
                _context.WatchlistItems.Remove(existing);
            else
                _context.WatchlistItems.Add(new WatchlistItem
                {
                    UserId = userId,
                    ListingId = id
                });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id });
        }

        //Helpers
        private async Task<List<DropdownOption>> GetCategoryOptionsAsync() =>
            await _context.TextbookCategories
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
    }
}