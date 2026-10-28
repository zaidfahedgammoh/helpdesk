using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
