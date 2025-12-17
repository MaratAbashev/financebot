using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

public interface IExcelTableService
{
    Task<Result<byte[]>> ExportToExcelForGroupAsync(Guid groupId);
    Task<Result<byte[]>> ExportToExcelForUserInGroupAsync(Guid userId, Guid groupId);
}