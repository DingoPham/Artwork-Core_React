using Artwork_Core.Data;
using Artwork_Core.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Artwork_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IPostgresSqlConnection _db;

        public UsersController(IPostgresSqlConnection db)
        {
            _db = db;
        }

        // GET: /api/users
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = new List<User>();

            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"SELECT ""id"", ""username"", ""email"", ""role"" 
                                   FROM master.""Users""";

            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3)
                });
            }

            return Ok(users);
        }

        // GET: /api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"SELECT ""id"", ""username"", ""email"", ""role""
                                   FROM master.""Users""
                                   WHERE ""id"" = @id";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3)
                });
            }

            return NotFound();
        }

        // POST: /api/users
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"INSERT INTO master.""Users""
                                   (""username"", ""email"", ""role"")
                                   VALUES (@username, @email, @role)
                                   RETURNING ""id"", ""username"", ""email"", ""role"";";

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@role", user.Role ?? "user");

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3)
                });
            }

            return BadRequest();
        }

        // PUT: /api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] User user)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"UPDATE master.""Users""
                                   SET ""username"" = @username,
                                       ""email"" = @email,
                                       ""role"" = @role
                                   WHERE ""id"" = @id
                                   RETURNING ""id"", ""username"", ""email"", ""role"";";

            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@role", user.Role);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3)
                });
            }

            return NotFound();
        }

        // DELETE: /api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            const string query = @"DELETE FROM master.""Users""
                                   WHERE ""id"" = @id";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();

            return Ok(new { message = "User deleted successfully" });
        }
    }
}