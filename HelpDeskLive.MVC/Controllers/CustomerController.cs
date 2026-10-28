using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
