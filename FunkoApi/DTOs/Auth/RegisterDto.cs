using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Dtos;

[ExcludeFromCodeCoverage]
public record RegisterDto(
    string Username, 
    string Password, 
    string Email
    );