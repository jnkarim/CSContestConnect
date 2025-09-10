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

        // -------- Register --------
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
                // Assign a default "User" role upon registration
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return View(model);
        }

        // -------- Standard Login --------
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

        // -------- Google External Login --------
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl!);
            return Challenge(props, provider);
        }

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

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
                return LocalRedirect(SafeReturnUrl(returnUrl));

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
                    EmailConfirmed = true
                };
                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join("; ", createRes.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }
                // Also assign the "User" role to new Google sign-ups
                await _userManager.AddToRoleAsync(user, "User");
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

        // -------- Logout --------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // -------- Admin Login --------

        // GET: /Account/AdminLogin
        // Displays the custom admin login page.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminLogin()
        {
            return View();
        }

        // POST: /Account/AdminLogin
        // Handles the form submission from the admin login page.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // A stronger password that meets default Identity requirements.
            const string adminPassword = "Admin@123456";

            // Check for hardcoded admin credentials.
            if (model.Email == "admin@gmail.com" && model.Password == adminPassword)
            {
                // Find or create the admin user to maintain a valid session principal.
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                    // Use the strong password when creating the user for the first time.
                    var createResult = await _userManager.CreateAsync(user, adminPassword);

                    if (!createResult.Succeeded)
                    {
                        // If creation fails, display the errors. This helps in debugging.
                        foreach (var error in createResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewData["ErrorMessage"] = "Could not create the default admin user. See errors for details.";
                        return View(model);
                    }
                    // Assign the 'Admin' role to the newly created user.
                    await _userManager.AddToRoleAsync(user, "Admin");
                }

                // Sign in the user.
                await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
                
                // SUCCESS: Redirect to the main admin dashboard.
                return RedirectToAction("Index", "Admin"); 
            }
            
            // If credentials do not match the hardcoded values.
            ViewData["ErrorMessage"] = "Invalid admin email or password.";
            return View(model);
        }
    }
}

