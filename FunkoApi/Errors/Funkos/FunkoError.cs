﻿using System.Diagnostics.CodeAnalysis;

namespace FunkoApi.Errors.Funkos;

[ExcludeFromCodeCoverage]
public static class FunkoError
{
    public static NotFoundError NotFound(long id) => new($"Funko con ID {id} no encontrado");
    public static ConflictError NombreDuplicado(string nombre) => new($"El Funko con nombre '{nombre}' ya existe");
    public static BusinessRuleError PrecioInvalido(decimal precio) => new($"El precio {precio} no es válido. Debe ser mayor que cero");
    public static BusinessRuleError CategoriaNoEncontrada(string categoriaNombre) =>
        new($"La categoría '{categoriaNombre}' no existe y no se puede asignar al Funko");
    public static BusinessRuleError ErrorDeValidacion(string mensaje) => new(mensaje);
}