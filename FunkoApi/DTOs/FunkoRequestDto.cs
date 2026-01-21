namespace FunkoApi.Dtos;

public record FunkoRequestDto(
    string Nombre, 
    Guid CategoriaId, 
    decimal Precio
    );