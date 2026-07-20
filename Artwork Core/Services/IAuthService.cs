using Artwork_Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Artwork_Core.Services
{
    public interface IAuthService
    {
        Task<IActionResult> Login(HttpContext context, LoginRequest request);

        Task<IActionResult> Logout(HttpContext context);

        Task<IActionResult> Me(HttpContext context);
    }
}