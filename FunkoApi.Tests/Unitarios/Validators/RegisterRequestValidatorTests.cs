using FluentValidation.TestHelper;
using FunkoApi.Dtos;
using FunkoApi.Validators.Auth;
using NUnit.Framework;

namespace FunkoApi.Tests.Validators;

[TestFixture]
public class RegisterRequestValidatorTests
{
    private RegisterRequestValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new RegisterRequestValidator();
    }

    // ============================================
    // TESTS DEL CAMPO USERNAME
    // ============================================

    [Test]
    public void Username_CuandoEsVacio_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("", "Password123", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El nombre de usuario es obligatorio");
    }

    [Test]
    public void Username_CuandoTieneMenosDe3Caracteres_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("ab", "Password123", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El usuario debe tener al menos 3 caracteres");
    }

    [Test]
    public void Username_CuandoEsValido_NoDebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "Password123", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    // ============================================
    // TESTS DEL CAMPO EMAIL
    // ============================================

    [Test]
    public void Email_CuandoEsVacio_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "Password123", "");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El email es obligatorio");
    }

    [Test]
    public void Email_CuandoFormatoEsInvalido_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "Password123", "invalid-email");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El formato del email no es válido");
    }

    [Test]
    public void Email_CuandoFormatoEsValido_NoDebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "Password123", "valid@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    // ============================================
    // TESTS DEL CAMPO PASSWORD
    // ============================================

    [Test]
    public void Password_CuandoEsVacio_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña es obligatoria");
    }

    [Test]
    public void Password_CuandoTieneMenosDe6Caracteres_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "Pass1", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña debe tener al menos 6 caracteres");
    }

    [Test]
    public void Password_CuandoNoTieneMayuscula_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "password123", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña debe contener al menos una letra mayúscula");
    }

    [Test]
    public void Password_CuandoNoTieneMinuscula_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "PASSWORD123", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña debe contener al menos una letra minúscula");
    }

    [Test]
    public void Password_CuandoNoTieneNumero_DebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "PasswordABC", "test@example.com");

        // ACT & ASSERT
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña debe contener al menos un número");
    }

    [Test]
    public void Password_CuandoCumpleTodasLasReglas_NoDebeFallar()
    {
        // ARRANGE
        var dto = new RegisterDto("testuser", "Password123", "test@example.com");

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
        var dto = new RegisterDto("validuser", "SecurePass123", "valid@example.com");

        // ACT
        var result = _validator.TestValidate(dto);

        // ASSERT
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ValidarDto_CuandoTodosLosCamposSonInvalidos_DebeRetornarMultiplesErrores()
    {
        // ARRANGE
        var dto = new RegisterDto("ab", "pass", "invalid-email");

        // ACT
        var result = _validator.TestValidate(dto);

        // ASSERT
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
