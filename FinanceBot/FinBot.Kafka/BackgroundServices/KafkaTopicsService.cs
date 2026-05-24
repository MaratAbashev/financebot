using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.Hosting;

namespace FinBot.Kafka.BackgroundServices;

public class KafkaTopicsService(
    KafkaGlobalSettings kafkaGlobalSettings,
    IEnumerable<ITopic> topics, 
    BackgroundServicesInitManager initManager,
    IAdminClient adminClient): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var topicsSet = topics
                .Select(t => t.TopicName)
                .ToHashSet();

            HashSet<string> existingTopics;
            
            try
            {
                var describeResult = await adminClient.DescribeTopicsAsync(
                    TopicCollection.OfTopicNames(topicsSet));
                existingTopics = describeResult.TopicDescriptions
                    .Select(t => t.Name)
                    .ToHashSet();
            }
            catch (DescribeTopicsException ex)
            {
                existingTopics = ex.Results.TopicDescriptions
                    .Where(td => !td.Error.IsError)
                    .Select(td => td.Name)
                    .ToHashSet();
            }
            
            topicsSet.ExceptWith(existingTopics);
            
            foreach (var topic in topicsSet)
            {
                await adminClient.CreateTopicsAsync([
                    new TopicSpecification
                    {
                        Name = topic,
                        NumPartitions = kafkaGlobalSettings.PartitionNums,
                        ReplicationFactor = kafkaGlobalSettings.PartitionReplicationFactor
                    }
                ], new CreateTopicsOptions{ OperationTimeout = TimeSpan.FromMinutes(1) });
            }

            initManager.IsTopicsCreated = TopicCreationStatus.Created;
        }
        catch (Exception ex)
        {
            initManager.IsTopicsCreated = TopicCreationStatus.Error;
            initManager.Error = ex;
        }
        
    }
}