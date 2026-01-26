using CSharpFunctionalExtensions;
using FunkoApi.Data;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Auth;

public class AuthService(
    UserManager<User> userManager, 
    TokenService tokenService)  
{
    public async Task<Result<string, AppError>> RegisterAsync(RegisterDto dto)
    {
        var user = new User
        {
            UserName = dto.Username,
            Email = dto.Email,
            Nombre = dto.Username, 
            CreatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            
            return Result.Failure<string, AppError>(new BusinessRuleError(errorMsg));
        }
        
        var roleResult = await userManager.AddToRoleAsync(user, Roles.User);
        
        if (!roleResult.Succeeded)
        {
             return Result.Failure<string, AppError>(new BusinessRuleError("Error al asignar rol de usuario"));
        }

        return Result.Success<string, AppError>("Usuario registrado correctamente");
    }

    public async Task<Result<string, AppError>> LoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.Username);
        
        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Result.Failure<string, AppError>(new BusinessRuleError("Usuario o contraseña incorrectos"));
        }
        
        var roles = await userManager.GetRolesAsync(user);

        
        var token = tokenService.GenerateToken(user, roles); 

        return Result.Success<string, AppError>(token);
    }
}