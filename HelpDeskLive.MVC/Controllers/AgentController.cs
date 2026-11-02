
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public class AgentController : Controller
{
    public IActionResult Index()
    {
        var accessToken = HttpContext.Session.GetString("AccessToken");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToAction("Login", "Account");
        }

        if (userRole != "Agent")
        {
            return Forbid();
        }

        return View();
    }
}
