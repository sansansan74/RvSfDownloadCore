using Microsoft.Extensions.DependencyInjection;
using RvSfDownloadCore.Infrastructure;
using RvSfDownloadCore.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Старт приложения \"RvSfDownloadCore\"!");
        HostStarter.Start(args, DoWork);

        static void DoWork(IServiceProvider servicesProvider, string[] args)
        {
            var loadingBalancer = servicesProvider.GetService<LoadingBalancerService>();
            loadingBalancer.DownloadDocuments();
        }
    }
}