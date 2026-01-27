using FunkoApi.Models;
using FunkoApi.Dtos;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Services.Categorias;
using FunkoApi.Errors;
using FunkoApi.Errors.Categorias;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.SignalR;
using FunkoApi.WebSockets;

namespace FunkoApi.Tests.Unitarios.Services;

[TestFixture]
public class CategoriaServiceTests
{
    private Mock<ICategoriaRepository> _repositoryMock;
    private Mock<IDistributedCache> _cacheMock;
    private Mock<IValidator<CategoriaRequestDto>> _validatorMock;
    private Mock<ILogger<CategoriaService>> _loggerMock;
    private CategoriaService _service;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<ICategoriaRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _validatorMock = new Mock<IValidator<CategoriaRequestDto>>();
        _loggerMock = new Mock<ILogger<CategoriaService>>();

        _service = new CategoriaService(
            _repositoryMock.Object,
            _cacheMock.Object,
            _validatorMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task GetAllAsync_CuandoCacheMiss_ConsultaRepositorioYGuardaEnCache()
    {
        // ARRANGE
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var categorias = new List<Categoria>
        {
            new Categoria { Id = Guid.NewGuid(), Nombre = "Marvel", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Categoria { Id = Guid.NewGuid(), Nombre = "DC", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categorias);

        _cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.GetAllAsync();

        // ASSERT
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Nombre, Is.EqualTo("Marvel"));
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAllAsync_CuandoCacheHit_NoConsultaRepositorio()
    {
        // ARRANGE
        var categoriasEnCache = new List<CategoriaResponseDto>
        {
            new CategoriaResponseDto(Guid.NewGuid(), "Marvel"),
            new CategoriaResponseDto(Guid.NewGuid(), "DC")
        };

        var jsonCache = System.Text.Json.JsonSerializer.Serialize(categoriasEnCache);
        var bytesCache = System.Text.Encoding.UTF8.GetBytes(jsonCache);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytesCache);

        // ACT
        var result = await _service.GetAllAsync();

        // ASSERT
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Nombre, Is.EqualTo("Marvel"));
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_CuandoExiste_RetornaCategoria()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria
        {
            Id = categoriaId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _repositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        _cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.GetByIdAsync(categoriaId);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("Marvel"));
        Assert.That(result.Value.Id, Is.EqualTo(categoriaId));
        _repositoryMock.Verify(r => r.GetByIdAsync(categoriaId), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _repositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // ACT
        var result = await _service.GetByIdAsync(categoriaId);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        _repositoryMock.Verify(r => r.GetByIdAsync(categoriaId), Times.Once);
    }

    [Test]
    public async Task CreateAsync_CuandoDatosSonValidos_RetornaSuccess()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto("DC");
        var categoriaGuardada = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = "DC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByNombreAsync("DC"))
            .ReturnsAsync((Categoria?)null);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(categoriaGuardada);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("DC"));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Categoria>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_CuandoValidacionFalla_RetornaFailure()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto("");

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Nombre", "El nombre es requerido")
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_CuandoNombreDuplicado_RetornaConflictError()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto("Marvel");
        var categoriaExistente = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByNombreAsync("Marvel"))
            .ReturnsAsync(categoriaExistente);

        // ACT
        var result = await _service.CreateAsync(dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<ConflictError>());
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoDatosSonValidos_RetornaSuccess()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var dto = new CategoriaRequestDto("Marvel Updated");
        var categoriaExistente = new Categoria
        {
            Id = categoriaId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var categoriaActualizada = new Categoria
        {
            Id = categoriaId,
            Nombre = "Marvel Updated",
            CreatedAt = categoriaExistente.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoriaExistente);

        _repositoryMock.Setup(r => r.GetByNombreAsync("Marvel Updated"))
            .ReturnsAsync((Categoria?)null);

        _repositoryMock.Setup(r => r.UpdateAsync(categoriaId, It.IsAny<Categoria>()))
            .ReturnsAsync(categoriaActualizada);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.UpdateAsync(categoriaId, dto);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("Marvel Updated"));
        _repositoryMock.Verify(r => r.UpdateAsync(categoriaId, It.IsAny<Categoria>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task UpdateAsync_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var dto = new CategoriaRequestDto("Marvel");

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByNombreAsync("Marvel"))
            .ReturnsAsync((Categoria?)null);

        _repositoryMock.Setup(r => r.UpdateAsync(categoriaId, It.IsAny<Categoria>()))
            .ReturnsAsync((Categoria?)null);

        // ACT
        var result = await _service.UpdateAsync(categoriaId, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        _repositoryMock.Verify(r => r.UpdateAsync(categoriaId, It.IsAny<Categoria>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_CuandoNombreDuplicado_RetornaConflictError()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var dto = new CategoriaRequestDto("DC");
        var categoriaExistente = new Categoria
        {
            Id = categoriaId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var otraCategoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = "DC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoriaExistente);

        _repositoryMock.Setup(r => r.GetByNombreAsync("DC"))
            .ReturnsAsync(otraCategoria);

        // ACT
        var result = await _service.UpdateAsync(categoriaId, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<ConflictError>());
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CuandoExiste_RetornaSuccess()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria
        {
            Id = categoriaId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Funkos = new List<Funko>()
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        _repositoryMock.Setup(r => r.DeleteAsync(categoriaId))
            .ReturnsAsync(categoria);

        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.DeleteAsync(categoriaId);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        _repositoryMock.Verify(r => r.DeleteAsync(categoriaId), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task DeleteAsync_CuandoNoExiste_RetornaNotFound()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.DeleteAsync(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // ACT
        var result = await _service.DeleteAsync(categoriaId);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<NotFoundError>());
        _repositoryMock.Verify(r => r.DeleteAsync(categoriaId), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_CuandoEstaCacheado_NoConsultaRepositorio()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var categoriaEnCache = new CategoriaResponseDto(categoriaId, "Marvel");
        var jsonCache = System.Text.Json.JsonSerializer.Serialize(categoriaEnCache);
        var bytesCache = System.Text.Encoding.UTF8.GetBytes(jsonCache);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytesCache);

        // ACT
        var result = await _service.GetByIdAsync(categoriaId);

        // ASSERT
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nombre, Is.EqualTo("Marvel"));
        _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CuandoValidacionFalla_RetornaFailure()
    {
        // ARRANGE
        var categoriaId = Guid.NewGuid();
        var dto = new CategoriaRequestDto("");
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Nombre", "El nombre es requerido")
        };

        _validatorMock.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // ACT
        var result = await _service.UpdateAsync(categoriaId, dto);

        // ASSERT
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.InstanceOf<BusinessRuleError>());
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Categoria>()), Times.Never);
    }
}
