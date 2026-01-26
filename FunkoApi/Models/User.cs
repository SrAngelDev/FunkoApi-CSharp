using Microsoft.AspNetCore.Identity;

namespace FunkoApi.Models;

public class User : IdentityUser<long>
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

