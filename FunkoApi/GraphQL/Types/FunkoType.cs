﻿using System.Diagnostics.CodeAnalysis;
using FunkoApi.Models;

namespace FunkoApi.GraphQL.Types;

[ExcludeFromCodeCoverage]
public class FunkoType : ObjectType<Funko>
{
    protected override void Configure(IObjectTypeDescriptor<Funko> descriptor)
    {
        descriptor.BindFieldsExplicitly(); // Ocultamos todo por defecto

        descriptor.Field(f => f.Id).Type<IdType>(); // ID propio de GraphQL
        descriptor.Field(f => f.Nombre);
        descriptor.Field(f => f.Precio);
        descriptor.Field(f => f.Imagen);
        descriptor.Field(f => f.CreatedAt);
        
        // Relación: Permitimos navegar a la categoría
        descriptor.Field(f => f.Categoria)
            .Type<CategoriaType>();
    }
}