using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Domain;
using RvSfDownloadCore.Helpers;
using RvSfDownloadCore.Repository;
using RvSfDownloadCore.Source;

namespace RvSfDownloadCore.Services
{


    public class EdoUrlParam
    {
        public string? url;
        public string? param;
    }

    /// <summary>
    /// Загрузка данных ремонтов вагонов
    /// </summary>
    internal class RvDownloadRepository
    {
        private readonly ILogger _logger;

        private readonly string _taskDownloadParams;
        private readonly string _sfDownloadLogin;
        private readonly string _sfDownloadPassword;
        private readonly string _actDownloadParams;
        private readonly RvHttpRepository _httpRepository;

        public RvDownloadRepository(ILogger logger, IConfiguration config, RvHttpRepository httpRepository) 
        {
            _logger = logger;
            _httpRepository = httpRepository;
            
            _taskDownloadParams = ParamsGetter.ReadNonEmptyConfigParam(config, logger, "HttpSettings:TaskDownloadParams");
            _sfDownloadLogin = ParamsGetter.ReadNonEmptyConfigParam(config, logger, "HttpSettings:login");
            _sfDownloadPassword = ParamsGetter.ReadNonEmptyConfigParam(config, logger, "HttpSettings:password");
            _actDownloadParams = ParamsGetter.ReadNonEmptyConfigParam(config, logger, "HttpSettings:ActDownloadParams");
        }

        /// <summary>
        /// Загружает с портала перечень ремонтов, изменившихся за период 
        /// </summary>
        /// <returns>Загруженный файл</returns>
        public void DownloadChangedRepairsByDiapason(Diapason diapason, DocumentSource documentSource)
        {
            if (diapason == null)   // Передаваемый код загрузки должен быть не пуст
                return;

            // Пример запроса:
            // https://server_name/export_tr.php?onlysfexists&lastchange&login=myLogin&ecp2&pass=myPass&onlyvagondata&dt1=01.10.2018&dt2=01.02.2019
            string paramFormat = _taskDownloadParams;  
            paramFormat = documentSource.ReplaceDateFormatInUrl(paramFormat);
            string param = string.Format(
                paramFormat, 
                _sfDownloadLogin,
                _sfDownloadPassword,
                diapason.DiapStart, 
                diapason.DiapFinish);

            _logger.LogTrace($"Start загрузка перечня ремонтов url={documentSource.ChangedDocumentsListUrl}, param = {param}");
            diapason.SourceXml = _httpRepository.PostResponce(documentSource.ChangedDocumentsListUrl, param);
            _logger.LogTrace("Finish загрузка перечня ремонтов");
        }

        /// <summary> Загрузка xml актов по списку
        /// </summary>
        /// <param name="repairIds"></param>
        /// <param name="documentSource"></param>
        /// <returns></returns>
        public byte[] DownloadRepairXmlByRepairIds(IEnumerable<string> repairIds, DocumentSource documentSource)
        {
            _logger.LogTrace($"Start загрузка xml актов по списку");

            string param = string.Format(
                _actDownloadParams, 
                _sfDownloadLogin, 
                _sfDownloadPassword,
                string.Join(",", repairIds));

            _logger.LogTrace($"Start загрузка xml актов по списку url={documentSource.ActByIdListUrl}, param = {param}");
            var binaryXmlRepairs = _httpRepository.PostResponce(documentSource.ActByIdListUrl, param);
            _logger.LogTrace($"Finish загрузка xml актов по списку");

            return binaryXmlRepairs;
        }

        string GetParamsLoginPassword() => $"login={_sfDownloadLogin}&pass={_sfDownloadPassword}";

        /// <summary>
        /// Загружает с портала документы ЭДО - ZIP-с подписями и визуальную форму с PDF
        /// </summary>
        /// <returns>Загруженный файл</returns>
        public void DownloadPrintForm(PrintFormDescriptor sf, DocumentSource documentSource)
        {
            string url = sf.GetDownloadUrl();
            string param = GetParamsLoginPassword();
            _logger.LogTrace($"Start download PrintForm.  DocRepairId=[{sf.DocRepairId}]");
            sf.PrintFormFileBody = _httpRepository.PostResponce(url, param);
            _logger.LogTrace($"Finish download PrintForm.  DocRepairId=[{sf.DocRepairId}]");
        }



