using CSContestConnect.Web.Data;
using CSContestConnect.Web.Models;
using CSContestConnect.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Controllers   // IMPORTANT
{
    [Authorize(Roles = IdentitySeed.RoleAdmin)]
    public class AdminController : Controller  // IMPORTANT name ends with "Controller"
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) => _db = db;

        [HttpGet]                                // GET /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var nowUtc = DateTime.UtcNow;
            var weekAgo = nowUtc.AddDays(-7);

            var vm = new AdminDashboardVm
            {
                Total = await _db.Events.CountAsync(),
                Pending = await _db.Events.CountAsync(e => e.ApprovalStatus == EventApprovalStatus.Pending),
                Approved = await _db.Events.CountAsync(e => e.ApprovalStatus == EventApprovalStatus.Approved),
                Rejected = await _db.Events.CountAsync(e => e.ApprovalStatus == EventApprovalStatus.Rejected),
                NewThisWeek = await _db.Events.CountAsync(e => e.CreatedAt >= weekAgo),
                ApprovedThisWeek = await _db.Events.CountAsync(e =>
                    e.ApprovalStatus == EventApprovalStatus.Approved && e.ApprovedAt != null && e.ApprovedAt >= weekAgo),
                Latest = await _db.Events.Include(e => e.CreatedBy)
                                         .OrderByDescending(e => e.CreatedAt).Take(8).ToListAsync(),
                UpcomingApproved = await _db.Events.Include(e => e.CreatedBy)
                                         .Where(e => e.ApprovalStatus == EventApprovalStatus.Approved && e.StartsAt >= nowUtc)
                                         .OrderBy(e => e.StartsAt).Take(8).ToListAsync()
            };

            ViewData["Active"] = "AdminDashboard";
            return View(vm);                     // looks for Views/Admin/Dashboard.cshtml
        }
    }
}
