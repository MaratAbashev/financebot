using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces;

public interface IIntegrationsService
{
    Task<Result> GenerateExcelTableForGroup(Guid groupId);
    Task<Result> GenerateExcelTableForUserInGroup(Guid userId, Guid groupId);
    
    Task<Result> GenerateDiagramForGroup(Guid groupId);
    Task<Result> GenerateDiagramForUserInGroup(Guid userId, Guid groupId);
    
    Task<Result> GenerateLineChartForGroup(Guid groupId);
    Task<Result> GenerateLineChartForUserInGroup(Guid userId, Guid groupId);
    
    
    Task<Result<byte[]>> GetExcelTableForGroup(Guid groupId);
    Task<Result<byte[]>> GetExcelTableForUserInGroup(Guid userId, Guid groupId);
    
    Task<Result<byte[]>> GetDiagramForGroup(Guid groupId);
    Task<Result<byte[]>> GetDiagramForUserInGroup(Guid userId, Guid groupId);
    
    Task<Result<byte[]>> GetLineChartForGroup(Guid groupId);
    Task<Result<byte[]>> GetLineChartForUserInGroup(Guid userId, Guid groupId);
}