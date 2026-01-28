using FunkoApi.Controllers;
using FunkoApi.Dtos;
using FunkoApi.Errors;
using FunkoApi.Services.Categorias;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using CSharpFunctionalExtensions;

namespace FunkoApi.Tests.Unitarios.Controllers;

[TestFixture]
public class CategoriasControllerTests
{
    private Mock<ICategoriaService> _serviceMock;
    private CategoriasController _controller;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<ICategoriaService>();
        _controller = new CategoriasController(_serviceMock.Object);
    }

    [Test]
    public async Task GetAll_RetornaListaDeCategorias()
    {
        // ARRANGE
        var categorias = new List<CategoriaResponseDto>
        {
            new CategoriaResponseDto(Guid.NewGuid(), "Marvel"),
            new CategoriaResponseDto(Guid.NewGuid(), "DC")
        };

        _serviceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync(categorias);

        // ACT
        var resultado = await _controller.GetAll();

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (IEnumerable<CategoriaResponseDto>)okResult.Value!;

        Assert.That(model.Count(), Is.EqualTo(2));
        Assert.That(model.First().Nombre, Is.EqualTo("Marvel"));
    }

    [Test]
    public async Task GetById_CuandoExiste_RetornaOkConCategoria()
    {
        // ARRANGE
        var idExistente = Guid.NewGuid();
        var categoriaDto = new CategoriaResponseDto(idExistente, "Disney");

        _serviceMock.Setup(s => s.GetByIdAsync(idExistente))
            .ReturnsAsync(Result.Success<CategoriaResponseDto, AppError>(categoriaDto));

        // ACT
        var resultado = await _controller.GetById(idExistente);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (CategoriaResponseDto)okResult.Value!;

        Assert.That(model.Nombre, Is.EqualTo("Disney"));
        Assert.That(model.Id, Is.EqualTo(idExistente));
    }

    [Test]
    public async Task GetById_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        var idNoExistente = Guid.NewGuid();
        var error = new NotFoundError("Categoría no encontrada");

        _serviceMock.Setup(s => s.GetByIdAsync(idNoExistente))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.GetById(idNoExistente);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Create_CuandoServicioRetornaExito_DevuelveCreated()
    {
        // ARRANGE
        var requestDto = new CategoriaRequestDto("Star Wars");
        var responseDto = new CategoriaResponseDto(Guid.NewGuid(), "Star Wars");
        
        _serviceMock.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<CategoriaResponseDto, AppError>(responseDto));

        // ACT
        var resultado = await _controller.Create(requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<CreatedAtActionResult>());
        
        var createdResult = (CreatedAtActionResult)resultado.Result!;
        var model = (CategoriaResponseDto)createdResult.Value!;

        Assert.That(model.Nombre, Is.EqualTo("Star Wars"));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(CategoriasController.GetById)));
    }

    [Test]
    public async Task Create_CuandoFallaValidacion_DevuelveBadRequest()
    {
        // ARRANGE
        var requestDto = new CategoriaRequestDto(""); // Nombre vacío
        var error = new BusinessRuleError("El nombre no puede estar vacío");
        
        _serviceMock.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Create(requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<BadRequestObjectResult>());
        
        var badRequestResult = (BadRequestObjectResult)resultado.Result!;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Create_CuandoCategoriaDuplicada_RetornaConflict()
    {
        // ARRANGE
        var requestDto = new CategoriaRequestDto("Marvel");
        var error = new ConflictError("Ya existe una categoría con ese nombre");
        
        _serviceMock.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Create(requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<ConflictObjectResult>());
        
        var conflictResult = (ConflictObjectResult)resultado.Result!;
        Assert.That(conflictResult.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task Update_CuandoExiste_RetornaOkConCategoriaActualizada()
    {
        // ARRANGE
        var idExistente = Guid.NewGuid();
        var requestDto = new CategoriaRequestDto("Marvel Studios");
        var responseDto = new CategoriaResponseDto(idExistente, "Marvel Studios");

        _serviceMock.Setup(s => s.UpdateAsync(idExistente, requestDto))
            .ReturnsAsync(Result.Success<CategoriaResponseDto, AppError>(responseDto));

        // ACT
        var resultado = await _controller.Update(idExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (CategoriaResponseDto)okResult.Value!;

        Assert.That(model.Nombre, Is.EqualTo("Marvel Studios"));
    }

    [Test]
    public async Task Update_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        var idNoExistente = Guid.NewGuid();
        var requestDto = new CategoriaRequestDto("Categoria");
        var error = new NotFoundError("Categoría no encontrada");

        _serviceMock.Setup(s => s.UpdateAsync(idNoExistente, requestDto))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Update(idNoExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Update_CuandoNombreDuplicado_RetornaConflict()
    {
        // ARRANGE
        var idExistente = Guid.NewGuid();
        var requestDto = new CategoriaRequestDto("DC");
        var error = new ConflictError("Ya existe una categoría con ese nombre");

        _serviceMock.Setup(s => s.UpdateAsync(idExistente, requestDto))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Update(idExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task Delete_CuandoExiste_RetornaNoContent()
    {
        // ARRANGE
        var idExistente = Guid.NewGuid();
        
        _serviceMock.Setup(s => s.DeleteAsync(idExistente))
            .ReturnsAsync(Result.Success<CategoriaResponseDto, AppError>(null!));

        // ACT
        var resultado = await _controller.Delete(idExistente);

        // ASSERT
        Assert.That(resultado, Is.InstanceOf<NoContentResult>());
        
        var noContentResult = (NoContentResult)resultado;
        Assert.That(noContentResult.StatusCode, Is.EqualTo(204));
        
        _serviceMock.Verify(s => s.DeleteAsync(idExistente), Times.Once);
    }

    [Test]
    public async Task Delete_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        var idNoExistente = Guid.NewGuid();
        var error = new NotFoundError("Categoría no encontrada");

        _serviceMock.Setup(s => s.DeleteAsync(idNoExistente))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Delete(idNoExistente);

        // ASSERT
        Assert.That(resultado, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_CuandoTieneFunkosAsociados_RetornaBadRequest()
    {
        // ARRANGE
        var idExistente = Guid.NewGuid();
        var error = new BusinessRuleError("No se puede eliminar la categoría porque tiene Funkos asociados");

        _serviceMock.Setup(s => s.DeleteAsync(idExistente))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Delete(idExistente);

        // ASSERT
        Assert.That(resultado, Is.InstanceOf<BadRequestObjectResult>());
        
        var badRequestResult = (BadRequestObjectResult)resultado;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task HandleError_CuandoConflictError_RetornaConflict()
    {
        // ARRANGE
        var idExistente = Guid.NewGuid();
        var requestDto = new CategoriaRequestDto("Marvel");
        var error = new ConflictError("Conflicto");

        _serviceMock.Setup(s => s.UpdateAsync(idExistente, requestDto))
            .ReturnsAsync(Result.Failure<CategoriaResponseDto, AppError>(error));

        // ACT
        var resultado = await _controller.Update(idExistente, requestDto);

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<ConflictObjectResult>());
        
        var conflictResult = (ConflictObjectResult)resultado.Result!;
        Assert.That(conflictResult.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task GetAll_CuandoNoHayCategorias_RetornaListaVacia()
    {
        // ARRANGE
        var categoriasVacias = new List<CategoriaResponseDto>();

        _serviceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync(categoriasVacias);

        // ACT
        var resultado = await _controller.GetAll();

        // ASSERT
        Assert.That(resultado.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult)resultado.Result!;
        var model = (IEnumerable<CategoriaResponseDto>)okResult.Value!;

        Assert.That(model.Count(), Is.EqualTo(0));
    }
}
