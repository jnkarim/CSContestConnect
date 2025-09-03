using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSContestConnect.Web.Controllers
{
    public class EventsController : Controller
    {
        // GET: /Events
        public IActionResult Index()
        {
            return View(); // renders Views/Events/Index.cshtml
        }

        // GET: /Events/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(); // renders Views/Events/Create.cshtml
        }
    }
}
