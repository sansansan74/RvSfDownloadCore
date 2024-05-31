namespace RvSfDownloadCore.Source
{
    public class DocumentSource
    {
        public static readonly DocumentSource[] Sources =
        {
            new DocumentSourceTOR(),
            new DocumentSourceVRK(),
            new DocumentSourceKeep(),
            new DocumentSourceStore(),
            new DocumentSourceRepairDetail()
        };

        public DocumentSource(SystemSourcesEnum systemSource, string changedDocumentListsUrl
            , string actByIdListUrl, string downloadEdoCopyParam, string invoiceUrl)
        {
            SystemSource = systemSource;
            ChangedDocumentsListUrl = changedDocumentListsUrl;
            ActByIdListUrl = actByIdListUrl;
            DownloadEdoCopyParam = downloadEdoCopyParam;
            InvoiceUrl = invoiceUrl;
        }

        internal SystemSourcesEnum SystemSource { get; }

        internal string ChangedDocumentsListUrl { get; }

        internal string ActByIdListUrl { get; }

        // описание параметров здесь: services_page.php
        internal string DownloadEdoCopyParam { get; }

        internal string InvoiceUrl { get; }
        internal virtual string ReplaceDateFormatInUrl(string url) => url;

        public static DocumentSource GetDocumentSourceFactory(int SourceId)
        {
            switch ((SystemSourcesEnum)SourceId)
            {
                case SystemSourcesEnum.TOR:
                    return new DocumentSourceTOR();
                case SystemSourcesEnum.VRK:
                    return new DocumentSourceVRK();
                case SystemSourcesEnum.KEEP:
                    return new DocumentSourceKeep();
                case SystemSourcesEnum.STORE:
                    return new DocumentSourceStore();
                case SystemSourcesEnum.REPAIR_DETAIL:
                    return new DocumentSourceRepairDetail();
                default:
                    throw new ArgumentException($"Некорректный и сточник SourceId={SourceId} ");
            }
        }
    }

    /// <summary>
    /// Текущий ремонт
    /// </summary>
    internal class DocumentSourceTOR : DocumentSource
    {
        internal DocumentSourceTOR() : base(
            SystemSourcesEnum.TOR,
            "https://service_host_name.net/export_tr.php?onlyvagondata",
            "https://service_host_name.net/export_tr.php",
            "t",
            "https://service_host_name.net/the_invoice_tr.php"
            )
        {
        }
    }

    internal class DocumentSourceVRK : DocumentSource
    {
        internal DocumentSourceVRK() : base(
            SystemSourcesEnum.VRK,
            "https://service_host_name.net/export_new.php?onlyvagondata",
            "https://service_host_name.net/export_new.php",
            "v",
            "https://service_host_name.net/the_invoice.php"
        )
        {
        }

    }


    internal class DocumentSourceKeep : DocumentSource
    {
        internal DocumentSourceKeep() : base(
            SystemSourcesEnum.KEEP,
            "https://service_host_name.net/export_keep.php?onlyvagondata",
            "https://service_host_name.net/export_keep.php",
            "k",
            "https://service_host_name.net/the_invoice_keep.php"
        )
        {
        }
    }

    internal class DocumentSourceStore : DocumentSource
    {
        internal DocumentSourceStore() : base(
            SystemSourcesEnum.STORE,
            "https://service_host_name.net/export_sklad.php?onlyvagondata",
            "https://service_host_name.net/export_sklad.php",
            "w",
            "https://service_host_name.net/sklad_shet_fp.php"
        )
        {
        }
    }


    internal class DocumentSourceRepairDetail : DocumentSource
    {
        internal DocumentSourceRepairDetail() : base(
            SystemSourcesEnum.REPAIR_DETAIL,
            "https://service_host_name.net/export_repair_details.php?onlyvagondata",
            "https://service_host_name.net/export_repair_details.php",
            "r",
            "https://service_host_name.net/the_invoice_rd.php"
        )
        {
        }
    }

}
