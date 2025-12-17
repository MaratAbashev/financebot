using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces;

public interface IIntegrationsService
{
    Task<Result> GenerateExcelTableForGroup(Guid groupId);
    Task<Result> GenerateExcelTableForUserInGroup(Guid userId, Guid groupId);
    
    Task<Result<byte[]>> GetExcelTableForGroup(Guid groupId);
    Task<Result<byte[]>> GetExcelTableForUserInGroup(Guid userId, Guid groupId);
}