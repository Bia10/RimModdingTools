using System;
using System.Net.Http;

namespace RimModdingTools.Downloader
{
    public class DownloaderSettings : IDownloaderSettings
    {
        public DownloaderSettings(HttpClient httpClient, string author, string repository,
            bool includePreRelease, string downloadDirPath)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            IncludePreRelease = includePreRelease;
            DownloadDirPath = downloadDirPath;
        }

        public HttpClient HttpClient { get; set; }
        public string Author { get; set; }
        public string Repository { get; set; }
        public bool IncludePreRelease { get; set; }
        public string DownloadDirPath { get; set; }
    }
}