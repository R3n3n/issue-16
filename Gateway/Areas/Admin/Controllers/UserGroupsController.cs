using Gateway.Areas.Admin.Models;
using ITELECTIVE_SSO.Data;
using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Users/{userId}/Groups")]
    public class UserGroupsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SsoDbContext _context;

        public UserGroupsController(UserManager<ApplicationUser> userManager, SsoDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("Available")]
        public async Task<IActionResult> Available(string userId)
        {
            var assignedGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var availableGroups = await _context.Groups
                .Where(g => !assignedGroupIds.Contains(g.Id))
                .Include(g => g.TenantApp)
                .Select(g => new
                {
                    groupId = g.Id,
                    name = g.Name,
                    appName = g.TenantApp.Name
                })
                .ToListAsync();

            return Json(availableGroups);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var assignedGroups = await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Group)
                    .ThenInclude(g => g.TenantApp)
                .Select(ug => new
                {
                    groupId = ug.Group.Id,
                    name = ug.Group.Name,
                    appName = ug.Group.TenantApp.Name,
                    powerLevel = ug.Group.PowerLevel
                })
                .ToListAsync();

            return Json(assignedGroups);
        }

        // POST /Admin/Users/{userId}/Groups
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(string userId, AssignGroupViewModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var group = await _context.Groups.FindAsync(model.GroupId);
            if (group == null)
            {
                return NotFound();
            }

            var alreadyAssigned = await _context.UserGroups
                .AnyAsync(ug => ug.UserId == userId && ug.GroupId == model.GroupId);

            if (alreadyAssigned)
            {
                TempData["ErrorMessage"] = "User is already assigned to this group.";
                return RedirectToAction("Details", "Users", new { id = userId });
            }

            _context.UserGroups.Add(new UserGroup
            {
                UserId = userId,
                GroupId = model.GroupId
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User assigned to group '{group.Name}'.";
            return RedirectToAction("Details", "Users", new { id = userId });
        }

        [HttpDelete("{groupId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(string userId, int groupId)
        {
            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);

            if (userGroup == null)
            {
                return NotFound();
            }

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}