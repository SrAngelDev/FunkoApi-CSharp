﻿using System.Diagnostics.CodeAnalysis;
using FunkoApi.Dtos;
using FunkoApi.Services.Funkos;
using HotChocolate.Authorization;

namespace FunkoApi.GraphQL;

[ExcludeFromCodeCoverage]
public class Mutation
{
    // Usamos "Policy" en lugar de "Roles"
    [Authorize(Policy = "AdminPolicy")] 
    public async Task<FunkoResponseDto> CreateFunko(
        FunkoRequestDto funkoDto, 
        [Service] IFunkoService service)
    {
        var result = await service.CreateAsync(funkoDto);

        if (result.IsFailure)
        {
            // Convertimos el AppError de ROP a un error de GraphQL
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage(result.Error.Message)
                .SetCode("VALIDATION_ERROR")
                .Build());
        }

        return result.Value;
    }

    [Authorize(Policy = "AdminPolicy")]
    public async Task<bool> DeleteFunko(long id, [Service] IFunkoService service)
    {
        var result = await service.DeleteAsync(id);
        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }
        return true;
    }
}