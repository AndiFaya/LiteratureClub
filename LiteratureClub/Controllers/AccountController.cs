using LiteratureClub.Data;
using LiteratureClub.Models;
using LiteratureClub.Services;
using LiteratureClub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LiteratureClub.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _email;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            EmailService email,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _email = email;
            _logger = logger;
        }

        
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel
            {
                Campuses = await GetCampusOptionsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            // Extra format validation
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
                // Uniqueness checks
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
                    ModelState.AddModelError(nameof(vm.CampusId),
                        "Please select a valid campus.");

                if (!ModelState.IsValid)
                {
                    vm.Campuses = await GetCampusOptionsAsync();
                    return View(vm);
                }

                // Create user — email NOT confirmed yet
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
                _logger.LogInformation(
                    "CreateAsync for {Email}: {Ok}. Errors: {Errs}",
                    user.Email, result.Succeeded,
                    string.Join("; ", result.Errors.Select(e => e.Description)));

                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    vm.Campuses = await GetCampusOptionsAsync();
                    return View(vm);
                }

                await _userManager.AddToRoleAsync(user, "Student");

                // Generate token and build the verification link
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, token },
                    Request.Scheme)!;

                _logger.LogInformation(
                    "Verification link for {Email}: {Link}", user.Email, confirmLink);

                // Send via SendGrid
                var sent = await _email.SendEmailVerificationAsync(user, confirmLink);

                if (sent)
                {
                    // Normal path: redirect to "check your inbox" page
                    return RedirectToAction("VerificationSent",
                        new { email = user.Email });
                }

                // Fallback: SendGrid not yet configured — auto-confirm so
                // the user isn't locked out during development/testing.
                // The link is still logged above for admin reference.
                _logger.LogWarning(
                    "SendGrid not configured or send failed. " +
                    "Auto-confirming {Email} so they can sign in.", user.Email);

                await _userManager.ConfirmEmailAsync(user, token);
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] =
                    $"Welcome, {user.DisplayUsername}! " +
                    "Your account is active. " +
                    "(Verification email could not be sent — please configure SendGrid.)";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty,
                    "A system error occurred. Please try again.");
            }

            vm.Campuses = await GetCampusOptionsAsync();
            return View(vm);
        }

        
        [HttpGet]
        public IActionResult VerificationSent(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        
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
                // Sign in immediately after verifying
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] =
                    $"Welcome to LiteratureClub, {user.DisplayUsername}! " +
                    "Your email is verified and you're now signed in.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error =
                "This verification link is invalid or has expired. " +
                "Please request a new one below.";
            return View("ConfirmEmailResult");
        }

        
        [HttpGet]
        public IActionResult ResendVerification() => View();

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(string email)
        {
            ViewBag.Sent = true; // always show success to prevent email enumeration

            var user = await _userManager.FindByEmailAsync(email?.Trim() ?? "");
            if (user != null && !user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, token },
                    Request.Scheme)!;

                _logger.LogInformation(
                    "Resend verification for {Email}: {Link}", user.Email, confirmLink);

                await _email.SendEmailVerificationAsync(user, confirmLink);
            }

            return View();
        }

        
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        
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
                    ModelState.AddModelError(string.Empty,
                        "Please verify your email before signing in. " +
                        "Check your inbox for the verification link.");
                    ViewData["UnverifiedEmail"] = user.Email;
                    return View(vm);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user, vm.Password, vm.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} signed in.", vm.Email);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty,
                        "Account locked after too many failed attempts. " +
                        "Try again in 5 minutes.");
                    return View(vm);
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty,
                    "A system error occurred. Please try again.");
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult AccessDenied() => View();

        
        private async Task<List<CampusOption>> GetCampusOptionsAsync()
        {
            try
            {
                return await _context.Campuses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.University)
                    .ThenBy(c => c.Name)
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