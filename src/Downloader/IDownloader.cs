namespace RimModdingTools.Downloader
{
    public interface IDownloader
    {
        bool DownloadLatestRelease();
        bool CheckIfReleasesExist();
        bool CheckIfTagsExist();
        void DeInit();
        string GetAssetName();
    }
}