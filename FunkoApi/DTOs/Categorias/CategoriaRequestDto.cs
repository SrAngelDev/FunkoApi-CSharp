using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Dtos;

[ExcludeFromCodeCoverage]
public record CategoriaRequestDto(
    string Nombre
    );