using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Errors.Categorias;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using Microsoft.Extensions.Caching.Memory;

namespace FunkoApi.Services.Categorias;

public class CategoriaService(
    ICategoriaRepository repository,
    IMemoryCache cache,
    IValidator<CategoriaRequestDto> validator) : ICategoriaService
{
    private const string CacheListKey = "categorias_all";
    private const string CacheBaseKey = "categoria_"; 
    private string GetKey(Guid id) => $"{CacheBaseKey}{id}";
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(30);

    public async Task<IEnumerable<CategoriaRespondeDto>> GetAllAsync()
    {
        if (!cache.TryGetValue(CacheListKey, out IEnumerable<CategoriaRespondeDto>? dtos))
        {
            var categorias = await repository.GetAllAsync();
            dtos = categorias.Select(c => new CategoriaRespondeDto(c.Id, c.Nombre)).ToList();
            cache.Set(CacheListKey, dtos, _cacheTime);
        }
        return dtos!;
    }

    public async Task<Result<CategoriaRespondeDto, AppError>> GetByIdAsync(Guid id)
    {
        string key = GetKey(id);
        if (!cache.TryGetValue(key, out CategoriaRespondeDto? dto))
        {
            var categoria = await repository.GetByIdAsync(id);
            if (categoria == null) return Result.Failure<CategoriaRespondeDto, AppError>(CategoriaError.NotFound(id));
            
            dto = new CategoriaRespondeDto(categoria.Id, categoria.Nombre);
            cache.Set(key, dto, _cacheTime);
        }
        return Result.Success<CategoriaRespondeDto, AppError>(dto!);
    }

    public async Task<Result<CategoriaRespondeDto, AppError>> CreateAsync(CategoriaRequestDto dto)
    {
        //Primero validamos
        var categoriaResult = await validator.ValidateAsync(dto);
        if (!categoriaResult.IsValid)
        {
            return Result.Failure<CategoriaRespondeDto, AppError>(new BusinessRuleError(categoriaResult.Errors.First().ErrorMessage));
        }
        
        //Evitar duplicados
        var nombreExiste = await repository.GetByNombreAsync(dto.Nombre);
        if (nombreExiste != null)
        {
            return Result.Failure<CategoriaRespondeDto, AppError>(CategoriaError.NombreDuplicado(dto.Nombre));
        }
        
        //Creamos nueva categoria
        var nuevaCategoria = await repository.CreateAsync(new Categoria {Nombre =  dto.Nombre});
        
        //Eliminamos la vieja cache ya que si volvemos a llamar a todos al buscar primero en la cache devuelve datos obsoletos
        cache.Remove(CacheListKey);
        
        return Result.Success<CategoriaRespondeDto, AppError>(new CategoriaRespondeDto(nuevaCategoria.Id, nuevaCategoria.Nombre));
    }
    
    public async Task<Result<CategoriaRespondeDto, AppError>> UpdateAsync(Guid id, CategoriaRequestDto dto) {
        var actualizado = await repository.UpdateAsync(id, new Categoria { Nombre = dto.Nombre });
        if (actualizado == null) return Result.Failure<CategoriaRespondeDto, AppError>(CategoriaError.NotFound(id));

        // Invalidamos la cache vieja y la del objeto
        cache.Remove(CacheListKey);
        cache.Remove(GetKey(id));

        return Result.Success<CategoriaRespondeDto, AppError>(new CategoriaRespondeDto(actualizado.Id, actualizado.Nombre));
    }

    public async Task<Result<CategoriaRespondeDto, AppError>> DeleteAsync(Guid id) {

        var eliminado = await repository.DeleteAsync(id);
        if (eliminado == null)
        {
            return Result.Failure<CategoriaRespondeDto, AppError>(CategoriaError.NotFound(id));
        }

        // Invalidamos la cache vieja y la del objeto
        cache.Remove(CacheListKey);
        cache.Remove(GetKey(id));

        return Result.Success<CategoriaRespondeDto, AppError>(new CategoriaRespondeDto(eliminado.Id, eliminado.Nombre));
    }
}