using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace FinBot.MinIOS3;

public class MinioInitializer(IOptions<MinioOptions> options, IMinioClient client, ILogger<MinioInitializer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var bucket in options.Value.Buckets)
            await EnsureBucketExistsAsync(bucket, cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
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
