using Artwork_Core.Data;
using Artwork_Core.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using Npgsql;

using System.Security.Claims;

namespace Artwork_Core.Services;

public class AuthService : IAuthService
{
    private readonly IPostgresSqlConnection _connection;
    public AuthService(IPostgresSqlConnection connection)
    {
        _connection = connection;
    }
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        await using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        // Kiểm tra username hoặc email đã tồn tại chưa
        const string checkSql = @" SELECT COUNT(*) FROM master.""Users"" WHERE ""username"" = @username OR ""email"" = @email;";

        await using (var checkCmd = new NpgsqlCommand(checkSql, conn))
        {
            checkCmd.Parameters.AddWithValue("@username", request.Username);
            checkCmd.Parameters.AddWithValue("@email", request.Email);

            var count = (long)(await checkCmd.ExecuteScalarAsync())!;

            if (count > 0)
            {
                return new ConflictObjectResult(
                    "Username hoặc Email đã tồn tại.");
            }
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        const string insertSql = @"INSERT INTO master.""Users"" ( ""username"", ""email"", ""password_hash"", ""role"" ) VALUES ( @username, @email, @password_hash, 'User' ) RETURNING ""id"", ""username"", ""email"", ""role"";";

        await using var cmd = new NpgsqlCommand(insertSql, conn);

        cmd.Parameters.AddWithValue("@username", request.Username);
        cmd.Parameters.AddWithValue("@email", request.Email);
        cmd.Parameters.AddWithValue("@password_hash", passwordHash);

        await using var reader = await cmd.ExecuteReaderAsync();

        await reader.ReadAsync();

        return new OkObjectResult(new
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3)
        });
    }
    public async Task<IActionResult> Login(HttpContext context, LoginRequest request)
    {
        using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        const string sql = @"
        SELECT ""id"", ""username"", ""password_hash"", ""role"" FROM master.""Users"" WHERE ""username"" = @username";

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@username", request.Username);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return new UnauthorizedObjectResult("Sai tài khoản hoặc mật khẩu.");
        }

        var id = reader.GetInt32(0);
        var username = reader.GetString(1);
        var passwordHash = reader.GetString(2);

        // Kiểm tra mật khẩu bằng BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
        {
            return new UnauthorizedObjectResult("Sai tài khoản hoặc mật khẩu.");
        }

        var role = reader.GetString(3);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, role)
    };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return new OkObjectResult(new
        {
            Message = "Đăng nhập thành công",
            Username = username,
            Role = role
        });
    }
    public async Task<IActionResult> Logout(HttpContext context)
    {
        await context.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new OkObjectResult(new
        {
            message = "Đăng xuất thành công."
        });
    }
    public async Task<IActionResult> Me(HttpContext context)
    {
        if (!context.User.Identity!.IsAuthenticated)
        {
            return new UnauthorizedResult();
        }

        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(idClaim))
        {
            return new UnauthorizedResult();
        }

        await using var conn = _connection.CreateConnection();
        await conn.OpenAsync();

        const string sql = @" SELECT ""id"", ""username"", ""email"", ""role"" FROM master.""Users"" WHERE ""id"" = @id";

        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@id", int.Parse(idClaim));

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(new
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3)
        });
    }
    public async Task<IActionResult> UpdateProfile(HttpContext context, UpdateProfileRequest request)
    {
        if (!context.User.Identity!.IsAuthenticated)
        {
            return new UnauthorizedResult();
        }

        var idClaim = context.User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(idClaim))
        {
            return new UnauthorizedResult();
        }

        var userId = int.Parse(idClaim);

        await using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        const string checkSql = @" SELECT COUNT(*) FROM master.""Users"" WHERE ( ""username"" = @username OR ""email"" = @email ) AND ""id"" <> @id;";

        await using (var checkCmd = new NpgsqlCommand(checkSql, conn))
        {
            checkCmd.Parameters.AddWithValue("@username", request.Username);
            checkCmd.Parameters.AddWithValue("@email", request.Email);
            checkCmd.Parameters.AddWithValue("@id", userId);

            var count = (long)(await checkCmd.ExecuteScalarAsync())!;

            if (count > 0)
            {
                return new ConflictObjectResult(
                    "Username hoặc Email đã tồn tại.");
            }
        }

        const string updateSql =
            @"UPDATE master.""Users""
            SET ""username""=@username,
                ""email""=@email
            WHERE ""id""=@id
            RETURNING ""id"", ""username"", ""email"", ""role"";";

        await using var cmd =
            new NpgsqlCommand(updateSql, conn);

        cmd.Parameters.AddWithValue("@username", request.Username);

        cmd.Parameters.AddWithValue("@email", request.Email);

        cmd.Parameters.AddWithValue("@id", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return new NotFoundResult();
        }

        var id = reader.GetInt32(0);
        var username = reader.GetString(1);
        var email = reader.GetString(2);
        var role = reader.GetString(3);

        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return new OkObjectResult(new
        {
            Id = id,
            Username = username,
            Email = email,
            Role = role
        });

    }
    public async Task<IActionResult> ChangePassword(HttpContext context, ChangePasswordRequest request)
    {
        if (!context.User.Identity!.IsAuthenticated)
        {
            return new UnauthorizedResult();
        }

        var idClaim = context.User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(idClaim))
        {
            return new UnauthorizedResult();
        }

        var userId = int.Parse(idClaim);

        await using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        const string sql = @" SELECT ""password_hash"" FROM master.""Users"" WHERE ""id""=@id;";

        await using var cmd =
            new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@id", userId);

        var passwordHash =
            (string?)await cmd.ExecuteScalarAsync();

        if (passwordHash == null)
        {
            return new NotFoundResult();
        }

        if (!BCrypt.Net.BCrypt.Verify(
        request.CurrentPassword,
        passwordHash))
        {
            return new BadRequestObjectResult(
                "Mật khẩu hiện tại không đúng.");
        }

        var newHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        const string updateSql = @" UPDATE master.""Users"" SET ""password_hash""=@password WHERE ""id""=@id;";

        await using var updateCmd =
            new NpgsqlCommand(updateSql, conn);

        updateCmd.Parameters.AddWithValue(
            "@password",
            newHash);

        updateCmd.Parameters.AddWithValue(
            "@id",
            userId);

        await updateCmd.ExecuteNonQueryAsync();

        return new OkObjectResult(new
        {
            Message = "Đổi mật khẩu thành công."
        });


    }
}