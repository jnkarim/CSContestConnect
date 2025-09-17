using System.Net;
using System.Security.Claims;
using CSContestConnect.Web.Models;
using CSContestConnect.Web.Models.Auth;
using CSContestConnect.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace CSContestConnect.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IDisposableEmailService _disposableEmailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IDisposableEmailService disposableEmailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _disposableEmailService = disposableEmailService;
        }

        // -------- Register --------
        [HttpGet, AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Check for disposable email
            if (_disposableEmailService.IsDisposableEmail(model.Email))
            {
                ModelState.AddModelError("Email", "Please use a permanent email address. Temporary emails are not allowed.");
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                if (!existingUser.EmailConfirmed)
                {
                    // If user exists but email not confirmed, allow them to try again
                    ModelState.AddModelError("Email", "An account with this email already exists but is not verified. Please check your email for the confirmation link or request a new one.");
                    return View(model);
                }
                else
                {
                    ModelState.AddModelError("Email", "This email is already registered and verified. Please use a different email or try logging in.");
                    return View(model);
                }
            }

            // Create user with EmailConfirmed = false (this prevents login until verified)
            var user = new ApplicationUser 
            { 
                UserName = model.Email, 
                Email = model.Email,
                EmailConfirmed = false // This is crucial - prevents login until verified
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                
                var callbackUrl = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    protocol: Request.Scheme);

                var subject = "Confirm your email - CS Contest Connect";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #333;'>Welcome to CS Contest Connect!</h2>
                        <p>Thank you for registering! To complete your registration and start using your account, please verify your email address.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' style='background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Verify Email Address
                            </a>
                        </div>
                        <p><strong>Important:</strong> You cannot log in to your account until your email is verified.</p>
                        <p style='color: #666; font-size: 14px;'>If you didn't create this account, please ignore this email.</p>
                        <p style='color: #666; font-size: 12px;'>This link will expire in 2 hours for security reasons.</p>
                    </div>";

                await _emailService.SendEmailAsync(model.Email, subject, body);

                // Redirect to confirmation page instead of trying to login
                return RedirectToAction(nameof(RegisterConfirmation), new { email = model.Email });
            }

            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return View(model);
        }

        // -------- Register Confirmation --------
        [HttpGet, AllowAnonymous]
        public IActionResult RegisterConfirmation(string email)
        {
            ViewData["Email"] = email;
            return View();
        }

        // -------- Email Confirmation --------
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                TempData["ErrorMessage"] = "Invalid email confirmation link.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Login));
            }

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                TempData["InfoMessage"] = "Your email is already confirmed. You can log in.";
                return RedirectToAction(nameof(Login));
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Email confirmed successfully! You can now log in to your account.";
                return RedirectToAction(nameof(Login));
            }

            TempData["ErrorMessage"] = "Error confirming your email. The link may have expired. Please request a new confirmation email.";
            return RedirectToAction(nameof(ResendConfirmationEmail));
        }

        // -------- Login --------
        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? "/";
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            
            // Check if user exists
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Check if email is confirmed - THIS IS THE KEY CHECK
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Please verify your email address before logging in. Check your inbox for the verification email.");
                ViewData["ShowResendLink"] = true;
                ViewData["UserEmail"] = model.Email;
                return View(model);
            }

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

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // -------- Forgot Password --------
        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            
            // Only send password reset if user exists AND email is confirmed
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                
                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { email = model.Email, token = encodedToken },
                    protocol: Request.Scheme);

                var subject = "Reset your CS Contest Connect password";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #333;'>Password Reset Request</h2>
                        <p>We received a request to reset your password for your CS Contest Connect account.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{callbackUrl}' style='background-color: #dc3545; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Reset Password
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>If you didn't request this password reset, please ignore this email.</p>
                        <p style='color: #666; font-size: 12px;'>This link will expire in 2 hours for security reasons.</p>
                    </div>";
                
                await _emailService.SendEmailAsync(model.Email, subject, body);
            }

            // Always return the same confirmation message (security best practice)
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // -------- Reset Password --------
        [HttpGet, AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Decode the token immediately to check if it's valid
            try
            {
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                
                // Store the decoded token in ViewData to use in the form
                ViewData["DecodedToken"] = decodedToken;
                
                return View(new ResetPasswordViewModel 
                { 
                    Email = email, 
                    Token = token // Keep the encoded token for the form
                });
            }
            catch (FormatException)
            {
                TempData["ErrorMessage"] = "Invalid token format.";
                return RedirectToAction(nameof(ForgotPassword));
            }
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            try
            {
                // Decode the token from the form submission
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
                
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "Invalid token format. Please request a new password reset link.");
            }

            return View(model);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            TempData["SuccessMessage"] = "Your password has been reset. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        // -------- Resend Confirmation Email --------
        [HttpGet, AllowAnonymous]
        public IActionResult ResendConfirmationEmail() => View();

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email address.");
                return View();
            }

            // Check for disposable email again
            if (_disposableEmailService.IsDisposableEmail(email))
            {
                ModelState.AddModelError("", "Please use a permanent email address. Temporary emails are not allowed.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["SuccessMessage"] = "If your email is registered, you will receive a confirmation email.";
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["InfoMessage"] = "Your email is already confirmed. You can login.";
                return RedirectToAction(nameof(Login));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            
            var callbackUrl = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token = encodedToken },
                protocol: Request.Scheme);

            var subject = "Confirm your email - CS Contest Connect";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333;'>Confirm Your Email</h2>
                    <p>Please confirm your email address to complete your registration.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{callbackUrl}' style='background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Confirm Email Address
                        </a>
                    </div>
                    <p style='color: #666; font-size: 12px;'>This link will expire in 2 hours for security reasons.</p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);

            TempData["SuccessMessage"] = "Confirmation email sent. Please check your inbox.";
            return RedirectToAction(nameof(Login));
        }

        // -------- External Login (Google) --------
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl!);
            return Challenge(props, provider);
        }

        [HttpGet, AllowAnonymous]
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

            // Check for disposable email even for Google logins
            if (_disposableEmailService.IsDisposableEmail(email))
            {
                TempData["ErrorMessage"] = "Please use a permanent email address. Temporary emails are not allowed.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // For Google login, email is automatically verified since Google has verified it
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
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

        // -------- Logout --------
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // -------- Admin Login (Hardcoded) --------
        [HttpGet]
        public IActionResult AdminLogin()
        {
            ViewData["ErrorMessage"] = null;
            return View();
        }

        [HttpPost]
        public IActionResult AdminLogin(string Email, string Password)
        {
            // Hardcoded admin credentials
            const string adminEmail = "admin@gmail.com";
            const string adminPassword = "123456";

            if (Email == adminEmail && Password == adminPassword)
            {
                // You can set a temp cookie/session here if needed
                return RedirectToAction("Index", "Admin"); // Redirect to Admin panel
            }

            ViewData["ErrorMessage"] = "Invalid admin login attempt.";
            return View();
        }


    }
}