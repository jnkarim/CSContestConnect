using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSContestConnect.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        [Authorize]
        public IActionResult Dashboard() => View();
    }
}
