namespace FunkoApi.Dtos;

public record FunkoResponseDto(
    long Id, 
    string Nombre, 
    CategoriaRespondeDto Categoria, 
    decimal Precio, 
    DateTime CreatedAt,
    DateTime UpdatedAt
    );
    