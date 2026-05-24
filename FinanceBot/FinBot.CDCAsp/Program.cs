using FinBot.CDCAsp;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.Observability;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Simpl;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddReadDb(builder.Configuration);
builder.Services.AddReplicaDb(builder.Configuration);
builder.Services.AddHostedService<ReplicaConsumerService>();

builder.Services.AddSingleton<ScheduledCdcJob>();
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ScheduledCdcJob");
    
    q.AddJob<ScheduledCdcJob>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ScheduledCdcTrigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInMinutes(1) //5 минут для тестов и показа (должно быть каждую ночь)
            .RepeatForever()
            .WithMisfireHandlingInstructionFireNow())
    );
    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

using var scope = app.Services.CreateScope();

await using var readDb = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
await using var replicaDb = scope.ServiceProvider.GetRequiredService<ReplicaDbContext>();

await readDb.Database.MigrateAsync();
await replicaDb.Database.MigrateAsync();

app.Run();