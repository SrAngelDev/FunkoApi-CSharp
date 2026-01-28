using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Dtos;

[ExcludeFromCodeCoverage]
public record LoginDto(
    string Username, 
    string Password
    );