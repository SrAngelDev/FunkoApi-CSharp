﻿using System.Diagnostics.CodeAnalysis;
using FunkoApi.GraphQL;
using FunkoApi.GraphQL.Types;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunkoApi.Configuration;

[ExcludeFromCodeCoverage]
public static class GraphQLConfig
{
    public static IServiceCollection AddGraphQLConfig(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddType<FunkoType>()
            .AddType<CategoriaType>()
            .AddFiltering()
            .AddSorting()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true); 

        return services;
    }
}