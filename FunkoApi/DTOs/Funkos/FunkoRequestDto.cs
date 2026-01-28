using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Dtos;

[ExcludeFromCodeCoverage]
public record FunkoRequestDto(
    string Nombre, 
    Guid CategoriaId, 
    decimal Precio
    );