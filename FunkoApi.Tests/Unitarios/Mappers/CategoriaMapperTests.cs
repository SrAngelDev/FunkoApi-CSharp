using FunkoApi.Dtos;
using FunkoApi.Mappers;
using FunkoApi.Models;
using NUnit.Framework;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
public class CategoriaMapperTests
{
    [Test]
    public void ToResponseDto_CuandoCategoriaEsValida_DebeMapearcorrectamente()
    {
        // ARRANGE
        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ACT
        var result = categoria.ToResponseDto();

        // ASSERT
        Assert.That(result.Id, Is.EqualTo(categoria.Id));
        Assert.That(result.Nombre, Is.EqualTo(categoria.Nombre));
    }

    [Test]
    public void ToEntity_CuandoDtoEsValido_DebeMapearcorrectamente()
    {
        // ARRANGE
        var dto = new CategoriaRequestDto("DC");

        // ACT
        var result = dto.ToEntity();

        // ASSERT
        Assert.That(result.Nombre, Is.EqualTo("DC"));
        Assert.That(result.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(result.UpdatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void ToResponseDto_ConMultiplesCategorias_DebeMapearcorrectamente()
    {
        // ARRANGE
        var categorias = new List<Categoria>
        {
            new Categoria { Id = Guid.NewGuid(), Nombre = "Marvel" },
            new Categoria { Id = Guid.NewGuid(), Nombre = "DC" },
            new Categoria { Id = Guid.NewGuid(), Nombre = "Anime" }
        };

        // ACT
        var results = categorias.Select(c => c.ToResponseDto()).ToList();

        // ASSERT
        Assert.That(results.Count, Is.EqualTo(3));
        Assert.That(results[0].Nombre, Is.EqualTo("Marvel"));
        Assert.That(results[1].Nombre, Is.EqualTo("DC"));
        Assert.That(results[2].Nombre, Is.EqualTo("Anime"));
    }

    [Test]
    public void ToEntity_ConMultiplesDtos_DebeMapearcorrectamente()
    {
        // ARRANGE
        var dtos = new List<CategoriaRequestDto>
        {
            new CategoriaRequestDto("Marvel"),
            new CategoriaRequestDto("DC")
        };

        // ACT
        var results = dtos.Select(d => d.ToEntity()).ToList();

        // ASSERT
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].Nombre, Is.EqualTo("Marvel"));
        Assert.That(results[1].Nombre, Is.EqualTo("DC"));
    }
}
