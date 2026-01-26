using FluentValidation.TestHelper;
using FunkoApi.Dtos;
using FunkoApi.Validators.Auth;
using NUnit.Framework;

namespace FunkoApi.Tests.Validators;

[TestFixture]
public class LoginRequestValidatorTests
{
    private LoginRequestValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new LoginRequestValidator();
    }

    // ============================================
    // TESTS DEL CAMPO USERNAME
    // ============================================

    [Test]
    public void Username_CuandoEsVacio_DebeFallar()
    {
        // ARRANGE
        var dto = new LoginDto("", "Password123");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El usuario no puede estar vacío");
    }

    [Test]
    public void Username_CuandoEsValido_NoDebeFallar()
    {
        // ARRANGE
        var dto = new LoginDto("testuser", "Password123");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    // ============================================
    // TESTS DEL CAMPO PASSWORD
    // ============================================

    [Test]
    public void Password_CuandoEsVacio_DebeFallar()
    {
        // ARRANGE
        var dto = new LoginDto("testuser", "");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña no puede estar vacía");
    }

    [Test]
    public void Password_CuandoEsValido_NoDebeFallar()
    {
        // ARRANGE
        var dto = new LoginDto("testuser", "anypassword");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    // ============================================
    // TEST DE VALIDACIÓN COMPLETA
    // ============================================

    [Test]
    public void ValidarDto_CuandoTodosLosCamposSonValidos_NoDebeFallar()
    {
        // ARRANGE
        var dto = new LoginDto("validuser", "validpassword");

        // ACT
        var result = _validator.TestValidate(dto);

        // ASSERT
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ValidarDto_CuandoAmbosCanposSonVacios_DebeRetornarMultiplesErrores()
    {
        // ARRANGE
        var dto = new LoginDto("", "");

        // ACT
        var result = _validator.TestValidate(dto);

        // ASSERT
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
