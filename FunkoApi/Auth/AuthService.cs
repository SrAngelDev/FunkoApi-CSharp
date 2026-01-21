using CSharpFunctionalExtensions;
using FunkoApi.Data;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Auth;

public class AuthService(FunkoDbContext context, TokenService tokenService)
{
    public async Task<Result<string, AppError>> RegisterAsync(RegisterDto dto)
    {
        // Validar si existe
        if (await context.Users.AnyAsync(u => u.Username == dto.Username))
            return Result.Failure<string, AppError>(new ConflictError("El usuario ya existe"));

        // Crear usuario con password hasheada
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Roles = Roles.User // Por defecto usuario normal
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Result.Success<string, AppError>("Usuario registrado correctamente");
    }

    public async Task<Result<string, AppError>> LoginAsync(LoginDto dto)
    {
        // Buscar usuario
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        
        // Verificar password y existencia
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return Result.Failure<string, AppError>(new BusinessRuleError("Usuario o contraseña incorrectos"));

        // Generar Token
        var token = tokenService.GenerateToken(user);
        return Result.Success<string, AppError>(token);
    }
}