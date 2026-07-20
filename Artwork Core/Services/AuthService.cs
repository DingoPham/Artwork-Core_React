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

    public async Task<IActionResult> Login(HttpContext context, LoginRequest request)
    {
        throw new NotImplementedException();
        using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@username",
            request.Username);
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return new UnauthorizedObjectResult("Sai tài khoản hoặc mật khẩu.");
        }

        var id = reader.GetInt32(0);

        var username = reader.GetString(1);

        var passwordHash = reader.GetString(2);

        var role = reader.GetString(3);

        var claims = new List<Claim>
{
    new Claim(
        ClaimTypes.Name,
        username),

    new Claim(
        ClaimTypes.Role,
        role),

    new Claim(
        ClaimTypes.NameIdentifier,
        id.ToString())
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
            Username = username,
            Role = role
        });
    }

    public async Task<IActionResult> Logout(HttpContext context)
    {
        await context.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new OkObjectResult("Đăng xuất thành công.");
    }

    public async Task<IActionResult> Me(HttpContext context)
    {
        if (!context.User.Identity!.IsAuthenticated)
        {
            return new UnauthorizedResult();
        }
    }

}