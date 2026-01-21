using FunkoApi.Dtos;
using FunkoApi.Models;

namespace FunkoApi.Mappers;

public static class FunkoMapper
{
    public static FunkoResponseDto ToResponseDto(this Funko funko)
    {
        return new FunkoResponseDto(
            funko.Id,
            funko.Nombre,
            funko.Categoria != null ? funko.Categoria.ToResponseDto() : null,
            funko.Precio,
            funko.Imagen,
            funko.UpdatedAt,
            funko.CreatedAt
        );
    }
    
    public static Funko ToEntity(this FunkoRequestDto dto, Guid categoriaId)
    {
        return new Funko
        {
            Nombre = dto.Nombre,
            Precio = dto.Precio,
            CategoriaId = dto.CategoriaId,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}