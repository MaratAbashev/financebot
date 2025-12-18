using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Events;
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
        
        mapGroup.MapPost("/Diagram", GetDiagram);

        mapGroup.MapPost("/Diagram/Generate", GenerateDiagram);
        
        mapGroup.MapPost("/LineChart", GetLineChart);

        mapGroup.MapPost("/LineChart/Generate", GenerateLineChart);
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
    
    private static async Task<IResult> GenerateTable(
        IReportProducer producer,
        [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var evt = new ReportGenerationEvent 
        { 
            GroupId = groupId, 
            UserId = userId, 
            Type = ReportType.ExcelTable 
        };
    
        var result = await producer.QueueReportGenerationAsync(evt);

        return result.IsSuccess
            ? Results.Accepted(value: "Request queued")
            : Results.Problem(result.ErrorMessage);
    }
    
    private static async Task<IResult> GetDiagram(IIntegrationsService integrationsService, [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GetDiagramForGroup(groupId)
            : await integrationsService.GetDiagramForUserInGroup(userId.Value, groupId);
        
        
        return result.IsSuccess
            ? Results.File(
                fileContents: result.Data, 
                contentType: "image/png", 
                fileDownloadName: $"diagram_{DateTime.Now:yyyyMMdd}.xlsx"
            )
            : Results.Problem(result.ErrorMessage);
    }
    
    private static async Task<IResult> GenerateDiagram(
        IReportProducer producer,
        [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var evt = new ReportGenerationEvent 
        { 
            GroupId = groupId, 
            UserId = userId, 
            Type = ReportType.CategoryChart 
        };
        
        var result = await producer.QueueReportGenerationAsync(evt);

        return result.IsSuccess
            ? Results.Accepted(value: "Request queued")
            : Results.Problem(result.ErrorMessage);
    }
    
    private static async Task<IResult> GetLineChart(IIntegrationsService integrationsService, [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var result = userId is null
            ? await integrationsService.GetLineChartForGroup(groupId)
            : await integrationsService.GetLineChartForUserInGroup(userId.Value, groupId);
        
        
        return result.IsSuccess
            ? Results.File(
                fileContents: result.Data, 
                contentType: "image/png", 
                fileDownloadName: $"lineChart_{DateTime.Now:yyyyMMdd}.xlsx"
            )
            : Results.Problem(result.ErrorMessage);
    }
    
    private static async Task<IResult> GenerateLineChart(
        IReportProducer producer,
        [FromQuery] Guid groupId, [FromQuery] Guid? userId)
    {
        var evt = new ReportGenerationEvent 
        { 
            GroupId = groupId, 
            UserId = userId, 
            Type = ReportType.LineChart 
        };
        
        var result = await producer.QueueReportGenerationAsync(evt);

        return result.IsSuccess
            ? Results.Accepted(value: "Request queued")
            : Results.Problem(result.ErrorMessage);
    }
}