        /// <summary>
        /// Загружает с портала ZIP с подписями ЭДО
        /// </summary>
        /// <param name="sf"></param>
        /// <param name="documentSource"></param>
        public EdoUrlParam DownloadEdoSignZip(EdoDescriptor sf, DocumentSource documentSource)
        {
            // Второй параметр для ТОР ЦВ д.б. =t, для АСУ ВРК д.б.=v 
            string url = $"https://service_host_name.net/download_edo_copy.php?id={sf.DocRvSfId}&{documentSource.DownloadEdoCopyParam}&p7s";
            string param = $"login={_sfDownloadLogin}&pass={_sfDownloadPassword}&fileid={sf.DocRvSfId}";

            // ----------------------------------------  Здесть прикрутить нормальную передачу имени файла

            _logger.LogTrace($"Start download ZIP.  DocRvSfId=[{sf.DocRvSfId}]");
            //CreateResponseWithFileName(req, sf); // Разбирает ответ портала
            var edoFileBytes = _httpRepository.PostResponce(url, param);

            // DecodeFileName(resp.Headers["content-disposition"]), // Получаем имя прикрепланного файла и декодируем его
            sf.SetFileBody("filename.zip",edoFileBytes);
            _logger.LogTrace($"Finish download ZIP.  DocRvSfId=[{sf.DocRvSfId}]");

            return new EdoUrlParam()
            {
                url = url,
                param = param
            };
        }

        /// <summary> 
        /// Скачивает акты с портала. Процедура была нужна, чтобы разово скачать акты по запросу руководства
        /// скачать все акты двух компаний по их ИД
        /// </summary>
        /// <param name="sf"></param>
        public void DownloadAct(EdoDescriptor sf)
        {
            string url = $"https://service_host_name.net/certificate_of_works.php?id={sf.DocRepairId}&print&pdf";
            string param = GetParamsLoginPassword();

            _logger.LogTrace($"Start download act PDF.  DocRvSfId=[{sf.DocRvSfId}]");
            sf.EdoPdfFileBody = _httpRepository.PostResponce(url, param);
            _logger.LogTrace($"Finish download act PDF.  DocRvSfId=[{sf.DocRvSfId}]");
        }

        /// <summary>
        /// Загружает с портала PDF ЭДО
        /// </summary>
        /// <param name="sf"></param>
        /// <param name="documentSource"></param>
        public void DownloadEdoSfPrintFormPdf(EdoDescriptor sf, DocumentSource documentSource)
        {
            // Надо логиниться
            // ТОР ЦВ - the_invoice_tr, АСУ ВРК - the_invoice
            string url = $"{documentSource.InvoiceUrl}?id={sf.DocRepairId}&pdf";
            string param = GetParamsLoginPassword();

            _logger.LogTrace($"Start dowload PDF.  DocRvSfId=[{sf.DocRvSfId}]");
            sf.EdoPdfFileBody = _httpRepository.PostResponce(url, param); 
            _logger.LogTrace($"Finish dowload PDF.  DocRvSfId=[{sf.DocRvSfId}]");
        }


        /// <summary>
        /// Осуществляет декодировку имени загруженного файла (изначально он идет закодированный)
        /// </summary>
        /// <param name="fileName">Закодированное имя файла</param>
        /// <returns>Раскодированное имя файла</returns>
        private string DecodeFileName(string fileName)
        {
            /*   
            if (!String.IsNullOrEmpty(fileName))
            {
                //ContentDisposition contentDisposition = new ContentDisposition(fileName);
                //string filename = contentDisposition.EdoZipFileName;
                //StringDictionary parameters = contentDisposition.Parameters;



                string s = fileName.Substring(fileName.IndexOf("filename=") + 10).Replace("\"", "");
                var p0 = HttpUtility.UrlDecode(fileName, Encoding.GetEncoding("ISO-8859-1"));
                var p1 = HttpUtility.UrlDecode(s, Encoding.GetEncoding(1252));

                var p = HttpUtility.UrlDecode(s, Encoding.UTF8);

                string s0 = recode(fileName, Encoding.GetEncoding("ISO-8859-1"), Encoding.GetEncoding("UTF-8"));
                string s10 = recode(fileName, Encoding.GetEncoding("latin-1"), Encoding.GetEncoding("UTF-8"));
                
                string s1 = recode(fileName, Encoding.GetEncoding("ISO-8859-1"), Encoding.GetEncoding(1251));

                //var u8 = recode(s, "UTF-8");
                //var u16 = recode(s, "UTF-16");

                return s;
            } */
            return "ok.zip";
        }

        //string recode(string text, Encoding encFrom, Encoding encTo)
        //{
        //    //Encoding encFrom = Encoding.GetEncoding("UTF-8");
        //    //Encoding encTo = Encoding.GetEncoding("Windows-1251");

        //    byte[] utf8Bytes = encTo.GetBytes(text);
        //    byte[] win1251Bytes = Encoding.Convert(encFrom, encTo, utf8Bytes);

        //    return encTo.GetString(win1251Bytes);
        //}

    }

}
