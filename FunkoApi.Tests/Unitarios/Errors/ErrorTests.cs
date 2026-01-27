using FunkoApi.Errors;
using FunkoApi.Errors.Funkos;
using FunkoApi.Errors.Categorias;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class ErrorTests
{
    [Test]
    public void FunkoError_NotFound_DebeRetornarNotFoundError()
    {
        // ARRANGE
        long id = 123;

        // ACT
        var error = FunkoError.NotFound(id);

        // ASSERT
        Assert.That(error, Is.InstanceOf<NotFoundError>());
        Assert.That(error.Message, Does.Contain("123"));
        Assert.That(error.Code, Is.EqualTo(404));
    }

    [Test]
    public void FunkoError_NombreDuplicado_DebeRetornarConflictError()
    {
        // ARRANGE
        string nombre = "Batman";

        // ACT
        var error = FunkoError.NombreDuplicado(nombre);

        // ASSERT
        Assert.That(error, Is.InstanceOf<ConflictError>());
        Assert.That(error.Message, Does.Contain("Batman"));
        Assert.That(error.Code, Is.EqualTo(409));
    }

    [Test]
    public void FunkoError_PrecioInvalido_DebeRetornarBusinessRuleError()
    {
        // ARRANGE
        decimal precio = -5.0m;

        // ACT
        var error = FunkoError.PrecioInvalido(precio);

        // ASSERT
        Assert.That(error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(error.Message, Does.Contain("-5"));
        Assert.That(error.Code, Is.EqualTo(400));
    }

    [Test]
    public void FunkoError_CategoriaNoEncontrada_DebeRetornarBusinessRuleError()
    {
        // ARRANGE
        string categoriaNombre = "Marvel";

        // ACT
        var error = FunkoError.CategoriaNoEncontrada(categoriaNombre);

        // ASSERT
        Assert.That(error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(error.Message, Does.Contain("Marvel"));
        Assert.That(error.Code, Is.EqualTo(400));
    }

    [Test]
    public void FunkoError_ErrorDeValidacion_DebeRetornarBusinessRuleError()
    {
        // ARRANGE
        string mensaje = "Datos inválidos";

        // ACT
        var error = FunkoError.ErrorDeValidacion(mensaje);

        // ASSERT
        Assert.That(error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(error.Message, Is.EqualTo("Datos inválidos"));
        Assert.That(error.Code, Is.EqualTo(400));
    }

    [Test]
    public void CategoriaError_NotFound_DebeRetornarNotFoundError()
    {
        // ARRANGE
        var id = Guid.NewGuid();

        // ACT
        var error = CategoriaError.NotFound(id);

        // ASSERT
        Assert.That(error, Is.InstanceOf<NotFoundError>());
        Assert.That(error.Message, Does.Contain(id.ToString()));
        Assert.That(error.Code, Is.EqualTo(404));
    }

    [Test]
    public void CategoriaError_NombreDuplicado_DebeRetornarConflictError()
    {
        // ARRANGE
        string nombre = "Marvel";

        // ACT
        var error = CategoriaError.NombreDuplicado(nombre);

        // ASSERT
        Assert.That(error, Is.InstanceOf<ConflictError>());
        Assert.That(error.Message, Does.Contain("Marvel"));
        Assert.That(error.Code, Is.EqualTo(409));
    }

    [Test]
    public void CategoriaError_TieneFunkos_DebeRetornarBusinessRuleError()
    {
        // ARRANGE
        var id = Guid.NewGuid();

        // ACT
        var error = CategoriaError.TieneFunkos(id);

        // ASSERT
        Assert.That(error, Is.InstanceOf<BusinessRuleError>());
        Assert.That(error.Message, Does.Contain(id.ToString()));
        Assert.That(error.Code, Is.EqualTo(400));
    }
}
