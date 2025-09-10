using System.Security.Claims;
using CSContestConnect.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CSContestConnect.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // -------- Register (existing) --------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return View(model);
        }

        // -------- Login (existing) --------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? "/";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Email,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // ======== NEW: Google External Login ========

        // 1) Kick off Google OAuth challenge
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            // provider should be "Google"
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl!);
            return Challenge(props, provider);
        }

        // 2) Handle Google callback -> create/sign-in local user
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string? remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                TempData["ErrorMessage"] = $"External provider error: {remoteError}";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Login info not found.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // If already linked, just sign in
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
                return LocalRedirect(SafeReturnUrl(returnUrl));

            // No linked account: create (or reuse) by email, then link
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Google account has no email.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true // you can enforce confirmation later
                };
                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join("; ", createRes.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }
            }

            var linkRes = await _userManager.AddLoginAsync(user, info);
            if (!linkRes.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", linkRes.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        private string SafeReturnUrl(string? returnUrl)
            => (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) ? returnUrl! : "/";

        // -------- Logout (existing) --------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
