﻿using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Dtos;

[ExcludeFromCodeCoverage]
public record CategoriaResponseDto(
    Guid Id,
    string Nombre
    );