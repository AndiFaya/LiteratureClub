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

        // ── GET /Listings ──────────────────────────────────────────────────
        // Kept exactly as uploaded — includes new pagination feature
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
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Include(l => l.CourseCode)
                .Where(l => l.Status == ListingStatus.Available);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(l =>
                    l.Title.Contains(searchTerm) ||
                    l.Author.Contains(searchTerm) ||
                    l.ISBN.Contains(searchTerm));

            if (categoryId.HasValue) query = query.Where(l => l.CategoryId == categoryId);
            if (courseCodeId.HasValue) query = query.Where(l => l.CourseCodeId == courseCodeId);
            if (condition.HasValue) query = query.Where(l => l.Condition == condition);
            if (maxPrice.HasValue) query = query.Where(l => l.Price <= maxPrice);

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(l => l.Price),
                "price_desc" => query.OrderByDescending(l => l.Price),
                "oldest" => query.OrderBy(l => l.CreatedAt),
                _ => query.OrderByDescending(l => l.CreatedAt)
            };

            var totalRecords = await query.CountAsync();
            var listings = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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

        // ── GET /Listings/Detail/{id} ──────────────────────────────────────
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
            var reviews = await _context.SellerReviews
                .Where(r => r.SellerId == listing.SellerId)
                .ToListAsync();
            var isWatchlisted = currentUserId != null &&
                await _context.WatchlistItems
                    .AnyAsync(w => w.UserId == currentUserId && w.ListingId == id);

            var vm = new ListingDetailViewModel
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
                CategoryName = listing.Category.Name,
                CourseCode = listing.CourseCode.Code,
                ImagePath = listing.ImagePath,
                IsOpenForBidding = listing.IsOpenForBidding,
                BidExpiresAt = listing.BidExpiresAt,
                Status = listing.Status,
                CreatedAt = listing.CreatedAt,
                SellerId = listing.SellerId,
                SellerUsername = listing.Seller.DisplayUsername,
                SellerCampus = $"{listing.Seller.Campus.University} – {listing.Seller.Campus.Name}",
                SellerRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                SellerReviewCount = reviews.Count,
                IsOwnListing = currentUserId == listing.SellerId,
                IsWatchlisted = isWatchlisted,
                Bids = listing.Bids
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BidSummaryViewModel
                    {
                        Id = b.Id,
                        BidderUsername = b.Bidder.DisplayUsername,
                        OfferAmount = b.OfferAmount,
                        Quantity = b.Quantity,
                        Message = b.Message,
                        Status = b.Status,
                        ExpiresAt = b.ExpiresAt,
                        CreatedAt = b.CreatedAt
                    }).ToList()
            };

            return View(vm);
        }

        // ── GET /Listings/Create ───────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            return View(new ListingFormViewModel
            {
                Categories = await GetCategoryOptionsAsync(),
                CourseCodes = await GetCourseCodeOptionsAsync()
            });
        }

        // ── POST /Listings/Create ──────────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingFormViewModel vm)
        {
            if (vm.Condition == BookCondition.Used &&
                string.IsNullOrWhiteSpace(vm.ConditionDescription))
                ModelState.AddModelError("ConditionDescription",
                    "Please describe the condition for used books.");

            if (vm.IsOpenForBidding && vm.BidExpiresAt == null)
                ModelState.AddModelError("BidExpiresAt",
                    "Please set a bid expiry date.");

            if (!ModelState.IsValid)
            {
                vm.Categories = await GetCategoryOptionsAsync();
                vm.CourseCodes = await GetCourseCodeOptionsAsync();
                return View(vm);
            }

            var sellerId = _userManager.GetUserId(User)!;
            string? imagePath = null;
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                imagePath = await SaveImageAsync(vm.ImageFile);

            var listing = new Listing
            {
                SellerId = sellerId,
                ISBN = vm.ISBN,
                Title = vm.Title,
                Author = vm.Author,
                Edition = vm.Edition,
                PublicationYear = vm.PublicationYear,
                Publisher = vm.Publisher,
                Condition = vm.Condition,
                ConditionDescription = vm.ConditionDescription,
                Format = vm.Format,
                Price = vm.Price,
                CategoryId = vm.CategoryId,
                CourseCodeId = vm.CourseCodeId,
                IsOpenForBidding = vm.IsOpenForBidding,
                BidExpiresAt = vm.BidExpiresAt,
                ImagePath = imagePath,
                Status = ListingStatus.Available
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your listing has been posted!";
            return RedirectToAction(nameof(Detail), new { id = listing.Id });
        }

        // ── GET /Listings/Edit/{id} ────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.SellerId != currentUserId) return Forbid();

            if (listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing can no longer be edited — a bid has been accepted or a transaction is in progress.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            return View(new ListingFormViewModel
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
            });
        }

        // ── POST /Listings/Edit/{id} ───────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ListingFormViewModel vm)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.SellerId != currentUserId) return Forbid();

            if (listing.Status != ListingStatus.Available)
            {
                TempData["Error"] = "This listing can no longer be edited — a bid has been accepted or a transaction is in progress.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            ModelState.Remove(nameof(vm.Categories));
            ModelState.Remove(nameof(vm.CourseCodes));
            ModelState.Remove(nameof(vm.ExistingImagePath));
            ModelState.Remove(nameof(vm.ImageFile));

            if (vm.Condition == BookCondition.Used &&
                string.IsNullOrWhiteSpace(vm.ConditionDescription))
                ModelState.AddModelError("ConditionDescription",
                    "Please describe the condition for used books.");

            if (vm.IsOpenForBidding && vm.BidExpiresAt == null)
                ModelState.AddModelError("BidExpiresAt",
                    "Please set a bid expiry date.");

            if (!ModelState.IsValid)
            {
                vm.ExistingImagePath = listing.ImagePath;
                vm.Categories = await GetCategoryOptionsAsync();
                vm.CourseCodes = await GetCourseCodeOptionsAsync();
                return View(vm);
            }

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

        // ── POST /Listings/Delete/{id} ─────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.SellerId != currentUserId) return Forbid();

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

        // ── POST /Listings/ToggleWatchlist/{id} ────────────────────────────
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

        // ── Helpers ────────────────────────────────────────────────────────
        private async Task<List<DropdownOption>> GetCategoryOptionsAsync() =>
            await _context.TextbookCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new DropdownOption { Id = c.Id, Label = c.Name })
                .ToListAsync();

        private async Task<List<DropdownOption>> GetCourseCodeOptionsAsync() =>
            await _context.CourseCodes
                .Where(c => c.IsActive)
                .OrderBy(c => c.Code)
                .Select(c => new DropdownOption
                {
                    Id = c.Id,
                    Label = $"{c.Code} – {c.CourseName}"
                })
                .ToListAsync();

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "listings");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/listings/{uniqueName}";
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}