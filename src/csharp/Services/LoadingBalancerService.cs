using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Services.DownloadServices;
using RvSfDownloadCore.Services.Interfaces;

namespace RvSfDownloadCore.Services
{
    internal class LoadingBalancerService
    {
        // Логирование
        private readonly ILogger _logger;
        // Рабочие сервисы
        List<IDownloadService> services = new List<IDownloadService>();

        public LoadingBalancerService(ILogger logger,
            EdoDownloadService edoDownloadService,
            PrintFormDownloadService printFormDownloadService,
            ActDownloadService actsDownloadService,
            DiapazonDownloadService diapazonDownloadService)
        {
            _logger = logger;

            services.Add(diapazonDownloadService);
            services.Add(actsDownloadService);
            services.Add(printFormDownloadService);
            services.Add(edoDownloadService);
        }

        /// <summary> Старт сервисов скачивания документов
        /// </summary>
        internal void DownloadDocuments()
        {

            while (DownloadAllDocuments() > 0)
                ;
        }

        private int DownloadAllDocuments()
        {
            int downloadedTotal = 0;
            foreach (var service in services)
            {
                downloadedTotal += DownloadOneService(service);
            }

            return downloadedTotal;
        }

        private int DownloadOneService(IDownloadService downloadService)
        {
            _logger.LogTrace($"Start загрузки {downloadService.Name}.");
            int downloadedActs = downloadService.Download();
            _logger.LogTrace($"Finish загрузки {downloadService.Name}, загружено {downloadedActs} документов.");

            return downloadedActs;
        }
    }
}
