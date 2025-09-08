using Microsoft.AspNetCore.Mvc;

namespace CSContestConnect.Web.Controllers
{
    public class AdminController : Controller
    {
        // Admin Dashboard
        public IActionResult Index()
        {
            return View(); // Admin dashboard view
        }

        // Events Page
        public IActionResult Events()
        {
            return View(); // Events view (events.cshtml)
        }

        // Users Page
        public IActionResult Users()
        {
            return View(); // Users view (users.cshtml)
        }

        // Reviews Page
        public IActionResult Reviews()
        {
            return View(); // Reviews view (reviews.cshtml)
        }

        // Optional: Logout (redirect to login)
        public IActionResult Logout()
        {
            // Clear session or authentication cookie if needed
            return RedirectToAction("Login", "Account");
        }
    }
}
