using BookSwap.Data;
using BookSwap.Models;
using BookSwap.Services;
using BookSwap.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BookSwap.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _email;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            EmailService email,
            IWebHostEnvironment env,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _email = email;
            _env = env;
            _logger = logger;
        }

        // ── GET /Account/Register ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel { Campuses = await GetCampusOptionsAsync() });
        }

        // ── POST /Account/Register ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.DisplayUsername) &&
                !Regex.IsMatch(vm.DisplayUsername, @"^[a-zA-Z0-9_]{3,30}$"))
                ModelState.AddModelError(nameof(vm.DisplayUsername),
                    "Username must be 3–30 characters: letters, numbers, underscores only.");

            if (!string.IsNullOrWhiteSpace(vm.StudentNumber) &&
                !Regex.IsMatch(vm.StudentNumber, @"^\d{6,15}$"))
                ModelState.AddModelError(nameof(vm.StudentNumber),
                    "Student number must be 6–15 digits only.");

            if (!ModelState.IsValid)
            {
                vm.Campuses = await GetCampusOptionsAsync();
                return View(vm);
            }

            try
            {
                if (await _userManager.FindByEmailAsync(vm.Email.Trim()) != null)
                    ModelState.AddModelError(nameof(vm.Email),
                        "An account with this email already exists.");

                if (await _userManager.Users
                        .AnyAsync(u => u.StudentNumber == vm.StudentNumber.Trim()))
                    ModelState.AddModelError(nameof(vm.StudentNumber),
                        "This student number is already registered.");

                if (await _userManager.Users
                        .AnyAsync(u => u.DisplayUsername.ToLower() ==
                                       vm.DisplayUsername.ToLower().Trim()))
                    ModelState.AddModelError(nameof(vm.DisplayUsername),
                        "This username is already taken.");

                var campus = await _context.Campuses
                    .FirstOrDefaultAsync(c => c.Id == vm.CampusId && c.IsActive);
                if (campus == null)
                    ModelState.AddModelError(nameof(vm.CampusId), "Please select a valid campus.");

                if (!ModelState.IsValid)
                {
                    vm.Campuses = await GetCampusOptionsAsync();
                    return View(vm);
                }

                var user = new ApplicationUser
                {
                    FirstName = vm.FirstName.Trim(),
                    LastName = vm.LastName.Trim(),
                    DisplayUsername = vm.DisplayUsername.Trim(),
                    StudentNumber = vm.StudentNumber.Trim(),
                    Email = vm.Email.ToLower().Trim(),
                    UserName = vm.Email.ToLower().Trim(),
                    City = vm.City.Trim(),
                    CampusId = vm.CampusId,
                    IsActive = true,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, vm.Password);
                _logger.LogInformation("CreateAsync for {Email}: {Succeeded}. Errors: {Errors}",
                    user.Email, result.Succeeded,
                    string.Join("; ", result.Errors.Select(e => e.Description)));

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    vm.Campuses = await GetCampusOptionsAsync();
                    return View(vm);
                }

                await _userManager.AddToRoleAsync(user, "Student");

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, token },
                    Request.Scheme)!;

                // ── Development mode: auto-confirm + log the link ──────────
                // No Gmail setup needed during development.
                if (_env.IsDevelopment())
                {
                    _logger.LogWarning(
                        "=== DEV MODE: EMAIL VERIFICATION LINK ===\n{Link}\n=========================================",
                        confirmLink);

                    // Auto-confirm so the developer can log straight in
                    await _userManager.ConfirmEmailAsync(user, token);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["Success"] = $"Welcome, {user.DisplayUsername}! " +
                        "(Dev mode: email auto-confirmed. Check Output window for the verification link.)";
                    return RedirectToAction("Index", "Home");
                }

                // ── Production: send real verification email ───────────────
                var emailSent = await _email.SendEmailVerificationAsync(user, confirmLink);
                if (!emailSent)
                {
                    // Email failed — auto-confirm so user isn't locked out,
                    // and log the link so admin can manually share it.
                    _logger.LogError(
                        "Verification email FAILED for {Email}. Manual link:\n{Link}",
                        user.Email, confirmLink);

                    await _userManager.ConfirmEmailAsync(user, token);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["Success"] = $"Welcome, {user.DisplayUsername}! " +
                        "Your account is active. (Email delivery failed — please contact support if needed.)";
                    return RedirectToAction("Index", "Home");
                }

                return RedirectToAction("VerificationSent", new { email = user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty,
                    "A system error occurred. Please ensure the database is running and try again.");
            }

            vm.Campuses = await GetCampusOptionsAsync();
            return View(vm);
        }

        // ── GET /Account/VerificationSent ──────────────────────────────────
        [HttpGet]
        public IActionResult VerificationSent(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        // ── GET /Account/ResendVerification ───────────────────────────────
        [HttpGet]
        public IActionResult ResendVerification() => View();

        // ── POST /Account/ResendVerification ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(string email)
        {
            // Always show success to prevent email enumeration
            ViewBag.Sent = true;

            var user = await _userManager.FindByEmailAsync(email?.Trim() ?? "");
            if (user == null || user.EmailConfirmed)
                return View();

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = Url.Action(
                "ConfirmEmail", "Account",
                new { userId = user.Id, token },
                Request.Scheme)!;

            _logger.LogInformation("Resend verification link for {Email}:\n{Link}",
                user.Email, confirmLink);

            await _email.SendEmailVerificationAsync(user, confirmLink);
            return View();
        }

        // ── GET /Account/ConfirmEmail ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Register");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.EmailConfirmed)
            {
                TempData["Success"] = "Your email is already verified. Please sign in.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Welcome to BookSwap, {user.DisplayUsername}! Your email is verified.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "The verification link is invalid or has expired. Please request a new one below.";
            return View("ConfirmEmailResult");
        }

        // ── GET /Account/Login ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // ── POST /Account/Login ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                var user = await _userManager.FindByEmailAsync(vm.Email.Trim());
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(vm);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty,
                        "This account has been suspended. Please contact support.");
                    return View(vm);
                }

                if (!user.EmailConfirmed)
                {
                    // Let them know and offer a resend link
                    ModelState.AddModelError(string.Empty,
                        "Please verify your email before signing in.");
                    ViewData["UnverifiedEmail"] = user.Email;
                    return View(vm);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user, vm.Password, vm.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in.", vm.Email);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty,
                        "Account locked after too many failed attempts. Try again in 5 minutes.");
                    return View(vm);
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty, "A system error occurred. Please try again.");
            }

            return View(vm);
        }

        // ── POST /Account/Logout ───────────────────────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ── GET /Account/AccessDenied ──────────────────────────────────────
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ── Helpers ────────────────────────────────────────────────────────
        private async Task<List<CampusOption>> GetCampusOptionsAsync()
        {
            try
            {
                return await _context.Campuses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.University).ThenBy(c => c.Name)
                    .Select(c => new CampusOption
                    {
                        Id = c.Id,
                        DisplayName = $"{c.University} – {c.Name}"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load campuses.");
                return new();
            }
        }
    }
}