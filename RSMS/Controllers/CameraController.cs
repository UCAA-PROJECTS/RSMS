using Microsoft.AspNetCore.Mvc;

namespace RSMS.Controllers
{
    public class CameraController : Controller
    {
        public IActionResult Live(string shelterCode)
        {

            ViewBag.ShelterCode = shelterCode;
            //IP Camera Urls
            var cameraFeeds = new Dictionary<string, string>
            {
                {"ILS002","" },
                {"DVOR003", "http://rsms:CaaRsms@10.10.40.249:8889/dvorShelter" },
                {"GP001", "" }
            };

            if (!cameraFeeds.ContainsKey(shelterCode)) 
            {
                return NotFound("Shelter code not found.");
            }

            ViewBag.CameraUrl = cameraFeeds[shelterCode];
            return View();
        }
    }
}
