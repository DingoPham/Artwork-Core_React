using Artwork_Core.Data;
using Artwork_Core.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

using Npgsql;

using System.Security.Claims;

using System.Security.Cryptography;
using System.Text;

namespace Artwork_Core.Services;

public class AuthService : IAuthService
{
    private readonly IPostgresSqlConnection _connection;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    public AuthService(IPostgresSqlConnection connection, IEmailService emailService, IConfiguration config)
    {
        _connection = connection;
        _emailService = emailService;
        _config = config;
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
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return new BadRequestObjectResult(
                "Email không được để trống.");
        }

        await using var conn = _connection.CreateConnection();

        await conn.OpenAsync();

        const string sql = @"
        SELECT ""id"", ""username"", ""email""
        FROM master.""Users""
        WHERE LOWER(""email"") = LOWER(@email);
    ";

        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@email",
            request.Email.Trim()
        );

        await using var reader =
            await cmd.ExecuteReaderAsync();

        // Không tìm thấy user
        if (!await reader.ReadAsync())
        {
            return new OkObjectResult(new
            {
                Message =
                    "Nếu email tồn tại, bạn sẽ nhận được email đặt lại mật khẩu."
            });
        }

        var userId = reader.GetInt32(0);
        var username = reader.GetString(1);
        var email = reader.GetString(2);

        await reader.CloseAsync();

        // Xóa các token reset cũ chưa sử dụng
        const string deleteOldTokensSql = @"
        DELETE FROM master.""PasswordResetTokens""
        WHERE ""user_id"" = @userId
        AND ""used_at"" IS NULL;
    ";

        await using var deleteCmd =
            new NpgsqlCommand(
                deleteOldTokensSql,
                conn);

        deleteCmd.Parameters.AddWithValue(
            "@userId",
            userId);

        await deleteCmd.ExecuteNonQueryAsync();

        // Tạo token random
        var tokenBytes =
            RandomNumberGenerator.GetBytes(32);

        var token =
            Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

        // Hash token trước khi lưu DB
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)
            )
        );

        var expiresAt =
            DateTime.UtcNow.AddMinutes(30);

        const string insertTokenSql = @"
        INSERT INTO master.""PasswordResetTokens""
        (
            ""user_id"",
            ""token_hash"",
            ""expires_at""
        )
        VALUES
        (
            @userId,
            @tokenHash,
            @expiresAt
        );
    ";

        await using var insertCmd =
            new NpgsqlCommand(
                insertTokenSql,
                conn);

        insertCmd.Parameters.AddWithValue(
            "@userId",
            userId);

        insertCmd.Parameters.AddWithValue(
            "@tokenHash",
            tokenHash);

        insertCmd.Parameters.AddWithValue(
            "@expiresAt",
            expiresAt);

        await insertCmd.ExecuteNonQueryAsync();

        var frontendUrl =
            _config["Frontend:ResetPasswordUrl"];

        var resetLink =
            $"{frontendUrl}?token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendPasswordResetEmail(
                email,
                username,
                resetLink);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Password reset email error: {ex}");
        }
        return new OkObjectResult(new
        {
            Message =
         "Nếu email tồn tại, bạn sẽ nhận được email đặt lại mật khẩu."
        });
    }
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new BadRequestObjectResult(
                "Token không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new BadRequestObjectResult(
                "Mật khẩu mới không được để trống.");
        }

        if (request.NewPassword.Length < 8)
        {
            return new BadRequestObjectResult(
                "Mật khẩu phải có ít nhất 8 ký tự.");
        }

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    request.Token
                )
            )
        );

        await using var conn =
            _connection.CreateConnection();

        await conn.OpenAsync();

        const string sql = @"
        SELECT
            ""id"",
            ""user_id"",
            ""expires_at"",
            ""used_at""
        FROM master.""PasswordResetTokens""
        WHERE ""token_hash"" = @tokenHash
        LIMIT 1;
    ";

        await using var cmd =
            new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@tokenHash",
            tokenHash);

        await using var reader =
            await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return new BadRequestObjectResult(
                "Token không hợp lệ hoặc đã hết hạn.");
        }

        var tokenId = reader.GetInt32(0);
        var userId = reader.GetInt32(1);
        var expiresAt = reader.GetFieldValue<DateTime>(2);

        DateTime? usedAt = null;

        if (!reader.IsDBNull(3))
        {
            usedAt = reader.GetFieldValue<DateTime>(3);
        }

        await reader.CloseAsync();

        if (usedAt.HasValue)
        {
            return new BadRequestObjectResult(
                "Token đã được sử dụng.");
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            return new BadRequestObjectResult(
                "Token đã hết hạn.");
        }

        var newHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        const string updatePasswordSql = @"
        UPDATE master.""Users""
        SET ""password_hash"" = @passwordHash
        WHERE ""id"" = @userId;
    ";

        await using var updatePasswordCmd =
            new NpgsqlCommand(
                updatePasswordSql,
                conn);

        updatePasswordCmd.Parameters.AddWithValue(
            "@passwordHash",
            newHash);

        updatePasswordCmd.Parameters.AddWithValue(
            "@userId",
            userId);

        var affected =
            await updatePasswordCmd.ExecuteNonQueryAsync();

        if (affected == 0)
        {
            return new NotFoundResult();
        }

        // Đánh dấu token đã sử dụng
        const string markUsedSql = @"
        UPDATE master.""PasswordResetTokens""
        SET ""used_at"" = NOW()
        WHERE ""id"" = @tokenId;
    ";

        await using var markUsedCmd =
            new NpgsqlCommand(
                markUsedSql,
                conn);

        markUsedCmd.Parameters.AddWithValue(
            "@tokenId",
            tokenId);

        await markUsedCmd.ExecuteNonQueryAsync();

        // Vô hiệu hóa các token reset khác của user
        const string invalidateOtherTokensSql = @"
        UPDATE master.""PasswordResetTokens""
        SET ""used_at"" = NOW()
        WHERE ""user_id"" = @userId
        AND ""used_at"" IS NULL;
    ";

        await using var invalidateCmd =
            new NpgsqlCommand(
                invalidateOtherTokensSql,
                conn);

        invalidateCmd.Parameters.AddWithValue(
            "@userId",
            userId);

        await invalidateCmd.ExecuteNonQueryAsync();

        return new OkObjectResult(new
        {
            Message = "Đặt lại mật khẩu thành công."
        });
    }
}