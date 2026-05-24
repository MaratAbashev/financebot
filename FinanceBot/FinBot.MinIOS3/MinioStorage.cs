using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace FinBot.MinIOS3;

public class MinioStorage(IMinioClient client, ILogger<MinioStorage> logger) : IMinioStorage
{
    public async Task<Result<string>> UploadAsync(string bucket, byte[] data, string objectName, string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = new MemoryStream(data);

            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(data.Length)
                .WithContentType(contentType);

            await client.PutObjectAsync(args, cancellationToken);

            return Result<string>.Success(objectName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload {ObjectName} to bucket {Bucket}", objectName, bucket);
            return Result<string>.Failure($"Failed to upload: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> GetAsync(string bucket, string objectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs().WithBucket(bucket).WithObject(objectId);
            try
            {
                await client.StatObjectAsync(statArgs, cancellationToken);
            }
            catch (MinioException)
            {
                return Result<byte[]>.Failure("File not found", ErrorType.NotFound);
            }

            var memoryStream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectId)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await client.GetObjectAsync(args, cancellationToken);

            return Result<byte[]>.Success(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get {ObjectId} from bucket {Bucket}", objectId, bucket);
            return Result<byte[]>.Failure($"Failed to get file: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ExistsAsync(string bucket, string objectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new StatObjectArgs().WithBucket(bucket).WithObject(objectId);
            await client.StatObjectAsync(args, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (MinioException ex) when (ex is ObjectNotFoundException ||
                                        ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check existence of {ObjectId} in bucket {Bucket}", objectId, bucket);
            return Result<bool>.Failure($"Error checking file existence: {ex.Message}");
        }
    }
}
