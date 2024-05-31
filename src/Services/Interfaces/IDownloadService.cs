namespace RvSfDownloadCore.Services.Interfaces
{
    public interface IDownloadService
    {
        public string Name { get; }
        public int Download();
    }
}
