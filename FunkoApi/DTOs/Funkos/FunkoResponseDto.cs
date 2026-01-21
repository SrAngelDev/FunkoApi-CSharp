namespace FunkoApi.Dtos;

public record FunkoResponseDto(
    long Id, 
    string Nombre, 
    CategoriaResponseDto? Categoria, 
    decimal Precio, 
    string Imagen,
    DateTime CreatedAt,
    DateTime UpdatedAt
    );
    