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
        private readonly IPostgresSqlConnection _db;

        public IllustrationController(IPostgresSqlConnection db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var illustrations = new List<Illustration>();

            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = "SELECT \"id\", \"image_url\", \"title\" FROM master.\"Illustrations\"";

            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                illustrations.Add(new Illustration
                {
                    Id = reader.GetInt32(0),
                    ImageUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }
            return Ok(illustrations);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Illustration illustration)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"INSERT INTO master.""Illustrations"" (""image_url"", ""title"") 
                                   VALUES (@image_url, @title) 
                                   RETURNING ""id"", ""image_url"", ""title"";";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@image_url", illustration.ImageUrl);
            command.Parameters.AddWithValue("@title", illustration.Title);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new Illustration
                {
                    Id = reader.GetInt32(0),
                    ImageUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }

            return BadRequest();
        }
        
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // tạo tên file unique
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            // đường dẫn lưu file
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);

            // lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/images/{fileName}";

            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"INSERT INTO master.""Illustrations"" (""image_url"", ""title"")
                           VALUES (@image_url, @title)
                           RETURNING ""id"", ""image_url"", ""title"";";

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@image_url", imageUrl);
            command.Parameters.AddWithValue("@title", file.FileName);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new Illustration
                {
                    Id = reader.GetInt32(0),
                    ImageUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }

            return BadRequest();
        }

        [HttpPut] 
        public async Task<IActionResult> Put([FromBody] Illustration illustration)
        {
            return Ok(illustration);
        }
    }
}
