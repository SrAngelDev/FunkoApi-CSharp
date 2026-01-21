using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FunkoApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace FunkoApi.Auth;

public class TokenService(IConfiguration configuration)
{
    public string GenerateToken(User user)
    {
        // Definimos los Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Roles), 
            new Claim("id", user.Id.ToString())
        };

        // Obtenemos la clave secreta del appsettings (o usamos una por defecto para dev)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaDesarrollo1234!"));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Configuramos el token
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "FunkoApi",
            audience: configuration["Jwt:Audience"] ?? "FunkoClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2), // Expira en 2 horas
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
