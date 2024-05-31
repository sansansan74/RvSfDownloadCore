namespace RvSfDownloadCore.Domain
{
    public class PrintFormDescriptor
    {
        public Int32 DocId;         // Ид документа в базе
        public string DocRepairId;  // Ид ремонта на портале
        public Int32 SourceId;      // Источник данных: 1-ТОР ЦВ или 2-АСУ ВРК
        public Int32 AttachTypeId;        // ид загружаемого документа

        public string? PrintFormFileName;   // Имя zip-файла. Сейчас сильно закодировано
        public byte[]? PrintFormFileBody;  // Тело файла ZIP с подписями и сообщениями из обмена ЭДО
        public string DownloadUrl;

        public PrintFormDescriptor(Int32 docId, Int32 attachTypeId, string docRepairId, Int32 sourceId, string downloadUrl)
        {
            DocId = docId;
            AttachTypeId = attachTypeId;
            DocRepairId = docRepairId;
            SourceId = sourceId;
            DownloadUrl = downloadUrl;
        }

        public string GetDownloadUrl() => DownloadUrl.ToLower().Replace("#id#", DocRepairId.ToString());
    }
}
