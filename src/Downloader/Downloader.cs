using Newtonsoft.Json;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils.Console;
using Utils.String;

namespace RimModdingTools.Downloader
{
    //TODO: use tags when no rls
    public class Downloader : IDownloader
    {
        private readonly string _releasesEndpoint;
        private readonly string _tagEndpoint;
        private readonly IDownloaderSettings _settings;
        private string _assetName; 

        public Downloader(IDownloaderSettings settings)
        {
            _settings = settings;
            _settings.HttpClient.DefaultRequestHeaders.Add("User-Agent", _settings.Repository);
            _releasesEndpoint = "https://api.github.com/repos/" + _settings.Author + "/" + _settings.Repository + "/releases";
            _tagEndpoint = "https://api.github.com/repos/" + _settings.Author + "/" + _settings.Repository + "/tags";
        }

        public void DeInit()
        {
            _settings.HttpClient.DefaultRequestHeaders.Remove("User-Agent");
        }

        public string GetAssetName()
        {
            return !string.IsNullOrEmpty(_assetName) ? _assetName : string.Empty;
        }

        private async Task<Dictionary<string, SemVersion>> GetTagsAsync()
        {
            var pageNumber = "1";
            var tags = new Dictionary<string, SemVersion>();

            while (pageNumber != null)
            {
                var response = await _settings.HttpClient.GetAsync(new Uri(_tagEndpoint + "?page=" + pageNumber));
                var contentJson = await response.Content.ReadAsStringAsync();
                VerifyGitHubApiResponse(response.StatusCode, contentJson);
                var releasesJson = JsonConvert.DeserializeObject<dynamic>(contentJson);
                if (contentJson.Equals("[]"))
                {
                    Extensions.Log("There are no tags!", "warn");
                    return null;
                }

                if (releasesJson != null)
                    foreach (var releaseJson in releasesJson)
                    {
                        try
                        {
                            string tagName = releaseJson["name"].ToString();
                            var version = CleanVersion(tagName);
                            if (string.IsNullOrEmpty(version)) continue;
                            var semVersion = SemVersion.Parse(version);

                            tags.Add(version, semVersion);
                        }
                        catch (Exception ex)
                        {
                            Extensions.Log($"exception ex: {ex}", "warn", true);
                        }
                    }

                pageNumber = GetNextPageNumber(response.Headers);
            }

            return tags;
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
                if (contentJson.Equals("[]"))
                {
                    Extensions.Log("There are no releases!", "warn");
                    return null;
                }

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

        public bool CheckIfReleasesExist()
        {
            var response =  _settings.HttpClient.GetAsync(new Uri(_releasesEndpoint + "?page=" + 1)).Result;
            var contentJson =  response.Content.ReadAsStringAsync().Result;
            VerifyGitHubApiResponse(response.StatusCode, contentJson);

            if (!contentJson.Equals("[]")) return true;

            Extensions.Log("There are no releases!", "warn");
            return false;
        }

        public bool CheckIfTagsExist()
        {
            var response = _settings.HttpClient.GetAsync(new Uri(_tagEndpoint + "?page=" + 1)).Result;
            var contentJson = response.Content.ReadAsStringAsync().Result;
            VerifyGitHubApiResponse(response.StatusCode, contentJson);

            if (!contentJson.Equals("[]")) return true;

            Extensions.Log("There are no tags!", "warn");
            return false;
        }

        public bool DownloadLatestRelease()
        {
            var latestReleaseId = GetLatestRelease().Key;
            var assetUrls = GetAssetUrlsAsync(false, latestReleaseId).Result;

            while (!Directory.Exists(_settings.DownloadDirPath))
                Directory.CreateDirectory(_settings.DownloadDirPath);

            foreach (var assetUrl in assetUrls) 
                GetAssetsAsync(assetUrl);

            return true;
        }

        public bool DownloadLatestTag()
        {
            var latestTagId = GetLatestTag().Key;
            var assetUrls = GetAssetUrlsAsync(true, latestTagId).Result;

            while (!Directory.Exists(_settings.DownloadDirPath))
                Directory.CreateDirectory(_settings.DownloadDirPath);

            foreach (var assetUrl in assetUrls)
                GetAssetsAsync(assetUrl);

            return true;
        }

        private async Task<List<string>> GetAssetUrlsAsync(bool fromTags, string releaseId)
        {
            var assets = new List<string>();
            string assetsEndpoint;
            HttpResponseMessage response;
            string contentJson;

            if (fromTags)
            {
                assetsEndpoint = _tagEndpoint;
                response = await _settings.HttpClient.GetAsync(new Uri(assetsEndpoint));
                contentJson = await response.Content.ReadAsStringAsync();
                if (contentJson.Equals("[]")) //no tags..
                {
                    Extensions.Log($"Failed to find tag assets for rlsId: {releaseId}", "warn");
                    return null;
                }

                VerifyGitHubApiResponse(response.StatusCode, contentJson);
                var tagsJson = JsonConvert.DeserializeObject<dynamic>(contentJson);
                if (tagsJson != null)
                {
                    foreach (var tagJson in tagsJson)
                        if (tagJson["name"].ToString().Equals(releaseId))
                            assets.Add(tagJson["zipball_url"].ToString());

                    return assets;
                }
            }

            assetsEndpoint = _releasesEndpoint + "/" + releaseId + "/assets";
            response = await _settings.HttpClient.GetAsync(new Uri(assetsEndpoint));
            contentJson = await response.Content.ReadAsStringAsync();
            if (contentJson.Equals("[]")) //no release
            {
                Extensions.Log($"Failed to find release assets for rlsId: {releaseId}", "warn");
                return null;
            }
            VerifyGitHubApiResponse(response.StatusCode, contentJson);
            var assetsJson = JsonConvert.DeserializeObject<dynamic>(contentJson);
            if (assetsJson != null)
            {
                foreach (var assetJson in assetsJson)
                    assets.Add(assetJson["browser_download_url"].ToString());

                return assets;
            }

            Extensions.Log($"Failed to find any assets for rlsId: {releaseId}", "warn");
            return null;
        }

        private void GetAssetsAsync(string assetUrl)
        {
            _assetName = Path.GetFileName(assetUrl);

            //https://api.github.com/repos/OrionFive/Hospitality/zipball/refs/tags/B19
            if (assetUrl.Contains("zipball"))
            {
                assetUrl = assetUrl.Replace("api.", "codeload.");
                assetUrl = assetUrl.Replace("/repos", string.Empty);
                assetUrl = assetUrl.Replace("zipball", "legacy.zip");
                //https://codeload.github.com/OrionFive/Hospitality/legacy.zip/refs/tags/B19

                var urlSplit = assetUrl.Replace("https://codeload.github.com/", string.Empty).Split("/");
                var modName = urlSplit[1];
                _assetName = modName + "-" + _assetName + ".zip";
            }

            try
            {
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

        private KeyValuePair<string, SemVersion> GetLatestTag() //Todo: fix
        {
            var tag = GetTagsAsync().Result;
            var latestTag = tag.First();

            foreach (var release in tag.Where(version =>
                SemVersion.Compare(version.Value, latestTag.Value) > 0))
                latestTag = release;

            return latestTag;
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
            var cleanedVersion = version.StartsWith("v") ? version[1..] : version;
            var splitVersion = cleanedVersion.Split(".");
            if (splitVersion.Length == 0 || splitVersion[0].Equals(cleanedVersion)) return string.Empty;

            for (var i = 0; i < splitVersion.Length; i++)
                if (!splitVersion[i].IsDigitOnly())
                    splitVersion[i] = splitVersion[i].GetDigitsOnly();

            var major = splitVersion[0];
            var minor = splitVersion[1];
            var patch = !string.IsNullOrEmpty(splitVersion[2])?splitVersion[2]:string.Empty;

            cleanedVersion = major  + "." + minor + "." + patch;
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