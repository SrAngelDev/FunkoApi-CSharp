﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;

namespace FunkoApi.Models;

[ExcludeFromCodeCoverage]
public class User : IdentityUser<long>
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

