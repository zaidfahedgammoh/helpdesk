
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        var accessToken = HttpContext.Session.GetString("AccessToken");
        var userRole = HttpContext.Session.GetString("UserRole");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToAction("Login", "Account");
        }
        if (userRole != "Admin")
        {
            return Forbid();
        }

        return View();
    }
}
