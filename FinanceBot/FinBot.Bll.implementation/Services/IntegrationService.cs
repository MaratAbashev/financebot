using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Implementation.Services;

public class IntegrationService(
    IMinioStorage minioStorage,
    IExcelTableService excelTableService,
    IChartService chartService
) : IIntegrationsService
{
    public async Task<Result> GenerateExcelTableForGroup(Guid groupId)
    {
        var tableName = GenerateFileName(groupId, null, "sheet", "xlsx");

        var createTableResult = await excelTableService.ExportToExcelForGroupAsync(groupId);
        if (!createTableResult.IsSuccess)
        {
            return createTableResult.SameFailure();
        }

        var saveTableResult = await minioStorage.UploadExcelTableAsync(createTableResult.Data, tableName);
        if (!saveTableResult.IsSuccess)
        {
            return saveTableResult.SameFailure();
        }

        return Result.Success();
    }
    public async Task<Result> GenerateExcelTableForUserInGroup(Guid userId, Guid groupId)
    {
        var tableName = GenerateFileName(groupId, null, "sheet", "xlsx");

        var createTableResult = await excelTableService.ExportToExcelForUserInGroupAsync(userId, groupId);
        if (!createTableResult.IsSuccess)
        {
            return createTableResult.SameFailure();
        }

        var saveTableResult = await minioStorage.UploadExcelTableAsync(createTableResult.Data, tableName);
        if (!saveTableResult.IsSuccess)
        {
            return saveTableResult.SameFailure();
        }

        return Result.Success();
    }
    
    public async Task<Result> GenerateDiagramForGroup(Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, null, "diagram", "xlsx");

        var createDiagramResult = await chartService.GenerateCategoryChartForGroupAsync(groupId);
        if (!createDiagramResult.IsSuccess)
        {
            return createDiagramResult.SameFailure();
        }

        var saveDiagramResult = await minioStorage.UploadDiagramImageAsync(createDiagramResult.Data, diagramName);
        if (!saveDiagramResult.IsSuccess)
        {
            return saveDiagramResult.SameFailure();
        }

        return Result.Success();
    }
    public async Task<Result> GenerateDiagramForUserInGroup(Guid userId, Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, userId, "diagram", "xlsx");

        var createDiagramResult = await chartService.GenerateCategoryChartForUserInGroupAsync(userId, groupId);
        if (!createDiagramResult.IsSuccess)
        {
            return createDiagramResult.SameFailure();
        }

        var saveDiagramResult = await minioStorage.UploadDiagramImageAsync(createDiagramResult.Data, diagramName);
        if (!saveDiagramResult.IsSuccess)
        {
            return saveDiagramResult.SameFailure();
        }

        return Result.Success();
    }

    public async Task<Result> GenerateLineChartForGroup(Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, null, "lineChart", "xlsx");

        var createDiagramResult = await chartService.GenerateSpendingDiagramForGroupAsync(groupId);
        if (!createDiagramResult.IsSuccess)
        {
            return createDiagramResult.SameFailure();
        }

        var saveDiagramResult = await minioStorage.UploadDiagramImageAsync(createDiagramResult.Data, diagramName);
        if (!saveDiagramResult.IsSuccess)
        {
            return saveDiagramResult.SameFailure();
        }

        return Result.Success();
    }
    
    public async Task<Result> GenerateLineChartForUserInGroup(Guid userId, Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, userId, "lineChart", "xlsx");

        var createDiagramResult = await chartService.GenerateSpendingDiagramForUserInGroupAsync(userId, groupId);
        if (!createDiagramResult.IsSuccess)
        {
            return createDiagramResult.SameFailure();
        }

        var saveDiagramResult = await minioStorage.UploadDiagramImageAsync(createDiagramResult.Data, diagramName);
        if (!saveDiagramResult.IsSuccess)
        {
            return saveDiagramResult.SameFailure();
        }

        return Result.Success();
    }

    public async Task<Result<byte[]>> GetExcelTableForGroup(Guid groupId)
    {
        var tableName = GenerateFileName(groupId, null, "sheet", "xlsx");

        return await minioStorage.GetExcelTableAsync(tableName);
    }
    public async Task<Result<byte[]>> GetExcelTableForUserInGroup(Guid userId, Guid groupId)
    {
        var tableName = GenerateFileName(groupId, userId, "sheet", "xlsx");

        return await minioStorage.GetExcelTableAsync(tableName);
    }

    public async Task<Result<byte[]>> GetDiagramForGroup(Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, null, "diagram", "xlsx");

        return await minioStorage.GetDiagramImageAsync(diagramName);
    }
    public async Task<Result<byte[]>> GetDiagramForUserInGroup(Guid userId, Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, userId, "diagram", "xlsx");

        return await minioStorage.GetDiagramImageAsync(diagramName);
    }

    public async Task<Result<byte[]>> GetLineChartForGroup(Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, null, "lineChart", "xlsx");

        return await minioStorage.GetDiagramImageAsync(diagramName);
    }
    public async Task<Result<byte[]>> GetLineChartForUserInGroup(Guid userId, Guid groupId)
    {
        var diagramName = GenerateFileName(groupId, userId, "lineChart", "xlsx");

        return await minioStorage.GetDiagramImageAsync(diagramName);
    }

    private string GenerateFileName(Guid groupId, Guid? userId, string type, string extension)
    {
        var dateNow = DateTime.Now;
        
        return userId is not null
            ? $"{type}_{groupId}_{userId}.{extension}"
            : $"{type}_{groupId}.{extension}";
    }
}