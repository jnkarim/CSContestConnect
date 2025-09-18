using CSContestConnect.Web.Data;
using CSContestConnect.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Controllers
{
    [Authorize(Roles = IdentitySeed.RoleAdmin)]
    public class AdminEventsController : Controller
    {
        private readonly AppDbContext _db;

        public AdminEventsController(AppDbContext db)
        {
            _db = db;
        }

        // ----- Approvals (Pending) -----
        public async Task<IActionResult> Pending()
        {
            var items = await _db.Events
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == EventApprovalStatus.Pending)
                .OrderBy(e => e.StartsAt)
                .ToListAsync();

            ViewData["Active"] = "Approvals";
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
            if (ev == null) return NotFound();

            if (ev.ApprovalStatus == EventApprovalStatus.Approved)
            {
                TempData["Msg"] = $"Already approved: #{ev.Id} — {ev.Title}";
                TempData["MsgType"] = "info";
                return RedirectToAction(nameof(Pending));
            }

            if (ev.ApprovalStatus != EventApprovalStatus.Pending &&
                ev.ApprovalStatus != EventApprovalStatus.Rejected)
            {
                TempData["Msg"] = $"Invalid state to approve: #{ev.Id}";
                TempData["MsgType"] = "warning";
                return RedirectToAction(nameof(Pending));
            }

            ev.ApprovalStatus = EventApprovalStatus.Approved;
            ev.ApprovedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Msg"] = $"Approved: #{ev.Id} — {ev.Title}";
                TempData["MsgType"] = "success";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Msg"] = "Save failed due to a concurrency conflict. Try again.";
                TempData["MsgType"] = "danger";
            }

            // Came from Pending list
            return RedirectToAction(nameof(Pending));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
            if (ev == null) return NotFound();

            if (ev.ApprovalStatus != EventApprovalStatus.Pending)
            {
                TempData["Msg"] = $"Only pending events can be rejected. #{ev.Id} is {ev.ApprovalStatus}.";
                TempData["MsgType"] = "warning";
                return RedirectToAction(nameof(Pending));
            }

            ev.ApprovalStatus = EventApprovalStatus.Rejected;
            ev.ApprovedAt = null;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Msg"] = $"Rejected: #{ev.Id} — {ev.Title}";
                TempData["MsgType"] = "warning";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Msg"] = "Save failed due to a concurrency conflict. Try again.";
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction(nameof(Pending));
        }

        // ----- Rejected list -----
        public async Task<IActionResult> Rejected()
        {
            var items = await _db.Events
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == EventApprovalStatus.Rejected)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewData["Active"] = "Rejected";
            return View(items);
        }

        // From Rejected -> back to Pending (reconsider)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
            if (ev == null) return NotFound();

            if (ev.ApprovalStatus != EventApprovalStatus.Rejected)
            {
                TempData["Msg"] = $"Only rejected events can be restored. #{ev.Id} is {ev.ApprovalStatus}.";
                TempData["MsgType"] = "warning";
                return RedirectToAction(nameof(Rejected));
            }

            ev.ApprovalStatus = EventApprovalStatus.Pending;
            ev.ApprovedAt = null;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Msg"] = $"Restored to Pending: #{ev.Id} — {ev.Title}";
                TempData["MsgType"] = "info";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Msg"] = "Save failed due to a concurrency conflict. Try again.";
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction(nameof(Rejected));
        }

        // From Rejected -> Approve directly
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveFromRejected(int id)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
            if (ev == null) return NotFound();

            if (ev.ApprovalStatus != EventApprovalStatus.Rejected)
            {
                TempData["Msg"] = $"Only rejected events can be approved here. #{ev.Id} is {ev.ApprovalStatus}.";
                TempData["MsgType"] = "warning";
                return RedirectToAction(nameof(Rejected));
            }

            ev.ApprovalStatus = EventApprovalStatus.Approved;
            ev.ApprovedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Msg"] = $"Approved: #{ev.Id} — {ev.Title}";
                TempData["MsgType"] = "success";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Msg"] = "Save failed due to a concurrency conflict. Try again.";
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction(nameof(Rejected));
        }
    }
}
