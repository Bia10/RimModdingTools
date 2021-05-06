namespace RimModdingTools.Downloader
{
    public interface IDownloader
    {
        bool DownloadLatestRelease();
        bool DownloadLatestTag();
        bool CheckIfReleasesExist();
        bool CheckIfTagsExist();
        void DeInit();
        string GetAssetName();
    }
}