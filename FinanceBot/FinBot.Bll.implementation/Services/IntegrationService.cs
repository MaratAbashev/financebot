using System.Globalization;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Implementation.Services;

public class IntegrationService(
    IMinioStorage minioStorage,
    IExcelTableService excelTableService
    ) : IIntegrationsService
{
    public async Task<Result> GenerateExcelTableForGroup(Guid groupId)
    {
        var tableName = GenerateFileName(groupId, null, "xlsx");

        var tableExistsResult = await minioStorage.CheckIfTableExistsAsync(tableName);
        if (!tableExistsResult.IsSuccess)
        {
            return tableExistsResult.SameFailure();
        }

        var isTableExists = tableExistsResult.Data;
        if (isTableExists)
        {
            return Result.Success();
        }
        
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
        var tableName = GenerateFileName(groupId, null, "xlsx");

        var tableExistsResult = await minioStorage.CheckIfTableExistsAsync(tableName);
        if (!tableExistsResult.IsSuccess)
        {
            return tableExistsResult.SameFailure();
        }

        var isTableExists = tableExistsResult.Data;
        if (isTableExists)
        {
            return Result.Success();
        }
        
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

    public async Task<Result<byte[]>> GetExcelTableForGroup(Guid groupId)
    {
        var tableName = GenerateFileName(groupId, null, "xlsx");

        return await minioStorage.GetExcelTableAsync(tableName);
    }
    

    public async Task<Result<byte[]>> GetExcelTableForUserInGroup(Guid userId, Guid groupId)
    {
        var tableName = GenerateFileName(groupId, userId, "xlsx");
        
        return await minioStorage.GetExcelTableAsync(tableName);
    }

    private string GenerateFileName(Guid groupId, Guid? userId, string extension)
    {
        var dateNow = DateTime.Now;

        var DateStr = dateNow.ToString("yyyyMMdd");
        
        return userId is not null
            ? $"{groupId}_{userId}_{DateStr}.{extension}"
            : $"{groupId}_{DateStr}.{extension}";
    }
}