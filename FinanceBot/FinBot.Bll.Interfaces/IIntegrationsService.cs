using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces;

public interface IIntegrationsService
{
    Task<Result> GenerateExcelTableForGroup(Guid groupId, int months);
    Task<Result> GenerateExcelTableForUserInGroup(Guid userId, Guid groupId, int months);
    
    Task<Result<byte[]>> GetExcelTableForGroup(Guid groupId, int months);
    Task<Result<byte[]>> GetExcelTableForUserInGroup(Guid userId, Guid groupId, int months);
}