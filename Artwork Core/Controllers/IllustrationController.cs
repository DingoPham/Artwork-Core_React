using Artwork_Core.Data;
using Artwork_Core.Models;
using Artwork_Core.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IllustrationController : ControllerBase
    {
        private readonly IPostgresSqlConnection _db;
        private readonly R2Service _r2;

        public IllustrationController(IPostgresSqlConnection db, R2Service r2)
        {
            _db = db;
            _r2 = r2;
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
            var imageUrl = await _r2.Upload(file);

            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"INSERT INTO master.""Illustrations""
                           (""image_url"", ""title"")
                           VALUES (@url, @title)
                           RETURNING ""id"", ""image_url"", ""title"";";

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@url", imageUrl);
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
