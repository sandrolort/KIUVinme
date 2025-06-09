using Microsoft.AspNetCore.Mvc;

namespace KiuWho.Controllers; 

public class ChatController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}