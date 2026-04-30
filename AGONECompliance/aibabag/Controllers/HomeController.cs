using Microsoft.AspNetCore.Mvc;

namespace AiBabag.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
