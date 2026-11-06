using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoilShop.Application;

namespace SoilShop.Api;

[ApiController, Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("login")]
    public Task<LoginResult> Login(LoginInput input, CancellationToken ct) => auth.LoginAsync(input, ct);
    [HttpGet("me"), Authorize]
    public Task<UserDto> Me(CancellationToken ct) => auth.MeAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
}
