using Artwork_Core.Data;
using Artwork_Core.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IllustrationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IllustrationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _context.Illustrations.ToListAsync();
            return Ok(data);
        }
    }
}
