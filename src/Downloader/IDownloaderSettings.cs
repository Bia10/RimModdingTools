using System.Net.Http;

namespace RimModdingTools.Downloader
{
    public interface IDownloaderSettings
    {
        HttpClient HttpClient { get; set; }
        string Author { get; set; }
        string Repository { get; set; }
        bool IncludePreRelease { get; set; }
        string DownloadDirPath { get; set; }
    }
}