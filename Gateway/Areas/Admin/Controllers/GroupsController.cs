using Gateway.Areas.Admin.Models;
using ITELECTIVE_SSO.Data;
using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class GroupsController : Controller
    {
        private readonly SsoDbContext _context;

        public GroupsController(SsoDbContext context)
        {
            _context = context;
        }

        // GET /Admin/Groups
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.TenantApp)
                .OrderBy(g => g.TenantApp.Name)
                .ThenBy(g => g.PowerLevel)
                .ToListAsync();

            return View(groups);
        }

        // GET /Admin/Groups/Create
        public async Task<IActionResult> Create()
        {
            var model = new GroupViewModel
            {
                TenantApps = await GetTenantAppOptions()
            };

            return View(model);
        }

        private async Task<List<SelectListItem>> GetTenantAppOptions()
        {
            return await _context.TenantApps
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name
                })
                .ToListAsync();
        }

        // POST /Admin/Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupViewModel model)
        {
            var tenantApp = await _context.TenantApps.FindAsync(model.TenantAppId);
            if (tenantApp == null)
            {
                ModelState.AddModelError(nameof(model.TenantAppId), "Please select a valid app.");
            }

            if (!ModelState.IsValid)
            {
                model.TenantApps = await GetTenantAppOptions();
                return View(model);
            }

            var prefixedName = BuildPrefixedName(tenantApp!.Name, model.Name);

            var nameExists = await _context.Groups
                .AnyAsync(g => g.TenantAppId == model.TenantAppId && g.Name == prefixedName);

            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "A group with this name already exists for this app.");
                model.TenantApps = await GetTenantAppOptions();
                return View(model);
            }

            var group = new Group
            {
                TenantAppId = model.TenantAppId,
                Name = prefixedName,
                PowerLevel = model.PowerLevel,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Group '{group.Name}' was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Admin/Groups/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _context.Groups
                .Include(g => g.TenantApp)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var model = new GroupViewModel
            {
                Id = group.Id,
                TenantAppId = group.TenantAppId,
                Name = StripPrefix(group.Name, group.TenantApp.Name),
                PowerLevel = group.PowerLevel,
                TenantApps = await GetTenantAppOptions()
            };

            return View(model);
        }

        // POST /Admin/Groups/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GroupViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var tenantApp = await _context.TenantApps.FindAsync(model.TenantAppId);
            if (tenantApp == null)
            {
                ModelState.AddModelError(nameof(model.TenantAppId), "Please select a valid app.");
            }

            if (!ModelState.IsValid)
            {
                model.TenantApps = await GetTenantAppOptions();
                return View(model);
            }

            var prefixedName = BuildPrefixedName(tenantApp!.Name, model.Name);

            var nameExists = await _context.Groups
                .AnyAsync(g => g.TenantAppId == model.TenantAppId
                            && g.Name == prefixedName
                            && g.Id != id);

            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "A group with this name already exists for this app.");
                model.TenantApps = await GetTenantAppOptions();
                return View(model);
            }

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            group.TenantAppId = model.TenantAppId;
            group.Name = prefixedName;
            group.PowerLevel = model.PowerLevel;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Group '{group.Name}' was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Group '{group.Name}' was removed.";
            return RedirectToAction(nameof(Index));
        }

        private static string BuildPrefixedName(string appName, string groupName)
        {
            var baseName = StripPrefix(groupName, appName);
            return $"{appName}-{baseName}";
        }

        private static string StripPrefix(string name, string appName)
        {
            var prefix = $"{appName}-";
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? name[prefix.Length..]
                : name;
        }
    }
}