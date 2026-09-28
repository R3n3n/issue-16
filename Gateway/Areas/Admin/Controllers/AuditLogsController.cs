using Gateway.Models;
using ITELECTIVE_SSO.Data;
using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuditLogsController : Controller
    {
        private readonly SsoDbContext _context;
        private const int PageSize = 20;

        public AuditLogsController(SsoDbContext context)
        {
            _context = context;
        }

        // GET /Admin/AuditLogs
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int pageIndex = 1)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var inclusiveToDate = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.Timestamp < inclusiveToDate);
            }

            query = query.OrderByDescending(a => a.Timestamp);

            var logs = await PaginatedList<AuditLog>.CreateAsync(query, pageIndex, PageSize);

            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            return View(logs);

            return View(new PaginatedList<AuditLog>(new List<AuditLog>(), 0, 1, PageSize));
        }
    }
}