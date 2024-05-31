using RvSfDownloadCore.Source;

namespace RvSfDownloadCore.Domain
{
    public class Diapason
    {
        public int DiapId { get; internal set; }
        public DateTime DiapStart { get; internal set; }
        public DateTime DiapFinish { get; internal set; }
        public byte[] SourceXml { get; internal set; }
        public byte[] DiapXml { get; internal set; }
        public SystemSourcesEnum SourceId { get; internal set; }
    }
}
