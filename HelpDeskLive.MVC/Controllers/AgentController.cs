using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers
{
    public class AgentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
