using FinBot.Bll.Interfaces.Integration;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace FinBot.Integrations.MinioS3;

public class MinioStorage(IOptions<MinioOptions> options, IMinioClient client, ILogger<MinioStorage> logger) :
    IMinioStorage
{
    private readonly MinioOptions _options = options.Value;

    private async Task<Result<string>> UploadToBucketAsync(string bucket, Stream data,
        string contentType, CancellationToken token)
    {
        try
        {
            if (data.CanSeek) data.Position = 0;
            
            var objectName = Guid.NewGuid().ToString();
            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(data.Length)
                .WithContentType(contentType);

            await client.PutObjectAsync(args, token);
            
            return Result<string>.Success(objectName);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to upload: {ex.Message}");
        }
    }

    private async Task<Result<Stream>> GetFileAsync(string objectId, string bucket, CancellationToken token = default)
    {
        try
        {
            var memoryStream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectId)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await client.GetObjectAsync(args, token);

            memoryStream.Position = 0;

            return Result<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get file: {ObjectId} from {bucketName}", objectId, bucket);
            return Result<Stream>.Failure($"Failed to get file: {ex.Message}");
        }
    }
    

    public async Task<Result<string>> UploadExcelTableAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uploading excel table...");
        return await UploadToBucketAsync(
            bucket: _options.Buckets.ExcelTablesBucket,
            data: data,
            contentType: contentType,
            token: cancellationToken);
    }

    public async Task<Result<string>> UploadDiagramImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uploading diagram image...");
        return await UploadToBucketAsync(
            bucket: _options.Buckets.DiagramImagesBucket,
            data: data,
            contentType: contentType,
            token: cancellationToken);
    }


    public async Task<Result<Stream>> GetExcelTableAsync(string objectId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading excel table...");
        return await GetFileAsync(
            objectId: objectId,
            bucket: _options.Buckets.ExcelTablesBucket,
            token: cancellationToken);
    }

    public async Task<Result<Stream>> GetDiagramImageAsync(string objectId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading diagram image...");
        return await GetFileAsync(
            objectId: objectId,
            bucket: _options.Buckets.DiagramImagesBucket,
            token: cancellationToken);
    }
}