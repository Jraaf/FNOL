using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Infrastructure.BackgroundJobs;
using ClaimsModule.Infrastructure.Common;
using ClaimsModule.Infrastructure.Policies;
using ClaimsModule.Infrastructure.Storage;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimsModule.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddClaimsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddSingleton<IPolicyService, SeededPolicyService>();

        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        var storageProvider = config.GetSection(StorageOptions.SectionName)["Provider"] ?? "LocalFileSystem";
        if (string.Equals(storageProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IStorageService, AzureBlobStorageService>();
        else
            services.AddScoped<IStorageService, LocalFileSystemStorageService>();

        var hangfireConnection = config.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(hangfireConnection))
            hangfireConnection = config.GetConnectionString("ClaimsDb");
        if (string.IsNullOrWhiteSpace(hangfireConnection))
            throw new InvalidOperationException(
                "Hangfire requires either ConnectionStrings:Hangfire or ConnectionStrings:ClaimsDb to be set.");
        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            cfg.UseSimpleAssemblyNameTypeSerializer();
            cfg.UseRecommendedSerializerSettings();
            cfg.UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true,
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15)
            });
        });
        services.AddHangfireServer();
        services.AddScoped<GlPostingJob>();
        services.AddScoped<SlaMonitorJob>();
        services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        return services;
    }

    public static void ConfigureRecurringJobs(IServiceProvider sp)
    {
        var recurring = sp.GetRequiredService<IRecurringJobManager>();
        recurring.AddOrUpdate<SlaMonitorJob>(
            SlaMonitorJob.RecurringJobId,
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *");
    }
}
