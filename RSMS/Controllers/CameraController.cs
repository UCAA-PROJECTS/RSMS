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
                {"ILS002","http://172.29.100.10:8889/loc_camera" },
                {"DVOR003", "http://172.29.100.10:8889/dvor_camera" },
                {"GP001", "http://172.29.100.10:8889/gp_camera" }
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
