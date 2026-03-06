using Artwork_Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GalleryController : ControllerBase
    {
        [HttpGet]
        public IActionResult CheckGallery()
        {
            try
            {
                var _gallery = new List<string> { "Gallery alive!" };

                return Ok(_gallery);
            }
            catch(Exception err)
            {
                Console.WriteLine("Error! Check this: " + err);
                return StatusCode(500, "There are some errors from server!");
            }
        }
    }
}
