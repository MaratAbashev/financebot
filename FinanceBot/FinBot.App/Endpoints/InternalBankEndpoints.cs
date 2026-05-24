using FinBot.App.Extensions;
using FinBot.Bll.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.App.Endpoints;

public static class InternalBankEndpoints
{
        public static void MapInternalBankEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/InternalBank")
            .WithTags("InternalBank")
            .WithOpenApi();

        group.MapPost("/Check", CheckUserSync)
            .WithName("CheckUserSync")
            .WithDescription("Проверить синхронизацию пользователя с банком")
            .Produces<bool>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CheckUserSync(
        [FromQuery] long userTgId,
        IInternalBankService internalBankService)
    {
        var result = await internalBankService.IsBankConnectedAsync(userTgId, ct: CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : result.ToErrorHttpResult();
    }
}