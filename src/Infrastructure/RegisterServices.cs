using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using RvSfDownloadCore.Repository;
using RvSfDownloadCore.Services;
using RvSfDownloadCore.Services.DownloadServices;

namespace RvSfDownloadCore.Infrastructure
{
    /// <summary>
    /// Регистрирует сервисы
    /// </summary>
    public static class RegisterServices
    {
        public static IServiceProvider BuildDi(IConfiguration config)
        {
            return new ServiceCollection()
                .AddTransient<LoadingBalancerService>()
                .AddTransient<DbRepairRepository>()
                .AddTransient<EdoDownloadService>()
                .AddTransient<ActDownloadService>()
                .AddTransient<DiapazonDownloadService>()
                .AddTransient<RvHttpRepository>()
                .AddTransient<PrintFormDownloadService>()
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                })
                .AddSingleton(config)
                .AddSingleton<DbRepairRepository>()
                .AddSingleton<RvDownloadRepository>()
                .AddSingleton<ILogger>(svc => svc.GetRequiredService<ILogger<LoadingBalancerService>>())
                .BuildServiceProvider();
        }
    }
}
