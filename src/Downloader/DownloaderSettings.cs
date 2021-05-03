using System;
using System.Net.Http;

namespace RimModdingTools.Downloader
{
    public class DownloaderSettings : IDownloaderSettings
    {
        public DownloaderSettings(HttpClient httpClient, string author, string repository,
            bool includePreRelease, string downloadDirPath)
        {
            HTTPClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            IncludePreRelease = includePreRelease;
            DownloadDirPath = downloadDirPath;
        }

        public HttpClient HTTPClient { get; set; }
        public string Author { get; set; }
        public string Repository { get; set; }
        public bool IncludePreRelease { get; set; }
        public string DownloadDirPath { get; set; }

        public IDownloaderSettings Copy()
        {
            return new DownloaderSettings(HTTPClient, string.Copy(Author),
                string.Copy(Repository), IncludePreRelease, string.Copy(DownloadDirPath));
        }
    }
}