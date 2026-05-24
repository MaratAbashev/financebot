using FinBot.Domain.Utils;

namespace FinBot.MinIOS3;

public interface IMinioStorage
{
    Task<Result<string>> UploadAsync(string bucket, byte[] data, string objectName, string contentType,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> GetAsync(string bucket, string objectId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ExistsAsync(string bucket, string objectId,
        CancellationToken cancellationToken = default);
}
