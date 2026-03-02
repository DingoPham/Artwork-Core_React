using Artwork_Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GalleryController : ControllerBase
    {
        // Fake database
        private static List<GalleryItem> _gallery = new()
        {
            new GalleryItem { Id = 1, Title = "Dragon Art", Type = "Illustration" },
            new GalleryItem { Id = 2, Title = "Intro Animation", Type = "Animation" }
        };

        // GET: api/gallery
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_gallery);
        }

        // GET: api/gallery/1
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var item = _gallery.FirstOrDefault(x => x.Id == id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        // POST: api/gallery
        [HttpPost]
        public IActionResult Create(GalleryItem newItem)
        {
            newItem.Id = _gallery.Max(x => x.Id) + 1;
            _gallery.Add(newItem);
            return CreatedAtAction(nameof(GetById), new { id = newItem.Id }, newItem);
        }

        // PUT: api/gallery/1
        [HttpPut("{id}")]
        public IActionResult Update(int id, GalleryItem updatedItem)
        {
            var item = _gallery.FirstOrDefault(x => x.Id == id);
            if (item == null)
                return NotFound();

            item.Title = updatedItem.Title;
            item.Type = updatedItem.Type;

            return NoContent();
        }

        // DELETE: api/gallery/1
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var item = _gallery.FirstOrDefault(x => x.Id == id);
            if (item == null)
                return NotFound();

            _gallery.Remove(item);
            return NoContent();
        }
    }
}
