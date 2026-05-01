using LiteratureClub.Data;
using LiteratureClub.Models;
using LiteratureClub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Controllers
{
    public class WantedAdsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WantedAdsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? categoryId,
            int? courseCodeId)
        {
            var query = _context.WantedAds
                .Include(w => w.Requester).ThenInclude(u => u.Campus)
                .Include(w => w.Category)
                .Include(w => w.CourseCode)
                .Where(w => w.Status == WantedAdStatus.Open);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(w =>
                    w.Title.Contains(searchTerm) ||
                    w.Author.Contains(searchTerm) ||
                    (w.ISBN != null && w.ISBN.Contains(searchTerm)));

            if (categoryId.HasValue)
                query = query.Where(w => w.CategoryId == categoryId);

            if (courseCodeId.HasValue)
                query = query.Where(w => w.CourseCodeId == courseCodeId);

            var ads = await query
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var vm = new WantedAdIndexViewModel
            {
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                CourseCodeId = courseCodeId,
                Categories = await GetCategoryOptionsAsync(),
                CourseCodes = await GetCourseCodeOptionsAsync(),
                Ads = ads.Select(w => new WantedAdCardViewModel
                {
                    Id = w.Id,
                    Title = w.Title,
                    Author = w.Author,
                    Edition = w.Edition,
                    CategoryName = w.Category.Name,
                    CourseCode = w.CourseCode.Code,
                    RequesterUsername = w.Requester.DisplayUsername,
                    RequesterCampus = $"{w.Requester.Campus.University} – {w.Requester.Campus.Name}",
                    PreferredCondition = w.PreferredCondition,
                    PreferredFormat = w.PreferredFormat,
                    Status = w.Status,
                    CreatedAt = w.CreatedAt
                }).ToList()
            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var ad = await _context.WantedAds
                .Include(w => w.Requester).ThenInclude(u => u.Campus)
                .Include(w => w.Category)
                .Include(w => w.CourseCode)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (ad == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            //find available listings with a similar title
            var matches = await _context.Listings
                .Include(l => l.Seller)
                .Where(l =>
                    l.Status == ListingStatus.Available &&
                    (l.Title.Contains(ad.Title) ||
                     ad.Title.Contains(l.Title) ||
                     l.CourseCodeId == ad.CourseCodeId))
                .OrderBy(l => l.Price)
                .Take(5)
                .Select(l => new WantedAdMatchViewModel
                {
                    ListingId = l.Id,
                    Title = l.Title,
                    Author = l.Author,
                    Price = l.Price,
                    Condition = l.Condition,
                    SellerUsername = l.Seller.DisplayUsername,
                    ImagePath = l.ImagePath
                })
                .ToListAsync();

            var vm = new WantedAdDetailViewModel
            {
                Id = ad.Id,
                ISBN = ad.ISBN,
                Title = ad.Title,
                Author = ad.Author,
                Edition = ad.Edition,
                PublicationYear = ad.PublicationYear,
                Publisher = ad.Publisher,
                CategoryName = ad.Category.Name,
                CourseCode = ad.CourseCode.Code,
                PreferredCondition = ad.PreferredCondition,
                PreferredFormat = ad.PreferredFormat,
                AdditionalNotes = ad.AdditionalNotes,
                Status = ad.Status,
                CreatedAt = ad.CreatedAt,
                RequesterId = ad.RequesterId,
                RequesterUsername = ad.Requester.DisplayUsername,
                RequesterCampus = $"{ad.Requester.Campus.University} – {ad.Requester.Campus.Name}",
                IsOwnAd = currentUserId == ad.RequesterId,
                MatchingListings = matches
            };

            return View(vm);
        }

        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            return View(new WantedAdFormViewModel
            {
                Categories = await GetCategoryOptionsAsync(),
                CourseCodes = await GetCourseCodeOptionsAsync()
            });
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WantedAdFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = await GetCategoryOptionsAsync();
                vm.CourseCodes = await GetCourseCodeOptionsAsync();
                return View(vm);
            }

            var userId = _userManager.GetUserId(User)!;

            var ad = new WantedAd
            {
                RequesterId = userId,
                ISBN = vm.ISBN?.Trim(),
                Title = vm.Title.Trim(),
                Author = vm.Author.Trim(),
                Edition = vm.Edition?.Trim(),
                PublicationYear = vm.PublicationYear,
                Publisher = vm.Publisher.Trim(),
                CategoryId = vm.CategoryId,
                CourseCodeId = vm.CourseCodeId,
                PreferredCondition = vm.PreferredCondition,
                PreferredFormat = vm.PreferredFormat,
                AdditionalNotes = vm.AdditionalNotes?.Trim(),
                Status = WantedAdStatus.Open
            };

            _context.WantedAds.Add(ad);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your wanted ad has been posted! Sellers with matching books will be able to see your request.";
            return RedirectToAction(nameof(Detail), new { id = ad.Id });
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _context.WantedAds.FindAsync(id);
            if (ad == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (ad.RequesterId != userId) return Forbid();

            return View(new WantedAdFormViewModel
            {
                Id = ad.Id,
                ISBN = ad.ISBN,
                Title = ad.Title,
                Author = ad.Author,
                Edition = ad.Edition,
                PublicationYear = ad.PublicationYear,
                Publisher = ad.Publisher,
                CategoryId = ad.CategoryId,
                CourseCodeId = ad.CourseCodeId,
                PreferredCondition = ad.PreferredCondition,
                PreferredFormat = ad.PreferredFormat,
                AdditionalNotes = ad.AdditionalNotes,
                Categories = await GetCategoryOptionsAsync(),
                CourseCodes = await GetCourseCodeOptionsAsync()
            });
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WantedAdFormViewModel vm)
        {
            var ad = await _context.WantedAds.FindAsync(id);
            if (ad == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (ad.RequesterId != userId) return Forbid();

            if (!ModelState.IsValid)
            {
                vm.Categories = await GetCategoryOptionsAsync();
                vm.CourseCodes = await GetCourseCodeOptionsAsync();
                return View(vm);
            }

            ad.ISBN = vm.ISBN?.Trim();
            ad.Title = vm.Title.Trim();
            ad.Author = vm.Author.Trim();
            ad.Edition = vm.Edition?.Trim();
            ad.PublicationYear = vm.PublicationYear;
            ad.Publisher = vm.Publisher.Trim();
            ad.CategoryId = vm.CategoryId;
            ad.CourseCodeId = vm.CourseCodeId;
            ad.PreferredCondition = vm.PreferredCondition;
            ad.PreferredFormat = vm.PreferredFormat;
            ad.AdditionalNotes = vm.AdditionalNotes?.Trim();
            ad.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Wanted ad updated.";
            return RedirectToAction(nameof(Detail), new { id });
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var ad = await _context.WantedAds.FindAsync(id);
            if (ad == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (ad.RequesterId != userId) return Forbid();

            ad.Status = WantedAdStatus.Closed;
            ad.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Wanted ad closed.";
            return RedirectToAction("Index", "Dashboard");
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFulfilled(int id)
        {
            var ad = await _context.WantedAds.FindAsync(id);
            if (ad == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (ad.RequesterId != userId) return Forbid();

            ad.Status = WantedAdStatus.Fulfilled;
            ad.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Great — we've marked your wanted ad as fulfilled!";
            return RedirectToAction("Index", "Dashboard");
        }

        //Helpers
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
    }
}
