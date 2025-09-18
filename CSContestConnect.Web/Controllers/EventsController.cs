using CSContestConnect.Web.Data;
using CSContestConnect.Web.Models;
using CSContestConnect.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageStore _imageStore;          // use interface
        private readonly IWebHostEnvironment _env;

        public EventsController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            IImageStore imageStore,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _imageStore = imageStore;
            _env = env;
        }

        // Public: only Approved shown
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var items = await _db.Events
                .Where(e => e.ApprovalStatus == EventApprovalStatus.Approved)
                .OrderBy(e => e.StartsAt)
                .ToListAsync();

            return View(items);
        }

        // Public details: approved only; creators & admins can see their own pending/rejected
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _db.Events.Include(e => e.CreatedBy).FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            var canViewNonApproved = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var uid = _userManager.GetUserId(User);
                var isAdmin = User.IsInRole(IdentitySeed.RoleAdmin);
                canViewNonApproved = isAdmin || ev.CreatedById == uid;
            }

            if (ev.ApprovalStatus != EventApprovalStatus.Approved && !canViewNonApproved)
                return NotFound(); // hide unapproved events

            return View(ev);
        }

        // User posts
        [Authorize] // both User and Admin can create, but will go to Pending
        public IActionResult Create() => View(new Event
        {
            StartsAt = DateTime.UtcNow.AddDays(7),
            EndsAt = DateTime.UtcNow.AddDays(7).AddHours(4),
            Price = 0
        });

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model, IFormFile? CoverImage)
        {
            if (model.EndsAt <= model.StartsAt)
                ModelState.AddModelError(nameof(Event.EndsAt), "End time must be after start time.");
            if (model.TicketCapacity is < 0)
                ModelState.AddModelError(nameof(Event.TicketCapacity), "Capacity cannot be negative.");

            if (!ModelState.IsValid) return View(model);

            // Save cover image (uses IImageStore -> LocalImageStore)
            if (CoverImage != null && CoverImage.Length > 0)
            {
                // LocalImageStore implements a SaveEventImageAsync in your project
                // If your IImageStore doesn't declare it yet, add it there; otherwise call the concrete via 'as' cast.
                if (_imageStore is LocalImageStore concreteStore)
                {
                    model.ImagePath = await concreteStore.SaveEventImageAsync(CoverImage, _env.WebRootPath, model.ImagePath);
                }
                else
                {
                    // Fallback: save under profiles path if interface lacks event method
                    model.ImagePath = await _imageStore.SaveProfileImageAsync(CoverImage!, _env.WebRootPath, model.ImagePath);
                }
            }

            var uid = _userManager.GetUserId(User)!;
            model.CreatedById = uid;
            model.ApprovalStatus = EventApprovalStatus.Pending;
            model.CreatedAt = DateTime.UtcNow;

            _db.Events.Add(model);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Event submitted for approval. An admin will review it soon.";
            return RedirectToAction(nameof(My));
        }

        // Creators see their events (all states)
        [Authorize]
        public async Task<IActionResult> My()
        {
            var uid = _userManager.GetUserId(User)!;
            var items = await _db.Events
                .Where(e => e.CreatedById == uid)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(items);
        }

        // Register — enforces capacity
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(x => x.Id == id);
            if (ev == null) return NotFound();

            if (ev.ApprovalStatus != EventApprovalStatus.Approved)
            {
                TempData["Msg"] = "This event is not open for registration.";
                TempData["MsgType"] = "warning";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Requires Event model to have TicketCapacity, RegisteredCount, and IsFull
            if (ev.TicketCapacity.HasValue && ev.RegisteredCount >= ev.TicketCapacity.Value)
            {
                TempData["Msg"] = "Tickets are sold out.";
                TempData["MsgType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            ev.RegisteredCount += 1;
            await _db.SaveChangesAsync();

            TempData["Msg"] = "You are registered!";
            TempData["MsgType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
