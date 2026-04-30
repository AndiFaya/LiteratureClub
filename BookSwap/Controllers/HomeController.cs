using BookSwap.Data;
using BookSwap.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Show the 8 most recently posted available listings on the home page
            var recentListings = await _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .Include(l => l.CourseCode)
                .Where(l => l.Status == BookSwap.Models.ListingStatus.Available)
                .OrderByDescending(l => l.CreatedAt)
                .Take(8)
                .Select(l => new ListingCardViewModel
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
                })
                .ToListAsync();

            var totalListings = await _context.Listings
                .CountAsync(l => l.Status == BookSwap.Models.ListingStatus.Available);

            var totalUsers = await _context.Users.CountAsync();

            ViewBag.TotalListings = totalListings;
            ViewBag.TotalUsers = totalUsers;

            return View(recentListings);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}