using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

public interface IMinioStorage
{
    Task<Result<string>> UploadExcelTableAsync(Stream data, string contentType,
        CancellationToken cancellationToken = default);
    
    Task<Result<string>> UploadDiagramImageAsync(Stream data, string contentType,
        CancellationToken cancellationToken = default);

    public Task<Result<Stream>> GetExcelTableAsync(string objectId, CancellationToken cancellationToken = default);
    public Task<Result<Stream>> GetDiagramImageAsync(string objectId, CancellationToken cancellationToken = default);
}