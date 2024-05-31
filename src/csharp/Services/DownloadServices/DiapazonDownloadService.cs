using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Repository;
using RvSfDownloadCore.Services.Interfaces;
using RvSfDownloadCore.Source;

namespace RvSfDownloadCore.Services.DownloadServices
{
    /// <summary>
    /// Выгрузка диапазонов, формирование задания на выгрузку данных
    /// </summary>
    class DiapazonDownloadService : IDownloadService
    {
        public string Name { get; } = "Диапазоны";
        private readonly int _maxCount;

        private readonly DbRepairRepository _dbRepairRepository;
        private readonly RvDownloadRepository _rvDownloadRepository;

        private readonly ILogger _logger;
        public DiapazonDownloadService(DbRepairRepository dbRepairRepository, RvDownloadRepository rvDownloadRepository, ILogger logger, IConfiguration config)
        {
            _dbRepairRepository = dbRepairRepository;
            _rvDownloadRepository = rvDownloadRepository;
            _logger = logger;
            _maxCount = Helpers.ParamsGetter.ReadNonEmptyIntConfigParam(config, logger, "DownloadPriority:Diapazons");
        }

        public int Download()
        {
            int downloadedCount = 0;
            if (_maxCount == 0)
                return 0;

            foreach (var source in DocumentSource.Sources)
            {
                downloadedCount += DoWork(source) ? 1 : 0;
            }

            return downloadedCount;
        }

        /// <summary>
        /// Выгрузить очередной диапазон с портала и создать задания на выгрузку отдельных СФ
        /// </summary>
        /// <returns>true - если выгрузили диапазон, иначе false</returns>
        public bool DoWork(DocumentSource documentSource)
        {
            try
            {
                // 1) получить диапазон
                var diapason = _dbRepairRepository.GetNextDiapason4Download(documentSource.SystemSource);
                if (diapason == null)
                    return false;

                // 2) скачать документы о ремонтах для полученного диапазона
                _rvDownloadRepository.DownloadChangedRepairsByDiapason(diapason, documentSource);

                // 3) преобразование данных
                diapason.DiapXml = diapason.SourceXml;

                // 4) сорхранение скаченных документов в БД
                _dbRepairRepository.DiapasonSave(diapason);          // Загрузить файл в БД

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки DoWork. [{ex.Message}]");
            }

            return false;
        }

    }
}
