using Microsoft.AspNetCore.Mvc;

namespace aibabag.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
