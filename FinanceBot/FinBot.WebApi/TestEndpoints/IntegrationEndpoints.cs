using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.WebApi.TestEndpoints;

public static class IntegrationEndpoints
{
    public static void MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var mapGroup = app.MapGroup("/Integrations")
            .WithTags("Integrations")
            .WithOpenApi();

        mapGroup.MapPost("/Table", GetTable);

        mapGroup.MapPost("/Table/Generate", GenerateTable);
    }

    private static async Task<IResult> GetTable(IIntegrationsService integrationsService, [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GetExcelTableForGroup(groupId)
            : await integrationsService.GetExcelTableForUserInGroup(userId.Value, groupId);
        
        
        return result.IsSuccess
            ? Results.File(
                fileContents: result.Data, 
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileDownloadName: $"expenses_{DateTime.Now:yyyyMMdd}.xlsx"
            )
            : Results.Problem(result.ErrorMessage);
    }
    
    private static async Task<IResult> GenerateTable(IIntegrationsService integrationsService, [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GenerateExcelTableForGroup(groupId)
            : await integrationsService.GenerateExcelTableForUserInGroup(userId.Value, groupId);
        
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.ErrorMessage);
    }
}