using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Domain;
using RvSfDownloadCore.Repository;
using RvSfDownloadCore.Services.Interfaces;
using RvSfDownloadCore.Source;

namespace RvSfDownloadCore.Services.DownloadServices
{
    /// <summary>
    /// Загружает печатные формы с портала в PDF,
    /// </summary>
    class PrintFormDownloadService : IDownloadService
    {
        public string Name { get; } = "Печатные формы";
        private readonly int _maxCount;
        private readonly ILogger _logger;
        private readonly DbRepairRepository _repairRepository;
        private readonly RvDownloadRepository _rvDownloadRepository;

        public PrintFormDownloadService(DbRepairRepository repairRepository,
                                            RvDownloadRepository rvDownloadRepository,
                                            ILogger logger,
                                            IConfiguration config)
        {
            _repairRepository = repairRepository;
            _rvDownloadRepository = rvDownloadRepository;
            _logger = logger;
            _maxCount = Helpers.ParamsGetter.ReadNonEmptyIntConfigParam(config, logger, "DownloadPriority:PrintForms");
        }

        public int Download()
        {
            if (_maxCount == 0)
                return 0;

            return DoWork(_maxCount);
        }

        /// <summary>
        /// Выгрузить все файлы СФ с портала
        /// </summary>
        public int DoWork(int maxDownload)
        {
            const int maxDownloadPortion = 20;
            int downloadedDocs = 0;
            try
            {
                // Получить следующий файл для загрузки
                while (downloadedDocs < maxDownload)
                {
                    var taskList = _repairRepository.GetNextPrintForm4Download(Math.Min(maxDownload - downloadedDocs, maxDownloadPortion));
                    if (!taskList.Any())
                        return downloadedDocs;

                    foreach (var printForm in taskList)
                    {
                        TryDownloadAndSavePrintForm(printForm);
                    }

                    downloadedDocs += taskList.Count();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки DoWork. [{ex.Message}]");
            }

            return downloadedDocs;
        }

        private void TryDownloadAndSavePrintForm(PrintFormDescriptor printForm)
        {
            try
            {
                _logger.LogTrace($"Загрузка Печатной формы. ИдРемонта={printForm.DocRepairId}, ИдТипаАттача={printForm.AttachTypeId}, Источник={printForm.SourceId}: Начало");
                // ### Получение списка источников документов (на сайте)
                var documentSource = GetDocumentSourceFactory(printForm.SourceId);
                // ### Скачивание документов с портала
                _rvDownloadRepository.DownloadPrintForm(printForm, documentSource); // Получить СФ для загрузки

                // ### Внесение данных в БД
                _repairRepository.SavePrintForm(printForm); // Загрузить файл в БД
                _logger.LogTrace($"Загрузка Печатной формы. ИдРемонта={printForm.DocRepairId}, ИдТипаАттача={printForm.AttachTypeId}, Источник={printForm.SourceId}: Конец");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _repairRepository.PrintFormSaveError(printForm, ex.Message);
            }
        }

        DocumentSource GetDocumentSourceFactory(int SourceId) => DocumentSource.GetDocumentSourceFactory(SourceId);
    }

}
