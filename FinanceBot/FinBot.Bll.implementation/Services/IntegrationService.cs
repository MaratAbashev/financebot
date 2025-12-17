using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Implementation.Services;

public class IntegrationService(
    IMinioStorage minioStorage,
    IExcelTableService excelTableService
    ) : IIntegrationsService
{
    public async Task<Result> GenerateExcelTableForGroup(Guid groupId, int months)
    {
        var tableName = GenerateFileName(groupId, null, months, "xlsx");

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
        
        var createTableResult = await excelTableService.ExportToExcelForGroupAsync(groupId, months);
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

    public async Task<Result> GenerateExcelTableForUserInGroup(Guid userId, Guid groupId, int months)
    {
        var tableName = GenerateFileName(groupId, null, months, "xlsx");

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
        
        var createTableResult = await excelTableService.ExportToExcelForUserInGroupAsync(userId, groupId, months);
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

    public async Task<Result<byte[]>> GetExcelTableForGroup(Guid groupId, int months)
    {
        var tableName = GenerateFileName(groupId, null, months, "xlsx");

        return await minioStorage.GetExcelTableAsync(tableName);
    }

    public async Task<Result<byte[]>> GetExcelTableForUserInGroup(Guid userId, Guid groupId, int months)
    {
        var tableName = GenerateFileName(groupId, userId, months, "xlsx");
        
        return await minioStorage.GetExcelTableAsync(tableName);
    }

    private string GenerateFileName(Guid groupId, Guid? userId, int months, string extension)
    {
        var dateNow = DateTime.Now;
        string fileName;

        var finishDateStr = dateNow.ToString("yyyyMM");

        if (months == 1)
        {
            fileName = userId is not null
                ? $"{groupId}_{userId}_{finishDateStr}.{extension}"
                : $"{groupId}_{finishDateStr}.{extension}";
        }
        else
        {
            var startDate = dateNow.AddMonths(-(months - 1));
            var startDateStr = startDate.ToString("yyyyMM");

            fileName = userId is not null
            ? $"{groupId}_{userId}_{startDateStr}_{finishDateStr}.{extension}"
            : $"{groupId}_{startDateStr}_{finishDateStr}.{extension}";
        }
        
        return fileName;
    }
}