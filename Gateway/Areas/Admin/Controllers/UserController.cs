using ITELECTIVE_SSO.Data;
using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SsoDbContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            SsoDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;

            var query = _userManager.Users.OrderBy(u => u.Email);

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        // GET: /Admin/Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Users/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            string Email,
            string Password,
            string ConfirmPassword)
        {
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }

            var existingUser = await _userManager.FindByEmailAsync(Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        // GET: /Admin/Users/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: /Admin/Users/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Users/ToggleActive/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
            {
                return Json(new { success = true, isActive = user.IsActive });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/Users/ResetPassword/{id} endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var tempPassword = GenerateTemporaryPassword();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = errors });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "PasswordReset",
                Details = $"Password was reset for user '{user.Email}' by an administrator.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, tempPassword });
        }

        private static string GenerateTemporaryPassword()
        {
            const string lowercase = "abcdefghijkmnpqrstuvwxyz";
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";

            var random = Random.Shared;

            var guaranteedChars = new[]
            {
                lowercase[random.Next(lowercase.Length)],
                uppercase[random.Next(uppercase.Length)],
                digits[random.Next(digits.Length)],
                digits[random.Next(digits.Length)]
            };

            var allChars = lowercase + uppercase + digits;
            var passwordChars = guaranteedChars
                .Concat(Enumerable.Range(0, 6).Select(_ => allChars[random.Next(allChars.Length)]))
                .ToArray();

            for (int i = passwordChars.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
            }

            return new string(passwordChars);
        }
    }
}