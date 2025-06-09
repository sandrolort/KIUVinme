using Microsoft.AspNetCore.Mvc;

namespace KiuWho.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Ads()
    {
        return View();
    }
}