using Newtonsoft.Json;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utils.Console;

namespace RimModdingTools.Downloader
{
    public class Downloader : IDownloader
    {
        private readonly string _releasesEndpoint;
        private readonly IDownloaderSettings _settings;
        private string _assetName; 

        public Downloader(IDownloaderSettings settings)
        {
            _settings = settings;
            _settings.HttpClient.DefaultRequestHeaders.Add("User-Agent", _settings.Repository);
            _releasesEndpoint = "https://api.github.com/repos/" + _settings.Author + "/" + _settings.Repository + "/releases";
        }

        public void DeInit()
        {
            _settings.HttpClient.DefaultRequestHeaders.Remove("User-Agent");
        }

        public string GetAssetName()
        {
            return !string.IsNullOrEmpty(_assetName) ? _assetName : string.Empty;
        }

        private async Task<Dictionary<string, SemVersion>> GetReleasesAsync()
        {
            var pageNumber = "1";
            var releases = new Dictionary<string, SemVersion>();

            while (pageNumber != null)
            {
                var response = await _settings.HttpClient.GetAsync(new Uri(_releasesEndpoint + "?page=" + pageNumber));
                var contentJson = await response.Content.ReadAsStringAsync();
                VerifyGitHubApiResponse(response.StatusCode, contentJson);
                var releasesJson = JsonConvert.DeserializeObject<dynamic>(contentJson);

                if (releasesJson != null)
                    foreach (var releaseJson in releasesJson)
                    {
                        bool preRelease = releaseJson["prerelease"];
                        if (!_settings.IncludePreRelease && preRelease) continue;
                        var releaseId = releaseJson["id"].ToString();

                        try
                        {
                            string tagName = releaseJson["tag_name"].ToString();
                            var version = CleanVersion(tagName);
                            var semVersion = SemVersion.Parse(version);

                            releases.Add(releaseId, semVersion);
                        }
                        catch (Exception ex)
                        {
                            Extensions.Log($"exception ex: {ex}" , "warn");
                        }
                    }

                pageNumber = GetNextPageNumber(response.Headers);
            }

            return releases;
        }

        public bool DownloadLatestRelease()
        {
            var latestReleaseId = GetLatestRelease().Key;
            var assetUrls = GetAssetUrlsAsync(latestReleaseId).Result;

            while (!Directory.Exists(_settings.DownloadDirPath))
            {
                Directory.CreateDirectory(_settings.DownloadDirPath);
                Thread.Sleep(1);
            }

            foreach (var assetUrl in assetUrls) 
                GetAssetsAsync(assetUrl);

            return true;
        }

        private async Task<List<string>> GetAssetUrlsAsync(string releaseId)
        {
            var assets = new List<string>();
            var assetsEndpoint = _releasesEndpoint + "/" + releaseId + "/assets";
            var response = await _settings.HttpClient.GetAsync(new Uri(assetsEndpoint));
            var contentJson = await response.Content.ReadAsStringAsync();
            VerifyGitHubApiResponse(response.StatusCode, contentJson);
            var assetsJson = JsonConvert.DeserializeObject<dynamic>(contentJson);

            if (assetsJson != null)
                foreach (var assetJson in assetsJson)
                    assets.Add(assetJson["browser_download_url"].ToString());

            return assets;
        }

        private void GetAssetsAsync(string assetUrl)
        {
            try
            {
                _assetName = Path.GetFileName(assetUrl);
                var path = Path.Combine(_settings.DownloadDirPath, _assetName);
                using var client = new WebClient();
                Extensions.Log($"Downloading: {assetUrl} toPath: {path}", "info");
                client.DownloadFile(new Uri(assetUrl), path);
            }
            catch (Exception ex)
            {
                Extensions.Log($"Failed downloading: {assetUrl} reason: {ex.Message}", "warn");
                throw new Exception("Assets download failed.");
            }
        }

        private KeyValuePair<string, SemVersion> GetLatestRelease()
        {
            var releases = GetReleasesAsync().Result;
            var latestRelease = releases.First();

            foreach (var release in releases.Where(release =>
                SemVersion.Compare(release.Value, latestRelease.Value) > 0))
                latestRelease = release;

            return latestRelease;
        }

        private static string CleanVersion(string version)
        {
            var count = version.Count(@char => @char == '.');
            var cleanedVersion = version.StartsWith("v") ? version[1..] : version;

            switch (count)
            {
                case 2:
                    var buildDelimiterIndex = cleanedVersion.LastIndexOf("-", StringComparison.Ordinal);

                    cleanedVersion = buildDelimiterIndex > 0
                        ? cleanedVersion[..buildDelimiterIndex]
                        : cleanedVersion;
                    break;
                case 3:
                    var splitVersion = cleanedVersion.Split(".");

                    var major = splitVersion[0];
                    var minor = splitVersion[1];
                    var patch = splitVersion[2];

                    cleanedVersion = major + "." + minor + "." + patch;
                    break;

                default:
                    Extensions.Log($"Version unrecognized: {version} count: {count}", "warn");
                    break;
            }

            return cleanedVersion;
        }

        private static string GetNextPageNumber(HttpHeaders headers)
        {
            string linkHeader;
            try
            {
                linkHeader = headers.GetValues("Link").FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(linkHeader)) return null;
            var links = linkHeader.Split(',');

            return !links.Any()
                ? null 
                : ( from link in links
                    where link.Contains(@"rel=""next""")
                    select Regex.Match(link, "(?<=page=)(.*)(?=>;)").Value).FirstOrDefault();
        }

        private static void VerifyGitHubApiResponse(HttpStatusCode statusCode, string content)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Forbidden when content.Contains("API rate limit exceeded"):
                    throw new Exception("GitHub API rate limit exceeded.");
                case HttpStatusCode.NotFound when content.Contains("Not Found"):
                    throw new Exception("GitHub Repo not found.");
                case HttpStatusCode.Continue:
                    break;
                default:
                {
                    if (statusCode != HttpStatusCode.OK) 
                        throw new Exception("GitHub API call failed.");
                    break;
                }
            }
        }
    }
}