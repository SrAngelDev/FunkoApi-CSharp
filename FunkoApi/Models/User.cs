namespace FunkoApi.Models;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // Guardaremos el Hash
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = Models.Roles.User; // Por defecto USER
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

