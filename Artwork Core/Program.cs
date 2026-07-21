using Artwork_Core.Data;
using Artwork_Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://artwork-site-8eth.onrender.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddScoped<IPostgresSqlConnection, PostgressSQLConnection>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login";

        options.LogoutPath = "/api/auth/logout";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;

        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.None;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<R2Service>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors("AllowReact");

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.Run();
}
else
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Run($"http://0.0.0.0:{port}");
}
