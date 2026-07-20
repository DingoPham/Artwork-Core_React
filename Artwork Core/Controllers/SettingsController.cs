using Microsoft.AspNetCore.Mvc;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : Controller
    {
        [HttpGet]
        public IActionResult CheckSettings()
        {
            try
            {
                var _settings = new List<string> { "Settings alive!" };
                return Ok(_settings);

            }
            catch (Exception err)
            {
                Console.WriteLine("Error! Check this: " + err);
                return StatusCode(500, "There are some errors from server!");
            }
        }
    }
}