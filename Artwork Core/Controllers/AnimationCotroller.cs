using Artwork_Core.Data;
using Artwork_Core.Models;
using Artwork_Core.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimationController : ControllerBase
    {
        private readonly IPostgresSqlConnection _db;
        private readonly R2Service _r2;

        public AnimationController(IPostgresSqlConnection db, R2Service r2)
        {
            _db = db;
            _r2 = r2;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var animation = new List<Animation>();

            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = "SELECT \"id\", \"video_url\", \"title\" FROM master.\"Animation\"";

            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                animation.Add(new Animation
                {
                    Id = reader.GetInt32(0),
                    VideoUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }
            return Ok(animation);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Animation illustration)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"INSERT INTO master.""Animation"" (""video_url"", ""title"") 
                                   VALUES (@video_url, @title) 
                                   RETURNING ""id"", ""video_url"", ""title"";";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@video_url", illustration.VideoUrl);
            command.Parameters.AddWithValue("@title", illustration.Title);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new Animation
                {
                    Id = reader.GetInt32(0),
                    VideoUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }

            return BadRequest();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var VideoUrl = await _r2.UploadAnimation(file);

            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"INSERT INTO master.""Animation""
                           (""video_url"", ""title"")
                           VALUES (@url, @title)
                           RETURNING ""id"", ""video_url"", ""title"";";

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@url", VideoUrl);
            command.Parameters.AddWithValue("@title", file.FileName);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new Animation
                {
                    Id = reader.GetInt32(0),
                    VideoUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }

            return BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Animation animation)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"UPDATE master.""Animation""
                           SET ""video_url"" = @video_url,
                               ""title"" = @title
                           WHERE ""id"" = @id
                           RETURNING ""id"", ""video_url"", ""title"";";

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@video_url", animation.VideoUrl);
            command.Parameters.AddWithValue("@title", animation.Title);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new Animation
                {
                    Id = reader.GetInt32(0),
                    VideoUrl = reader.GetString(1),
                    Title = reader.GetString(2)
                });
            }

            return NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            // lấy url trước
            const string selectQuery = @"SELECT ""video_url"" 
                                 FROM master.""Animation"" 
                                 WHERE ""id"" = @id";

            await using var selectCommand = new NpgsqlCommand(selectQuery, connection);
            selectCommand.Parameters.AddWithValue("@id", id);

            var videoUrl = await selectCommand.ExecuteScalarAsync();

            if (videoUrl == null)
                return NotFound();

            // xóa file trên R2
            await _r2.Delete(videoUrl.ToString());

            // xóa DB
            const string deleteQuery = @"DELETE FROM master.""Animation"" 
                                 WHERE ""id"" = @id";

            await using var deleteCommand = new NpgsqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@id", id);

            await deleteCommand.ExecuteNonQueryAsync();

            return Ok(new { message = "Deleted successfully" });
        }
    }
}
