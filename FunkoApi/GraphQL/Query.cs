﻿using System.Diagnostics.CodeAnalysis;
using FunkoApi.Dtos;
using FunkoApi.Services.Funkos;

namespace FunkoApi.GraphQL;

[ExcludeFromCodeCoverage]
public class Query
{
    public async Task<IEnumerable<FunkoResponseDto>> GetFunkos([Service] IFunkoService service)
    {
        return await service.GetAllAsync();
    }

    public async Task<FunkoResponseDto> GetFunko(long id, [Service] IFunkoService service)
    {
        var result = await service.GetByIdAsync(id);
        
        if (result.IsFailure)
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage(result.Error.Message)
                .SetCode("NOT_FOUND")
                .Build());
        }

        return result.Value;
    }
}