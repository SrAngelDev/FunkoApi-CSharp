using FunkoApi.Dtos;
using FunkoApi.Validators.Categorias;
using NUnit.Framework;

namespace FunkoApi.Tests.Unitarios.Validators;

[TestFixture]
public class CategoriaRequestValidatorTests
{
    private CategoriaRequestValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new CategoriaRequestValidator();
    }

    [Test]
    public async Task Validate_CuandoNombreEsValido_NoDebeRetornarErrores()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto("Marvel");

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task Validate_CuandoNombreEsVacio_DebeRetornarError()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto("");

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThan(0));
        Assert.That(result.Errors.Any(e => e.PropertyName == "Nombre"), Is.True);
    }

    [Test]
    public async Task Validate_CuandoNombreEsNull_DebeRetornarError()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto(null!);

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Nombre"), Is.True);
    }

    [Test]
    public async Task Validate_CuandoNombreEsMuyLargo_DebeRetornarError()
    {
        // ARRANGE
        var nombreLargo = new string('A', 51); // Más de 50 caracteres
        var dto = new CategoriaRequestDto(nombreLargo);

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Nombre"), Is.True);
    }

    [Test]
    public async Task Validate_CuandoNombreTieneLongitudMaximaPermitida_NoDebeRetornarErrores()
    {
        // ARRANGE
        var nombreMaximo = new string('A', 50); // Exactamente 50 caracteres
        var dto = new CategoriaRequestDto(nombreMaximo);

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_CuandoNombreEsMuyCorto_DebeRetornarError()
    {
        // ARRANGE
        var nombreCorto = "AB"; // Solo 2 caracteres, menos de 3
        var dto = new CategoriaRequestDto(nombreCorto);

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Nombre"), Is.True);
    }
    
    [Test]
    public async Task Validate_CuandoNombreTieneLongitudMinimaPermitida_NoDebeRetornarErrores()
    {
        // ARRANGE
        var nombreMinimo = "ABC"; // Exactamente 3 caracteres
        var dto = new CategoriaRequestDto(nombreMinimo);

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        Assert.That(result.IsValid, Is.True);
    }
}
