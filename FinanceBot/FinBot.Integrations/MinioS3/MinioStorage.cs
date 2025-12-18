using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace FinBot.Integrations.MinioS3;

public class MinioStorage(IOptions<MinioOptions> options, IMinioClient client, ILogger<MinioStorage> logger) :
    IMinioStorage
{
    private readonly MinioOptions _options = options.Value;

    private async Task<Result<string>> UploadToBucketAsync(string bucket, byte[] data, string objectName,
        string contentType, CancellationToken token)
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

            await client.PutObjectAsync(args, token);

            return Result<string>.Success(objectName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload {objectName}to bucket {Bucket}", objectName, bucket);
            return Result<string>.Failure($"Failed to upload: {ex.Message}");
        }
    }

    private async Task<Result<byte[]>> GetFileAsync(string objectId, string bucket, CancellationToken token = default)
    {
        try
        {
            var statArgs = new StatObjectArgs().WithBucket(bucket).WithObject(objectId);
            try 
            {
                await client.StatObjectAsync(statArgs, token);
            }
            catch (MinioException)
            {
                return Result<byte[]>.Failure("File not found", ErrorType.NotFound);
            }

            var memoryStream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectId)
                .WithCallbackStream(stream => 
                {
                    stream.CopyTo(memoryStream);
                });
        
            await client.GetObjectAsync(args, token);

            return Result<byte[]>.Success(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get file: {ObjectId} from {bucketName}", objectId, bucket);
            return Result<byte[]>.Failure($"Failed to get file: {ex.Message}");
        }
    }


    public async Task<Result<string>> UploadExcelTableAsync(byte[] data, string objectName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uploading excel table...");
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    
        return await UploadToBucketAsync(
            bucket: _options.Buckets.ExcelTablesBucket,
            data: data,
            objectName: objectName,
            contentType: contentType,
            token: cancellationToken);
    }

    public async Task<Result<string>> UploadDiagramImageAsync(byte[] data, string objectName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uploading diagram image...");
        string contentType = "image/png";
        
        return await UploadToBucketAsync(
            bucket: _options.Buckets.DiagramImagesBucket,
            data: data,
            objectName: objectName,
            contentType: contentType,
            token: cancellationToken);
    }


    public async Task<Result<byte[]>> GetExcelTableAsync(string objectId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading excel table...");
        return await GetFileAsync(
            objectId: objectId,
            bucket: _options.Buckets.ExcelTablesBucket,
            token: cancellationToken);
    }

    public async Task<Result<byte[]>> GetDiagramImageAsync(string objectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading diagram image...");
        return await GetFileAsync(
            objectId: objectId,
            bucket: _options.Buckets.DiagramImagesBucket,
            token: cancellationToken);
    }

    private async Task<Result<bool>> CheckFileExistsAsync(string objectId, string bucket, CancellationToken token)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectId);

            await client.StatObjectAsync(args, token);

            return Result<bool>.Success(true);
        }
        catch (MinioException ex) when (ex is ObjectNotFoundException ||
                                        ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check existence of file: {ObjectId} in {Bucket}", objectId, bucket);
            return Result<bool>.Failure($"Error checking file existence: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CheckIfTableExistsAsync(string objectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking if excel table exists: {ObjectId}...", objectId);
        return await CheckFileExistsAsync(
            objectId: objectId,
            bucket: _options.Buckets.ExcelTablesBucket,
            token: cancellationToken);
    }

    public async Task<Result<bool>> CheckIfDiagramExistsAsync(string objectId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking if diagram image exists: {ObjectId}...", objectId);
        return await CheckFileExistsAsync(
            objectId: objectId,
            bucket: _options.Buckets.DiagramImagesBucket,
            token: cancellationToken);
    }
}