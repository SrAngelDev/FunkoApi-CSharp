using FluentValidation.TestHelper;
using FunkoApi.Dtos;
using FunkoApi.Validators.Funkos;
using NUnit.Framework;

namespace FunkoApi.Tests.Validators;

[TestFixture]
public class FunkoRequestValidatorTests
{
    private FunkoRequestValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new FunkoRequestValidator();
    }
    
    // TESTS DEL CAMPO NOMBRE
    
    [Test]
    public void Nombre_CuandoEsVacio_DebeFallar()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("", Guid.NewGuid(), 20.0m);

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre del Funko es obligatorio.");
    }

    [Test]
    public void Nombre_CuandoExcedeLongitudMaxima_DebeFallar()
    {
        // ARRANGE
        var nombreLargo = new string('A', 101); // 101 caracteres
        var dto = new FunkoRequestDto(nombreLargo, Guid.NewGuid(), 20.0m);

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre no puede exceder 100 caracteres.");
    }

    [Test]
    public void Nombre_CuandoEsValido_NoDebeFallar()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("Batman", Guid.NewGuid(), 20.0m);

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }
    
    // TESTS DEL CAMPO PRECIO

    [Test]
    public void Precio_CuandoEsCero_DebeFallar()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("Batman", Guid.NewGuid(), 0m);

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Precio)
            .WithErrorMessage("El precio debe ser un valor positivo");
    }

    [Test]
    public void Precio_CuandoEsNegativo_DebeFallar()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("Batman", Guid.NewGuid(), -10.5m);

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Precio)
            .WithErrorMessage("El precio debe ser un valor positivo");
    }

    [Test]
    public void Precio_CuandoEsPositivo_NoDebeFallar()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("Batman", Guid.NewGuid(), 25.99m);

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
    }
    
    // TEST DE VALIDACIÓN COMPLETA

    [Test]
    public void ValidarDto_CuandoTodosLosCamposSonValidos_NoDebeFallar()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("Superman", Guid.NewGuid(), 30.50m);

        // ACT
        var result = _validator.TestValidate(dto);

        // ASSERT
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ValidarDto_CuandoMultiplesCamposSonInvalidos_DebeRetornarMultiplesErrores()
    {
        // ARRANGE
        var dto = new FunkoRequestDto("", Guid.NewGuid(), -5m);

        // ACT
        var result = _validator.TestValidate(dto);

        // ASSERT
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
        result.ShouldHaveValidationErrorFor(x => x.Precio);
    }
}
