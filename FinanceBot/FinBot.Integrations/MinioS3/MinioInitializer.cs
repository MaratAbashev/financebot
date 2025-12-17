using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace FinBot.Integrations.MinioS3;

public class MinioInitializer(IOptions<MinioOptions> options, ILogger<MinioInitializer> logger, IMinioClient client)
    : IHostedService
{
    private readonly MinioOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeBucketAsync(_options.Buckets.ExcelTablesBucket, cancellationToken);
        await InitializeBucketAsync(_options.Buckets.DiagramImagesBucket, cancellationToken);
    }

    private async Task InitializeBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var exists = await client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucketName),
                cancellationToken);

            if (!exists)
            {
                logger.LogInformation("Creating bucket {BucketName}", bucketName);
                await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing bucket {BucketName}", bucketName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}