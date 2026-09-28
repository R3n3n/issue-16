using Gateway.Models;
using Gateway.Services;
using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Gateway.Controllers
{
    // ===== TASK 1: Create AuthController (public area, no auth required) =====
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private const string InvalidCredentialsMessage = "Invalid email or password.";
        private const string SuspendedMessage = "Account Suspended";
        private const string LockedOutMessage = "Too many failed login attempts. Please try again in 15 minutes.";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IReturnUrlValidator _returnUrlValidator;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditService _auditService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IReturnUrlValidator returnUrlValidator,
            IJwtTokenService jwtTokenService,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _returnUrlValidator = returnUrlValidator;
            _jwtTokenService = jwtTokenService;
            _auditService = auditService;
        }
        // ===== END TASK 1 =====

        // ===== TASK 2: GET /Auth/Login?returnUrl=xxx - show login form with hidden returnUrl =====
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl)   // ← ISSUE 9 TASK 2: returnUrl is bound from the query string here
        {
            // ===== TASK 3: Validate returnUrl against approved apps =====
            var app = await _returnUrlValidator.ValidateAsync(returnUrl, GetClientIp());
            // ===== END TASK 3 =====

            // ===== TASK 4: Show error if returnUrl invalid =====
            if (app == null)
            {
                return View("UnapprovedApp");
            }
            // ===== END TASK 4 =====

            return View(new LoginViewModel { ReturnUrl = returnUrl, AppName = app.Name });
        }
        // ===== END TASK 2 =====

        // ===== TASK 5: POST /Auth/Login - validate credentials via SignInManager =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var ip = GetClientIp();

            // The hidden returnUrl field can be tampered with, so re-validate on every POST.
            var app = await _returnUrlValidator.ValidateAsync(model.ReturnUrl, ip);
            if (app == null)
            {
                return View("UnapprovedApp");
            }
            model.AppName = app.Name;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // ===== TASK 12: Log all login attempts to AuditLogs =====
                await _auditService.LogLoginAsync(null, model.Email, false, "Unknown email", ip);
                // ===== TASK 10: Handle failed login with clear error messages =====
                ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
                return View(model);
            }

            // ===== TASK 11: Rate limiting - lockoutOnFailure counts failures (5 attempts, 15 min lock; see Program.cs) =====
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            // ===== END TASK 5 =====

            if (signInResult.IsLockedOut)
            {
                await _auditService.LogLoginAsync(user.Id, model.Email, false, "Account locked out", ip);
                ModelState.AddModelError(string.Empty, LockedOutMessage);
                return View(model);
            }
            // ===== END TASK 11 =====

            if (!signInResult.Succeeded)
            {
                await _auditService.LogLoginAsync(user.Id, model.Email, false, "Invalid password", ip);
                ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
                return View(model);
            }
            // ===== END TASK 10 =====

            // ===== TASK 6: Check IsActive flag =====
            // ISSUE 5: "Verify login rejects inactive users" + "Show Account Suspended message".
            // Runs AFTER the password check, so the message is only shown to someone who knows the password.
            if (!user.IsActive)
            {
                await _auditService.LogLoginAsync(user.Id, model.Email, false, "Account suspended", ip);
                ModelState.AddModelError(string.Empty, SuspendedMessage);
                return View(model);
            }
            // ===== END TASK 6 =====

            // TASKS 7 & 8 live in JwtTokenService
            var token = await _jwtTokenService.CreateTokenAsync(user, app);

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _auditService.LogLoginAsync(user.Id, user.Email!, true, null, ip);
            // ===== END TASK 12 =====

            // ===== TASK 9: Redirect to returnUrl?token=jwt =====
            return Redirect(QueryHelpers.AddQueryString(app.ReturnUrl, "token", token));
            // ===== END TASK 9 =====
        }

        private string? GetClientIp() => HttpContext?.Connection?.RemoteIpAddress?.ToString();
    }
}