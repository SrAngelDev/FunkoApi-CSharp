using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Models;
using FunkoApi.Services.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController(ICategoriaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponseDto>>> GetAll()
    {
        var result = await service.GetAllAsync();
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoriaResponseDto>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        
        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value);
    }
    
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoriaResponseDto>> Create([FromBody] CategoriaRequestDto dto)
    {
        var result = await service.CreateAsync(dto);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }
        
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoriaResponseDto>> Update(Guid id, [FromBody] CategoriaRequestDto dto)
    {
        var result = await service.UpdateAsync(id, dto);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value); 
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }
        
        return NoContent(); 
    }
    
    private ActionResult HandleError(AppError error)
    {
        return error switch
        {
            NotFoundError => NotFound(new { error = error.Message }),
            ConflictError => Conflict(new { error = error.Message }),
            BusinessRuleError => BadRequest(new { error = error.Message }),
            _ => StatusCode(500, new { error = "Error interno del servidor" })
        };
    }
}
