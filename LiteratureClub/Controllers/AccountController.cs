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
            // Regex validations
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
                var trimmedEmail = vm.Email.Trim();
                var trimmedUsername = vm.DisplayUsername.Trim();
                var trimmedStudentNumber = vm.StudentNumber.Trim();

                // Uniqueness checks (Using database collation safety)
                if (await _userManager.FindByEmailAsync(trimmedEmail) != null)
                    ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");

                if (await _userManager.Users.AnyAsync(u => u.StudentNumber == trimmedStudentNumber))
                    ModelState.AddModelError(nameof(vm.StudentNumber), "This student number is already registered.");

                if (await _userManager.Users.AnyAsync(u => u.DisplayUsername == trimmedUsername))
                    ModelState.AddModelError(nameof(vm.DisplayUsername), "This username is already taken.");

                var campus = await _context.Campuses.FirstOrDefaultAsync(c => c.Id == vm.CampusId && c.IsActive);
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
                    DisplayUsername = trimmedUsername,
                    StudentNumber = trimmedStudentNumber,
                    Email = trimmedEmail.ToLower(),
                    UserName = trimmedEmail.ToLower(),
                    City = vm.City.Trim(),
                    CampusId = vm.CampusId,
                    IsActive = true,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, vm.Password);

                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);

                    vm.Campuses = await GetCampusOptionsAsync();
                    return View(vm);
                }

                await _userManager.AddToRoleAsync(user, "Student");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme)!;

                _logger.LogInformation("Verification link generated for {Email}", user.Email);

                var sent = await _email.SendEmailVerificationAsync(user, confirmLink);
                if (sent)
                {
                    return RedirectToAction("VerificationSent", new { email = user.Email });
                }

                // Dev Fallback
                _logger.LogWarning("Email delivery failed. Auto-confirming user for fallback loop.");
                await _userManager.ConfirmEmailAsync(user, token);
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["Success"] = $"Welcome, {user.DisplayUsername}! Your account is active. (Verification email skipped)";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration runtime crash for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty, "A system error occurred. Please try again.");
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
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Welcome to LiteratureClub, {user.DisplayUsername}! Your email is verified.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "This verification link is invalid or has expired.";
            return View("ConfirmEmailResult");
        }

        [HttpGet]
        public IActionResult ResendVerification() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(string email)
        {
            ViewBag.Sent = true;

            var user = await _userManager.FindByEmailAsync(email?.Trim() ?? "");
            if (user != null && !user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme)!;
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
                // 1. Find the user by their email field first using UserManager
                var user = await _userManager.FindByEmailAsync(vm.Email.Trim());

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(vm);
                }

                // 2. Use the exact username stored in the DB record to sign in safely
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    vm.Password,
                    vm.RememberMe,
                    lockoutOnFailure: true
                );

                if (result.Succeeded)
                {
                    if (!user.IsActive)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "This account has been suspended.");
                        return View(vm);
                    }

                    _logger.LogInformation("User logged in successfully.");
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Account locked out. Try again in 5 minutes.");
                    return View(vm);
                }

                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Please verify your email address before logging in.");
                    return View(vm);
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failure trace for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty, "A system error occurred.");
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                var user = await _userManager.FindByEmailAsync(vm.Email.Trim());

                if (user != null)
                {
                    // 1. Generate the secure token using ASP.NET Core Identity
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                    // 2. Build the callback verification link pointing to our ResetPassword GET action
                    var callbackUrl = Url.Action("ResetPassword", "Account",
                        new { token, email = user.Email },
                        protocol: Request.Scheme)!;

                    _logger.LogInformation("Password reset token generated for {Email}", user.Email);

                    // 3. Dispatch the message utilizing your existing EmailService infrastructure
                    // Note: Update the method name below to match whatever helper method your EmailService exposes
                    await _email.SendPasswordResetLinkAsync(user, callbackUrl);
                }

                // Security Best Practice: Always redirect to confirmation even if the email doesn't exist
                // This prevents malicious actors from scanning which student emails are registered.
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password token generation crash for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty, "A system error occurred. Please try again.");
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                _logger.LogWarning("ResetPassword accessed with missing routing tokens.");
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                var user = await _userManager.FindByEmailAsync(vm.Email.Trim());
                if (user == null)
                {
                    // Redirect silently to prevent account enumeration
                    return RedirectToAction("ResetPasswordConfirmation");
                }

                // Identity decodes and verifies the cryptographic token against the database stamp
                var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user {Email}", user.Email);
                    TempData["Success"] = "Your password has been reset successfully. Please log in.";
                    return RedirectToAction("ResetPasswordConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword runtime processing exception for {Email}", vm.Email);
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password.");
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

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
                _logger.LogError(ex, "Failed to load active campuses subset.");
                return new List<CampusOption>();
            }
        }
    }
}