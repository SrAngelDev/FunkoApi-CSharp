namespace FunkoApi.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FunkoApi.Models;
using Microsoft.IdentityModel.Tokens;

// Usamos Constructor Primario (C# 14)
public class TokenService(IConfiguration configuration)
{
    // CAMBIO IMPORTANTE: Ahora recibimos 'roles' como lista separada
    public string GenerateToken(User user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            
            new Claim("id", user.Id.ToString()) 
        };
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaDesarrollo1234!"));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "FunkoApi",
            audience: configuration["Jwt:Audience"] ?? "FunkoClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}