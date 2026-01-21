using FunkoApi.Dtos;
using FunkoApi.Models;

namespace FunkoApi.Mappers;

public static class CategoriaMapper
{
    // Entidad -> ResponseDto
    public static CategoriaResponseDto ToResponseDto(this Categoria categoria)
    {
        return new CategoriaResponseDto(
            categoria.Id,
            categoria.Nombre
        );
    }

    // RequestDto -> Entidad (Para creación)
    public static Categoria ToEntity(this CategoriaRequestDto dto)
    {
        return new Categoria
        {
            Nombre = dto.Nombre,
            // Las fuentes sugieren que las entidades suelen tener campos de auditoría
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow
        };
    }
}