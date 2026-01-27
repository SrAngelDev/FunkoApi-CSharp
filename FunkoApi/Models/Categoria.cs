﻿using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Models;

[ExcludeFromCodeCoverage]
public class Categoria
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Funko>? Funkos { get; set; }
}