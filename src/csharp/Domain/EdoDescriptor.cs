using System.Text;
using System.IO.Compression;
using FilePair = System.Collections.Generic.KeyValuePair<string, byte[]>;
using RvSfDownloadCore.Util;
using Microsoft.Extensions.Logging;

namespace RvSfDownloadCore.Domain
{
    public class EdoDescriptor
    {
        public string DocRvSfId;        // ид загружаемого документа
        public string DocRepairId;      // Ид ремонта
        public Int32 AttachTypeId;      // Источник данных: 1-ТОР ЦВ или 2-АСУ ВРК

        public Int32 SourceId;          // Источник данных: 1-ТОР ЦВ или 2-АСУ ВРК

        public string? EdoZipFileName;   // Имя zip-файла. Сейчас сильно закодировано
        public byte[]? EdoZipFileBody;   // Тело файла ZIP с подписями и сообщениями из обмена ЭДО

        public byte[]? RepairXmlBody;    // Тело файла XML ЭДО
        public byte[]? EdoPdfFileBody;   // Печатная форма файла PDF
        public Int64 Doc2EdoId;         // Ид архива документов

        public List<FilePair>? ZipFilesList;

        public Int32 DocId { get; }

        private readonly ILogger _logger;

        public EdoDescriptor(Int32 docId, string docRvSfId, string docRepairId, Int32 sourceId, Int32 attachTypeId, Int64 doc2EdoId, ILogger logger)
        {
            DocId = docId;
            DocRvSfId = docRvSfId;
            DocRepairId = docRepairId;
            SourceId = sourceId;
            AttachTypeId = attachTypeId;
            Doc2EdoId = doc2EdoId;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public void SetFileBody(string fileName, byte[] fileBody)
        {
            EdoZipFileName = fileName;
            EdoZipFileBody = fileBody;
        }

        public void TryExtractEdoXmlFromZipArchive()
        {
            try
            {
                ExtractEdoXmlFromZipArchive();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка TryExtractEdoXmlFromZipArchive [{ex.Message}]");
                CreateUnzipErrorException(ex);
            }
        }



        void CreateUnzipErrorException(Exception ex)
        {
            const int maxErrorLen = 1024;
            if ((EdoZipFileBody?.Length ?? 0) > 0)
            {
                string stringZipFileBody = Encoding.UTF8.GetString(EdoZipFileBody!, 0, EdoZipFileBody!.Length);
                int stringZipFileBodyLen = maxErrorLen - ex.Message.Length - 40;   // Какой максимальной длины может быть тело файла
                if (stringZipFileBodyLen > 0)
                {
                    if (stringZipFileBody.Length > stringZipFileBodyLen)
                        stringZipFileBody = stringZipFileBody.Substring(0, stringZipFileBodyLen) + "...";

                    throw new Exception($"{ex.Message}. FileBody=[{stringZipFileBody}]");
                }
            }
            throw ex;
        }

        /// <summary>
        /// Выгружает из zip файла СФ и присваивает его RepairXmlBody
        /// </summary>
        private void ExtractEdoXmlFromZipArchive()
        {
            _logger.LogTrace("Start unzip");
            using (MemoryStream ms = new MemoryStream(EdoZipFileBody))
            {
                using (ZipArchive za = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    ZipFilesList = za.Entries.Select(CreateZipFileContentFile).ToList();
                }
            }
            _logger.LogTrace("Finish unzip");
        }

        FilePair CreateZipFileContentFile(ZipArchiveEntry archiveEntry)
        {
            byte[] fileBody = GetArchiveFileBody(archiveEntry);
            return new FilePair(archiveEntry.FullName, fileBody);
        }

        private static byte[] GetArchiveFileBody(ZipArchiveEntry archiveEntry)
        {
            using (Stream stream = archiveEntry.Open())
            {
                return StreamUtils.ReadToEndBytes(stream);
            }
        }
    }
}
