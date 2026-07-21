using Artwork_Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Artwork_Core.Services
{
    public interface IAuthService
    {
        Task<IActionResult> Register(RegisterRequest request);
        Task<IActionResult> Login(HttpContext context, LoginRequest request);
        Task<IActionResult> Logout(HttpContext context);
        Task<IActionResult> Me(HttpContext context);
        Task<IActionResult> UpdateProfile(HttpContext context, UpdateProfileRequest request);
        Task<IActionResult> ChangePassword(HttpContext context, ChangePasswordRequest request);
    }
}