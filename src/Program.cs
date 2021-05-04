using HtmlAgilityPack;
using RimModdingTools.Downloader;
using RimModdingTools.XmlDocuments;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
//using SixLabors.ImageSharp.Processing;
using AnsiConsoleExtensions = RimModdingTools.Utils.AnsiConsoleExtensions;

namespace RimModdingTools
{
    static class Program
    {
        private static List<ModFolder> LoadedModFolders = new();
        private static List<ModFolder> LoadedDataFolders = new();
        private static ModsConfigData LoadedModConfig = new();

        public const string pathToRim = @"C:\Games\RimWorld";
        public const string pathToMods = pathToRim + @"\Mods\";

        public static void Main()
        {
            LoadDataDirs(pathToRim + @"\Data\");
            LoadModDirs(pathToMods);
            LoadModConfig();

            AnsiConsoleExtensions.Log($"DataFolders loaded: {LoadedDataFolders.Count}  ModFolders loaded: {LoadedModFolders.Count} ModConfig active mods: {LoadedModConfig.ActiveMods.Count}", "warn");

            const string url = "github.com/pardeike/HarmonyRimWorld/releases/latest";
            DownloadModFromGithub(url, pathToMods);

            //var idsToDl = new uint[] {1854607105, 2420141361};
            //DownloadModsFromSteam(idsToDl);
            //ParseModFolders();
            //RenameWorkshopIdToModName();
            //CheckForIncompatibleMods();
            //CheckForOutdatedMods();
            //CheckIfMissingDependency();
            //CheckActiveModsAgainstLocalMods();
        }

        public static void ParseModFolders()
        {
            foreach (var modFolders in LoadedModFolders)
            {
                var modFiles = Directory.GetFiles(modFolders.About.FullName);
                foreach (var file in modFiles)
                {
                    var curFile = new FileInfo(file);
                    switch (curFile.Name)
                    {
                        case "About.xml":
                            var xml = File.ReadAllText(curFile.FullName);
                            var modInfo = ModMetaData.GetFromXml(xml);
                            modInfo.PrintHeader(false);
                            break;
                        case "Preview.png" or "preview.png":
                            //var image = new CanvasImage(curFile.FullName);
                            //image.MaxWidth(64);
                            //AnsiConsole.Render(image);
                            break;
                        case "PublishedFileId.txt":
                            var workshopId = File.ReadAllText(curFile.FullName);
                            break;
                        case "Version.xml":
                            var version = File.ReadAllText(curFile.FullName);
                            break;
                        case "ModSync.xml":
                            var modSync = File.ReadAllText(curFile.FullName);
                            break;
                        case "LoadFolders.xml":
                            var loadFolders = File.ReadAllText(curFile.FullName);
                            break;
                        case "Manifest.xml":
                            var manifest = File.ReadAllText(curFile.FullName);
                            break;

                        default:
                            //AnsiConsoleExtensions.Log($"curfilename: {curFile.Name}", "info");
                            break;
                    }
                }
            }
        }

        //"https://api.github.com/<GitHubUsername>/<RepoName>/releases/latest"
        public static void DownloadModFromGithub(string githubUri, string outputPath)
        {
            var urlSplit = githubUri.Split("/");

            var httpClient = new HttpClient();
            var author = urlSplit[1];
            var repo = urlSplit[2];

            IDownloaderSettings settings = new DownloaderSettings(httpClient, author, repo, true, outputPath);
            IDownloader downloader = new Downloader.Downloader(settings);

            downloader.DownloadLatestRelease();

            var fullFileName = outputPath + downloader.GetAssetName();
            if (Directory.Exists(fullFileName.Replace(".zip", string.Empty)))
                Directory.Delete(fullFileName.Replace(".zip", string.Empty), true);

            try
            {
                ZipFile.ExtractToDirectory(fullFileName, outputPath);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }

            if (File.Exists(fullFileName))
                File.Delete(fullFileName);

            downloader.DeInit();
            httpClient.Dispose();
        }

        //Todo: What if mod has no release?
        //download repo
        //compile src via devenv.exe
        //create modFolder from compiled src

        public static void DownloadModsFromSteam(IEnumerable<uint> workshopIds)
        {
            var dllPath = @"C:\steamcmd\steamapps\workshop\content\" + ProcLauncher.CmdArguments.RimAppId.Trim() + @"\";

            var downloadArgs = workshopIds.Aggregate("", (current, id) =>
                current + ProcLauncher.CmdArguments.DownloadWorkshopItem + ProcLauncher.CmdArguments.RimAppId + id);
            var cmdArgs = ProcLauncher.CmdArguments.AnonLogin + downloadArgs + ProcLauncher.CmdArguments.Quit;
            var steamCmd = new ProcLauncher(@"C:\steamcmd\steamcmd.exe", cmdArgs); 

            steamCmd.Launch();

            if (ProcLauncher.HasExited())
            {
                foreach (var modDir in new DirectoryInfo(dllPath).GetDirectories())
                    modDir.MoveTo($@"{pathToMods}\{modDir.Name}");
            }
        }

        public static int GetWorkshopIdFromPackageId(string packageId) //TODO: may not be reliable
        {
            const string rimSearchUri = @"https://steamcommunity.com/workshop/browse/?appid=294100&searchtext=";
            var packageIdSearch = packageId.Replace(".", "+");
            var modSearchUri = rimSearchUri + packageIdSearch;
            var result = Util.GetUrlStatus(modSearchUri,
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36");
            if (result != HttpStatusCode.OK)
            {
                AnsiConsoleExtensions.Log($"Failed to obtain search result page, resultCode: {result}", "warn");
                return 0;
            }

            var web = new HtmlWeb();
            var document = web.Load(modSearchUri);

            var packageNameFormat = packageIdSearch.Split("+");

            foreach (var modTitle in document.DocumentNode
                .SelectNodes("//div[contains(@class, 'workshopItemTitle ellipsis')]"))
            {
                var modName = modTitle.InnerHtml;
                var modNameNoWs = modName.Replace(" ", "");

                foreach (var str in packageNameFormat)
                {
                    var likelyName = packageNameFormat[1];

                    if (Util.Contains(modName, likelyName, StringComparison.InvariantCultureIgnoreCase) 
                        || Util.Contains(modNameNoWs, likelyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        AnsiConsoleExtensions.Log($"likelyName: {likelyName} modName: {modName}", "warn");

                        var hrefNode = modTitle.ParentNode.OuterHtml;
                        var workshopId = Util.StringBetweenStrings(hrefNode, "?id=", "&");

                        AnsiConsoleExtensions.Log($"Likely we have found the workshopId: {workshopId} for mod: {modName} ", "success");
                        return int.Parse(workshopId);
                    }
                }
            }

            AnsiConsoleExtensions.Log("Failed to find the mod among results!", "warn");
            return 0;
        }

        public static void CheckActiveModsAgainstLocalMods()
        {
            var activeMods = LoadedModConfig.ActiveMods;
            var localMods = LoadedModFolders.Select(modFolder =>
                modFolder.LoadModMetaData()).Select(metaData =>
                metaData.PackageId).ToList();
            var modsNotFound = activeMods.Where(activeMod =>
                !localMods.Contains(activeMod));

            foreach (var notFoundMod in modsNotFound)
            {
                if (notFoundMod.Equals("ludeon.rimworld") || notFoundMod.Equals("ludeon.rimworld.royalty")) continue;
                AnsiConsoleExtensions.Log($"Modlist references mod not found localy: {notFoundMod}", "warn");
                var response = AnsiConsole.Confirm("[green]Would you like to download missing mods?[/]:");

                if (response)
                {
                    var workshopId = GetWorkshopIdFromPackageId(notFoundMod);
                    //TODO: download via steamCMD
                }
            }
        }

        public static void LoadModConfig()
        {
            const string pathToLocalLow = @"%userprofile%\appdata\locallow";
            const string pathToConfig = @"\Ludeon Studios\RimWorld by Ludeon Studios\Config\";
            const string configName = "ModsConfig.xml";
            var path = Environment.ExpandEnvironmentVariables(pathToLocalLow + pathToConfig + configName);
            var xml = File.ReadAllText(path);

            LoadedModConfig = ModsConfigData.GetFromXml(xml);
        }

        public static IEnumerable<string> GetDependenciesPackageIds()
        {
            var result = new List<string>();
            foreach (var modFolder in LoadedModFolders)
            {
                var metaData = modFolder.LoadModMetaData();
                foreach (var dependency in metaData.ModDependencies)
                {
                    if (result.Contains(dependency.PackageId)) continue;
                    result.Add($"{dependency.PackageId}");
                }
            }
            return result;
        }

        public static void CheckIfMissingDependency()
        {
            var dependenciesIds = GetDependenciesPackageIds();
            var modPackageIds = LoadedModFolders.Select(modFolder => 
                modFolder.LoadModMetaData()).Select(metaData => metaData.PackageId).ToList();
            var dataPackageIds = LoadedDataFolders.Select(dataFolder =>
                dataFolder.LoadModMetaData()).Select(metaData => metaData.PackageId).ToList();

            foreach (var dependencyId in dependenciesIds.Where(dependencyId => !modPackageIds.Contains(dependencyId)))
            {
                const string rimCore = "Ludeon.RimWorld";
                const string rimRoyalty = "Ludeon.RimWorld.Royalty";

                if (dependencyId.Equals(rimCore) || dependencyId.Equals(rimRoyalty))
                {
                    if (!dataPackageIds.Contains(rimCore) || !dataPackageIds.Contains(rimRoyalty))
                        AnsiConsoleExtensions.Log($"Core dependency not found: {dependencyId}", "warn");
                    continue;
                }
                AnsiConsoleExtensions.Log($"Mod dependency not found: {dependencyId}", "warn");
            }
        }

        private static void CheckForOutdatedMods()
        {
            foreach (var modFolder in LoadedModFolders.Where(modFolder => modFolder.IsOutdated())) 
                AnsiConsoleExtensions.Log($"Outdated mod: {modFolder.Name}", "info");
        }

        private static void LoadDataDirs(string dirPath)
        {
            var dataDirs = Directory.GetDirectories(dirPath);

            foreach (var dataDir in dataDirs)
            {
                var dirInfo = new DirectoryInfo(dataDir);
                var dataFolder = new ModFolder(dirInfo.Name, dirInfo.FullName);

                foreach (var folder in Directory.GetDirectories(dataDir))
                {
                    var currentDataDir = new DirectoryInfo(folder);
                    switch (currentDataDir.Name)
                    {
                        case "About":
                            dataFolder.About = currentDataDir;
                            break;
                        default:
                            //AnsiConsoleExtensions.Log($"not recognized: {currentDataDir.Name} path:{currentDataDir.ToString()}", "warn");
                            break;
                    }
                }

                LoadedDataFolders.Add(dataFolder);
            }
        }

        private static void LoadModDirs(string dirPath)
        {
            foreach (var modDir in Directory.GetDirectories(dirPath))
            {
                var dirInfo = new DirectoryInfo(modDir);
                var modFolder = new ModFolder(dirInfo.Name, dirInfo.FullName);
                if (!modFolder.IsValid()) 
                    AnsiConsoleExtensions.Log($"Invalid mod: {modFolder.Name}", "info");

                foreach (var folder in Directory.GetDirectories(modDir))
                {
                    var currentModDir = new DirectoryInfo(folder);
                    switch (currentModDir.Name)
                    {
                        case "1.0" or "v1.0":
                            modFolder.VersionOneZero = currentModDir;
                            break;
                        case "1.1" or "v1.1":
                            modFolder.VersionOneOne = currentModDir;
                            break;
                        case "1.2" or "v1.2":
                            modFolder.VersionOneTwo = currentModDir;
                            break;
                        case "About":
                            modFolder.About = currentModDir;
                            break;
                        case "Assemblies":
                            modFolder.Assemblies = currentModDir;
                            break;
                        case "Defs":
                            modFolder.Defs = currentModDir;
                            break;
                        case "Languages":
                            modFolder.Languages = currentModDir;
                            break;
                        case "News":
                            modFolder.News = currentModDir;
                            break;
                        case "Patches":
                            modFolder.Patches = currentModDir;
                            break;
                        case "Sounds":
                            modFolder.Sounds = currentModDir;
                            break;
                        case "Textures":
                            modFolder.Textures = currentModDir;
                            break;
                        case "Source" or "source" or "src":
                            modFolder.Source = currentModDir;
                            break;
                        case ".git" or ".vs" or "packages":
                            break;

                        default:
                            //AnsiConsoleExtensions.Log($"not recognized: {currentModDir.Name} path:{currentModDir.ToString()}", "warn");
                            break;
                    }
                }

                LoadedModFolders.Add(modFolder);
            }
        }

        private static void RenameWorkshopIdToModName()
        {
            foreach (var modFolder in LoadedModFolders)
            {
                if (!Util.IsDigitOnly(modFolder.Name)) continue;

                var modName = modFolder.LoadModMetaData().Name;

                string[] illegalChars = { "<", ">", ":", "\"", "/", "\\", "|", "?", "*"};
                foreach (var @char in illegalChars)
                    if (modName.Contains(@char))
                        modName = modName.Replace(@char, "");
                
                AnsiConsoleExtensions.Log($"Renaming mod: {modFolder.Name} to {modName}", "info");
                modFolder.Name = modName;
                Util.RenameFolder(modFolder.Path, modName);
            }
        }

        private static void CheckForIncompatibleMods()
        {
            var modNames = new List<string>();
            var modPackages = new List<string>();
            var modIncompatibleLists = new List<Tuple<List<string>, string>>();
            foreach (var modFolder in LoadedModFolders)
            {
                var modMetaData = modFolder.LoadModMetaData();
                var modName = modMetaData.Name;
                var modPackage = modMetaData.PackageId;
                modNames.Add(modName);
                modPackages.Add(modPackage);

                var incompatibleNames = modMetaData.IncompatibleWith;
                if (incompatibleNames.Count > 0)
                {
                    modIncompatibleLists.Add(new Tuple<List<string>, string>(incompatibleNames, modName));
                }
            }

            for (var i = 0; i < modPackages.Count; i++)
            {
                var modPackage = modPackages[i];
                var curModName = modNames[i];

                foreach (var (incompatibleNames, modName) in modIncompatibleLists)
                {
                    foreach (var incompatibleName in incompatibleNames)
                    {
                        if (incompatibleName.Equals(modPackage))
                        {
                            AnsiConsoleExtensions.Log($"Incompatible mods found! Mod1: {modName} with Mod2: {curModName}", "warn");
                        }
                    }
                }
            }
        }
    }
}
