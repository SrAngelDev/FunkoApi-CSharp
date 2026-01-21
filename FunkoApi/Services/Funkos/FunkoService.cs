using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Errors.Funkos;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using Microsoft.Extensions.Caching.Memory;
using FunkoApi.Mappers;
using FunkoApi.Storage;

namespace FunkoApi.Services.Funkos;

public class FunkoService(
    IFunkoRepository repository,
    ICategoriaRepository categoriaRepository,
    IMemoryCache cache,
    IValidator<FunkoRequestDto> validator,
    IStorageService storage) : IFunkoService
{
    private const string CacheListKey = "funkos_all";
    private const string CacheBaseKey = "funko_";
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(30);
    
    private string GetKey(long id) => $"{CacheBaseKey}{id}";

    public async Task<IEnumerable<FunkoResponseDto>> GetAllAsync()
    {
        if (!cache.TryGetValue(CacheListKey, out IEnumerable<FunkoResponseDto>? dtos))
        {
            var funkos = await repository.GetAllAsync();
            dtos = funkos.Select(f => f.ToResponseDto()).ToList();
            cache.Set(CacheListKey, dtos, _cacheTime);
        }
        return dtos!;
    }

    public async Task<Result<FunkoResponseDto, AppError>> GetByIdAsync(long id)
    {
        string key = GetKey(id);
        if (!cache.TryGetValue(key, out FunkoResponseDto? dto))
        {
            var funko = await repository.GetByIdAsync(id);
            
            if (funko == null) 
                return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));

            dto = funko.ToResponseDto();
            cache.Set(key, dto, _cacheTime);
        }
        return Result.Success<FunkoResponseDto, AppError>(dto!);
    }

    public async Task<Result<FunkoResponseDto, AppError>> CreateAsync(FunkoRequestDto dto)
    {

        var valResult = await validator.ValidateAsync(dto);
        if (!valResult.IsValid) 
            return Result.Failure<FunkoResponseDto, AppError>(new BusinessRuleError(valResult.Errors.First().ErrorMessage));
        
        var categoria = await categoriaRepository.GetByNombreAsync(dto.Nombre);
        if (categoria == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.CategoriaNoEncontrada(dto.Nombre));
        
        var nuevo = await repository.CreateAsync(dto.ToEntity(categoria.Id));
        cache.Remove(CacheListKey);

        return Result.Success<FunkoResponseDto, AppError>(nuevo.ToResponseDto());
    }

    public async Task<Result<FunkoResponseDto, AppError>> UpdateAsync(long id, FunkoRequestDto dto)
    {
        var valResult = await validator.ValidateAsync(dto);
        if (!valResult.IsValid) 
            return Result.Failure<FunkoResponseDto, AppError>(new BusinessRuleError(valResult.Errors.First().ErrorMessage));
        
        var existente = await repository.GetByIdAsync(id);
        if (existente == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));
        
        var categoria = await categoriaRepository.GetByNombreAsync(dto.Nombre);
        if (categoria == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.CategoriaNoEncontrada(dto.Nombre));
        
        existente.Nombre = dto.Nombre;
        existente.Precio = dto.Precio;
        existente.CategoriaId = categoria.Id; 
        existente.UpdatedAt = DateTime.UtcNow; 
        
        var actualizado = await repository.UpdateAsync(id, existente);
    
        if (actualizado == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));
        
        cache.Remove(CacheListKey); 
        cache.Remove(GetKey(id));   
        
        return Result.Success<FunkoResponseDto, AppError>(actualizado.ToResponseDto());
    }

    public async Task<Result<FunkoResponseDto, AppError>> DeleteAsync(long id)
    {
        var eliminado = await repository.DeleteAsync(id);
        
        if (eliminado == null) 
            return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));
        
        cache.Remove(CacheListKey);
        cache.Remove(GetKey(id));
        

        return Result.Success<FunkoResponseDto, AppError>(eliminado.ToResponseDto());
    }
    
    public async Task<Result<FunkoResponseDto, AppError>> UpdateImageAsync(long id, IFormFile file)
    {
        // Existe el Funko?
        var funko = await repository.GetByIdAsync(id);
        if (funko == null) return Result.Failure<FunkoResponseDto, AppError>(FunkoError.NotFound(id));

        // Guardar nueva imagen
        string nuevoNombreImagen;
        try 
        {
            nuevoNombreImagen = await storage.SaveFileAsync(file);
        }
        catch (Exception ex)
        {
            return Result.Failure<FunkoResponseDto, AppError>(new BusinessRuleError($"Error al subir imagen: {ex.Message}"));
        }

        // Borrar imagen antigua si no es la por defecto
        if (!string.IsNullOrEmpty(funko.Imagen) && !funko.Imagen.StartsWith("https"))
        {
            storage.DeleteFile(funko.Imagen);
        }

        // Actualizar entidad y BD
        funko.Imagen = nuevoNombreImagen; // Guardamos solo el nombre 
        funko.UpdatedAt = DateTime.UtcNow;
        
        var actualizado = await repository.UpdateAsync(id, funko);

        // Invalidar caché
        cache.Remove(CacheListKey);
        cache.Remove(GetKey(id));

        return Result.Success<FunkoResponseDto, AppError>(actualizado!.ToResponseDto());
    }
}