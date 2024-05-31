using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Domain;
using RvSfDownloadCore.Repository;
using RvSfDownloadCore.Services.Interfaces;
using RvSfDownloadCore.Source;
// ReSharper disable PossibleMultipleEnumeration

namespace RvSfDownloadCore.Services.DownloadServices
{
    /// <summary>
    /// Загружает документы c ЭЦП с портала: счета-фактуры в PDF, ZIP
    /// </summary>
    class EdoDownloadService : IDownloadService
    {
        public string Name { get; } = "Документы c ЭЦП";
        private readonly int _maxCount;
        public DbRepairRepository _dbRepairRepository { get; }
        public RvDownloadRepository _rvDownloadRepository { get; }

        private readonly ILogger _logger;
        private readonly string _edoSfLogPath;          // Папка, куда логгировать ошибки разархивирования СФ
        private readonly bool _edoSfLoggerEnabled;      // Включено ли логгирование в папку ошибок разархивирования СФ
        public EdoDownloadService(DbRepairRepository dbRepairRepository, RvDownloadRepository rvDownloadRepository, ILogger logger, IConfiguration config)
        {
            _dbRepairRepository = dbRepairRepository;
            _rvDownloadRepository = rvDownloadRepository;
            _logger = logger;
            _maxCount = Helpers.ParamsGetter.ReadNonEmptyIntConfigParam(config, logger, "DownloadPriority:Edo");

            _edoSfLogPath = config["LogSfErrorFiles:path"] ?? string.Empty;
            _edoSfLoggerEnabled = Helpers.ParamsGetter.ReadBoolConfigParamWithDefault(config, "LogSfErrorFiles:loggerEnabled", false);
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
        public int DoWork(int maxDownload = 100)
        {
            const int maxDownloadPortion = 20;
            int downloadedDocs = 0;
            try
            {
                while (downloadedDocs < maxDownload)
                {
                    var taskList = _dbRepairRepository.GetNextEdo4Download(Math.Min(maxDownload - downloadedDocs, maxDownloadPortion));
                    if (!taskList.Any())
                        return downloadedDocs;

                    foreach (var edoDescriptor in taskList)
                    {
                        TryDownloadAndSaveEdoAttach(edoDescriptor);
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

        private void TryDownloadAndSaveEdoAttach(EdoDescriptor sfFile)
        {
            EdoUrlParam? edoUrlParam = null;
            try
            {
                _logger.LogTrace($"Загрузка СФ. ИдРемонта={sfFile.DocRepairId}, ИдСФ={sfFile.DocRvSfId}, Источник={sfFile.SourceId}: Начало");

                var documentSource = DocumentSource.GetDocumentSourceFactory(sfFile.SourceId);

                edoUrlParam = _rvDownloadRepository.DownloadEdoSignZip(sfFile, documentSource);

                sfFile.TryExtractEdoXmlFromZipArchive();


                if (sfFile.AttachTypeId == 2)       // Источник данных: 1-ТОР ЦВ или 2-АСУ ВРК
                {
                    // По хорошему не надо скачивать ее здесь, а надо ставить общую задачу на скачивание аттача через общий механизм
                    // И готовые комплекты документов для экспорта в EDO отслеживать через условие:
                    // для DocId скачаны все EdoDoc & AttachDoc
                    _rvDownloadRepository.DownloadEdoSfPrintFormPdf(sfFile, documentSource);
                }

                //throw new Exception("Искусственная ошибка загрузки файла EDO");

                _dbRepairRepository.OpenConnection();

                if (sfFile.AttachTypeId == 2)
                    _dbRepairRepository.AttachEdit(sfFile.DocId, 3, "sf.pdf", sfFile.EdoPdfFileBody);    // Записать в БД загруженный Sf PDF

                // Тут бы правильно организовать транзакцию
                SaveEdoFiles(sfFile);
                _dbRepairRepository.EdoArchiveSaveFinish(sfFile);

                _dbRepairRepository.CloseConnection();

                _logger.LogTrace($"Загрузка СФ. ИдРемонта={sfFile.DocRepairId}, ИдСФ={sfFile.DocRvSfId}: Конец");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _dbRepairRepository.EdoSaveError(sfFile, ex.Message);
                EdoSaveFileError(sfFile, ex.Message, edoUrlParam!);     // Сохраняем в файл ошибки СФ (когда файл - некорректный архив)
            }
        }


        public void EdoSaveFileError(EdoDescriptor sfFile, string errorMessage, EdoUrlParam edoUrlParam)
        {
            // Пишем в файл, если стоит соответствующая инструкция в настройках
            if (!_edoSfLoggerEnabled)
                return;

            try
            {
                string fileName = CreateFileName(sfFile);           // Имя файла содержит ИД ЭДО и время ошибки

                SaveServerResponseToFile(sfFile, fileName);         // Сохраняем урл запроса
                SaveEdoUrlParamToFile(edoUrlParam, fileName);       // Сохраняем ответ сервера
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static string CreateFileName(EdoDescriptor sfFile)
        {
            string dateStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            return $"{dateStamp}_{sfFile.DocId}_{sfFile.Doc2EdoId}_{sfFile.DocRvSfId}";
        }

        private void SaveEdoUrlParamToFile(EdoUrlParam edoUrlParam, string fileName)
        {
            string fullFileName = Path.Combine(_edoSfLogPath, $"{fileName}.url.txt");

            string message = $"url=[{edoUrlParam.url}]\nparam=[{edoUrlParam.param}]\n\nall=[{edoUrlParam.url}&{edoUrlParam.param}]";
            try
            {
                File.WriteAllText(fullFileName, message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void SaveServerResponseToFile(EdoDescriptor sfFile, string fileName)
        {
            string fullFileName = Path.Combine(_edoSfLogPath, $"{fileName}.body.zip");
            try
            {
                File.WriteAllBytes(fullFileName, sfFile.EdoZipFileBody);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void SaveEdoFiles(EdoDescriptor sfFile)
        {
            // Уже существующие в БД файлы
            var archiveFiles = _dbRepairRepository.GetArchiveFiles(sfFile.Doc2EdoId);

            // Проходим по новому архиву и ищем в уже существующих в БД файлах
            foreach (var zipFile in sfFile.ZipFilesList)
            {
                var file = archiveFiles.FirstOrDefault(x => x.AttachFileName == zipFile.Key);
                if (file != null)
                {
                    // Такой файл есть - надо обновить
                    _dbRepairRepository.EdoArchiveFileEdit(file.AttachId, zipFile.Value);
                    archiveFiles.Remove(file);
                }
                else
                {
                    // Файла нет - надо добавить
                    _dbRepairRepository.EdoArchiveFileAdd(sfFile.Doc2EdoId, zipFile.Key, zipFile.Value);
                }
            }

            // Мы удалили из archiveFiles все найденное.
            // Оставшееся - это то, что надо удалить из БД
            foreach (var archiveFileDto in archiveFiles)
            {
                _dbRepairRepository.EdoArchiveFileRemove(sfFile.Doc2EdoId, archiveFileDto.AttachId);
            }
        }
    }
}
