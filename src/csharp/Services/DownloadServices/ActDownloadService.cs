using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Repository;
using RvSfDownloadCore.Services.Interfaces;
using RvSfDownloadCore.Source;
using RvSfDownloadCore.Util;
using System.Xml.Linq;

namespace RvSfDownloadCore.Services.DownloadServices
{
    /// <summary>
    /// Загружает документы с портала
    /// </summary>
    class ActDownloadService : IDownloadService
    {
        public string Name { get; } = "Акты";
        private readonly int _maxCount;
        private readonly ILogger _logger;

        private readonly DbRepairRepository _dbRepairRepository;
        private readonly RvDownloadRepository _rvDownloadRepository;
        private readonly int _downloadActPerIteration;


        public ActDownloadService(DbRepairRepository dbRepairRepository,
            RvDownloadRepository rvDownloadRepository,
            ILogger logger,
            IConfiguration config)
        {
            _dbRepairRepository = dbRepairRepository;
            _rvDownloadRepository = rvDownloadRepository;
            _logger = logger;
            _maxCount = Helpers.ParamsGetter.ReadNonEmptyIntConfigParam(config, logger, "DownloadPriority:Acts");
            _downloadActPerIteration = Helpers.ParamsGetter.ReadNonEmptyIntConfigParam(config, logger, "HttpSettings:DownloadActPerIteration");
        }


        /// <summary>
        /// Скачивание актов выполненных работ
        /// </summary>
        /// <returns></returns>
        public int Download()
        {

            int acts = 0;

            if (_maxCount == 0)
                return acts;

            foreach (var src in DocumentSource.Sources)
            {
                acts += DoWork(src);
            }

            return acts;
        }

        /// <summary>
        /// Загрузить акты с портала
        /// </summary>
        /// <returns>Количество загруженнх актов</returns>
        public int DoWork(DocumentSource documentSource)
        {
            int actTotalDownloaded = 0;
            
            // Акты грузим порциями, т.к. сервер некорректно обрабатывает, если порция больше определенного порога.
            // При порции 250 актов сервер начинал возвращать частично заполненный XML - ошибку не давал.
            try
            {
                while (actTotalDownloaded < _maxCount)
                {
                    int actDownloadedInPortion = ActDownloadOnePortion(documentSource);
                    if (actDownloadedInPortion == 0)
                        break;

                    actTotalDownloaded += actDownloadedInPortion;
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки DoWork. [{ex.Message}]");
            }

            return actTotalDownloaded;
        }

        private int ActDownloadOnePortion(DocumentSource documentSource)
        {
            int actDownloaded = 0;

            //  1)  Получить список ID актов для скачивания (вызов хр.процедуры)
            IEnumerable<string> acts = _dbRepairRepository.ActsGetNext4Update(documentSource.SystemSource, _downloadActPerIteration);

            if (acts.Any())
            {
                //  2) Скачать с https://service_host_name.net акты (пачка актов в соответствие с полученным списком id
                var xmlBytes = _rvDownloadRepository.DownloadRepairXmlByRepairIds(acts, documentSource);
                //  3) Обработка данных
                var xmlObj = XmlUtils.CreateXmlObjectFromBytes(xmlBytes);

                var vagonsTag = xmlObj.Elements(); // выделяет подчиненные теги ВАГОН или ХРАНЕНИЕ
                actDownloaded = vagonsTag.Count();
                //  4) сохранение данных в БД
                //          если нет ошибок, то сохранение данных в базы dbo.Doc и dbo.DocUpdate через вызов хр.процедуры
                //          иначе, в  dbo.DocUpdate  через вызов хр.процедуры load.VagonSaveError
                SaveVagons2Db(documentSource, vagonsTag);

                //  5) Пометить акты как отсутствующие load.MarkMissingActInDownloadResult
                MarkMissingActInDownloadResult(documentSource, vagonsTag, acts);
            }

            return actDownloaded;
        }

        private void MarkMissingActInDownloadResult(DocumentSource documentSource, IEnumerable<XElement> vagonsTag, IEnumerable<string> acts)
        {
            var saved = vagonsTag.Select(x => x.Attribute("Код").Value).ToDictionary(x => x);
            foreach (var vagonTagId in acts)
            {
                if (!saved.ContainsKey(vagonTagId))
                    _dbRepairRepository.MarkMissingActInDownloadResult(vagonTagId, documentSource.SystemSource);
            }
        }

        private void SaveVagons2Db(DocumentSource documentSource, IEnumerable<XElement> vagonsTag)
        {
            foreach (var vagonXml in vagonsTag)
            {
                var vagonXmlBytes = XmlUtils.SaveXmlObjectToBytes(vagonXml, 4096);
                VagonSave2DbOne(documentSource, vagonXml, vagonXmlBytes);   // Сохранить в БД полученный XML
            }
        }

        /// <summary>
        /// Если нет ошибок, то сохранение данных в базы dbo.Doc и dbo.DocUpdate через вызов хр.процедуры
        /// иначе, в  dbo.DocUpdate  через вызов хр.процедуры load.VagonSaveError
        /// </summary>
        /// <param name="documentSource"></param>
        /// <param name="vagonXml"></param>
        /// <param name="vagonXmlBytes"></param>
        private void VagonSave2DbOne(DocumentSource documentSource, XElement vagonXml, byte[] vagonXmlBytes)
        {
            string DocRvRepairId = vagonXml.Attribute("Код").Value;
            try
            {
                _dbRepairRepository.VagonSave(documentSource.SystemSource, DocRvRepairId, vagonXmlBytes);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка обработки сохланения Акта в dbo.DocUpdate  через вызов хр.процедуры load.VagonSaveError");
                _dbRepairRepository.VagonSaveError(Convert.ToInt32(documentSource.SystemSource), DocRvRepairId, e.Message, vagonXmlBytes);
            }
        }
    }

}
