using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

public interface IMinioStorage
{
    Task<Result<string>> UploadExcelTableAsync(byte[] data, string objectName,
        CancellationToken cancellationToken = default);
    
    Task<Result<string>> UploadDiagramImageAsync(byte[] data, string objectName,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> GetExcelTableAsync(string objectId, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> GetDiagramImageAsync(string objectId, CancellationToken cancellationToken = default);
    
    Task<Result<bool>> CheckIfTableExistsAsync(string objectId, CancellationToken cancellationToken = default);
    Task<Result<bool>> CheckIfDiagramExistsAsync(string objectId, CancellationToken cancellationToken = default);
}