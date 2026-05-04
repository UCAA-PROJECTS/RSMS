using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RSMS.Models;

namespace RSMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
// function to return partial view containing notifications based on type (alarm, warning, or all)
public IActionResult GetNotifications(string type)
{
    var notifications = new List<string>();

    if (type == "alarm")
    {
        notifications.Add("Alarm: Gulu power failure");
        notifications.Add("Alarm: Soroti battery low");
    }
    else if (type == "warning")
    {
        notifications.Add("Warning: Buwaya temperature high");
    }
    else
    {
        notifications.Add("Alarm: Gulu power failure");
        notifications.Add("Warning: Buwaya temperature high");
    }

    return PartialView("_NotificationsPartial", notifications);
}        

public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
