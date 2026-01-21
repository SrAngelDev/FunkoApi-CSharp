using FunkoApi.Auth;
using FunkoApi.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        return result.IsSuccess ? Ok(new { token = result.Value }) : Unauthorized(result.Error);
    }
}