using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiteratureClub.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LiteratureClub.Models;


namespace LiteratureClub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context, 
            ILogger<AdminController> logger, 
            RoleManager<IdentityRole> roleManager, 
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // GET: Admin
        public async  Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalListings = await _context.Listings.CountAsync();
            ViewBag.ActiveListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Available);
            ViewBag.TotalTransactions = await _context.Transactions.CountAsync();
            ViewBag.PendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
            ViewBag.TotalAnnouncements = await _context.Announcements.CountAsync();

            return View();
        }


        //USERS MANAGEMENT
        // GET: Admin/Users
        public async Task<IActionResult> Users(string? search)
        {
            var query = _context.Users
                .Include(u => u.Campus)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u =>
                    u.Email!.Contains(search) ||
                    u.DisplayUsername.Contains(search) ||
                    u.StudentNumber.Contains(search));

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            // Attach roles to each user
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
                userRoles[u.Id] = await _userManager.GetRolesAsync(u);

            ViewBag.UserRoles = userRoles;
            ViewBag.Search = search;
            return View(users);
        }

        // POST /Admin/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Admin toggled user {Id} active={Active}", userId, user.IsActive);
            TempData["Success"] = $"User '{user.DisplayUsername}' has been {(user.IsActive ? "activated" : "suspended")}.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/PromoteToAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Student");
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = $"'{user.DisplayUsername}' is now an Admin.";
                _logger.LogInformation("Admin promoted user {Id} to Admin", userId);
            }

            return RedirectToAction(nameof(Users));
        }

        //Listings management

        // GET /Admin/Listings
        public async Task<IActionResult> Listings(string? search)
        {
            var query = _context.Listings
                .Include(l => l.Seller)
                .Include(l => l.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    l.Title.Contains(search) ||
                    l.Author.Contains(search) ||
                    l.Seller.DisplayUsername.Contains(search));

            var listings = await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            return View(listings);
        }

        
        // POST /Admin/RemoveListing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveListing(int listingId, string reason)
        {
            var listing = await _context.Listings.FindAsync(listingId);
            if (listing == null) return NotFound();

            listing.Status = ListingStatus.Removed;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Listing '{listing.Title}' removed.";
            _logger.LogInformation("Admin removed listing {Id}. Reason: {Reason}", listingId, reason);
            return RedirectToAction(nameof(Listings));
        }

        // Reports 

        // GET /Admin/Reports
        public async Task<IActionResult> Reports()
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.Listing)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // POST /Admin/ResolveReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int reportId, ReportStatus status)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return NotFound();

            report.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Report #{reportId} marked as {status}.";
            return RedirectToAction(nameof(Reports));
        }
    }
}
