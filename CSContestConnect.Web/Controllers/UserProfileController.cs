// CSContestConnect.Web/Controllers/UserProfileController.cs
using System;
using System.IO;
using System.Threading.Tasks;
using CSContestConnect.Web.Data;
using CSContestConnect.Web.Models;
using CSContestConnect.Web.Models.ViewModels;
using CSContestConnect.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSContestConnect.Web.Controllers
{
    [Authorize]
    [Route("Profile")]
    public class UserProfileController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageStore _imageStore;
        private readonly IWebHostEnvironment _env;

        public UserProfileController(
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

        private async Task<UserProfile> EnsureProfileAsync(ApplicationUser user)
        {
            if (user.UserProfileId.HasValue)
            {
                var existing = await _db.UserProfiles.FindAsync(user.UserProfileId.Value);
                if (existing != null) return existing;
            }

            var profile = new UserProfile
            {
                FullName = user.Email ?? "New User",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync();

            user.UserProfileId = profile.Id;
            await _userManager.UpdateAsync(user);

            return profile;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await EnsureProfileAsync(user);
            return View(profile);
        }

        [HttpGet("Edit")]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await EnsureProfileAsync(user);

            var vm = new EditUserProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                FullName = profile.FullName,
                Bio = profile.Bio,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                Phone = profile.Phone,
                Website = profile.Website,
                LinkedIn = profile.LinkedIn,
                GitHub = profile.GitHub,
                Country = profile.Country,
                City = profile.City,
                School = profile.School,
                College = profile.College,
                University = profile.University,
                Degree = profile.Degree,
                GraduationYear = profile.GraduationYear,
                CurrentImagePath = profile.ProfileImagePath
            };

            return View(vm);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await EnsureProfileAsync(user);

            if (model.ProfileImageFile != null)
            {
                try
                {
                    profile.ProfileImagePath = await _imageStore.SaveProfileImageAsync(
                        model.ProfileImageFile,
                        _env.WebRootPath,
                        profile.ProfileImagePath
                    );
                    model.CurrentImagePath = profile.ProfileImagePath;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.ProfileImageFile), ex.Message);
                    return View(model);
                }
            }

            // Map fields back to entity
            profile.FullName = model.FullName?.Trim() ?? profile.FullName;
            profile.Bio = model.Bio;
            // Convert DateOfBirth to UTC if specified
            profile.DateOfBirth = model.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(model.DateOfBirth.Value, DateTimeKind.Utc)
                : null;
            profile.Gender = model.Gender;
            profile.Phone = model.Phone;
            profile.Website = model.Website;
            profile.LinkedIn = model.LinkedIn;
            profile.GitHub = model.GitHub;
            profile.Country = model.Country;
            profile.City = model.City;
            profile.School = model.School;
            profile.College = model.College;
            profile.University = model.University;
            profile.Degree = model.Degree;
            profile.GraduationYear = model.GraduationYear;
            profile.UpdatedAtUtc = DateTime.UtcNow; // Already UTC

            await _db.SaveChangesAsync();

            TempData["ProfileSaved"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("DeleteProfileImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var profile = await EnsureProfileAsync(user);

            if (!string.IsNullOrWhiteSpace(profile.ProfileImagePath))
            {
                await _imageStore.DeleteProfileImageAsync(profile.ProfileImagePath, _env.WebRootPath);
                profile.ProfileImagePath = null;
                profile.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            TempData["ProfileSaved"] = "Profile image deleted successfully!";
            return RedirectToAction(nameof(Edit));
        }
    }
}