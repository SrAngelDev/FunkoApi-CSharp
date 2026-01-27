﻿using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Models;

[ExcludeFromCodeCoverage]
public class Funko
{
    public long Id { get; set; }
    public required string Nombre { get; set; }
    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public decimal Precio { get; set; }
    public string Imagen { get; set; } = "https://placehold.co/600x400.png"; 
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}