namespace RimModdingTools.Downloader
{
    public interface IDownloader
    {
        bool DownloadLatestRelease();
        void DeInit();
        string GetAssetName();
    }
}