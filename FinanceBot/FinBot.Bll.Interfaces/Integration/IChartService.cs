using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

public interface IChartService
{
    Task<Result<byte[]>> GenerateCategoryChartForGroupAsync(Guid groupId);
    Task<Result<byte[]>> GenerateCategoryChartForUserInGroupAsync(Guid userId, Guid groupId);
    Task<Result<byte[]>> GenerateSpendingDiagramForGroupAsync(Guid groupId);
    Task<Result<byte[]>> GenerateSpendingDiagramForUserInGroupAsync(Guid userId, Guid groupId);
}