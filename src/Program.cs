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
using Utils.Console;
using Utils.FileSystem;
using Utils.Net;
using Utils.Process;
using Utils.String;
//using SixLabors.ImageSharp.Processing;

namespace RimModdingTools
{
    static class Program
    {
        private static List<ModFolder> _loadedModFolders = new();
        private static List<ModFolder> _loadedDataFolders = new();
        private static ModsConfigData _loadedModConfig = new();

        private const string PathToRim = @"C:\Games\RimWorld";
        private const string PathToData = PathToRim + @"\Data\";
        private const string PathToMods = PathToRim + @"\Mods\";
        private const string PathToSteamCmd = @"C:\steamcmd\";
        private const string PathToRimThreadidIncompatible = PathToMods + "IncompatibleWithRimThreadid\\";

        public static void Main()
        {
            //LoadDataDirs(PathToData);
            //LoadModDirs(PathToMods);
            //foreach (var modFolder in _loadedModFolders) modFolder.Init();
            //LoadLocalModConfig();
            //Extensions.Log($"DataFolders loaded: {_loadedDataFolders.Count}  ModFolders loaded: {_loadedModFolders.Count} ModConfig active mods: {_loadedModConfig.ActiveMods.Count}", "warn");
            //foreach (var modFolder in _loadedModFolders) 
                //Extensions.Log($"modFolder: {modFolder.ModMetaData.Name} Compatible?: {IsRimThreadedCompatible(modFolder, true)}", "info", true);

            //MoveRimthredidIncompatibleMods(PathToRimThreadidIncompatible);
            //const string url = "github.com/OrionFive/Hospitality";
            //DownloadModFromGithub(url, PathToMods);
            var idsToDl = new uint[] { 1742151109, 1690978457};
            DownloadModsFromSteam(idsToDl);
            //ParseModFolders();
            //RenameWorkshopIdToModName();
            //CheckForIncompatibleMods();
            //CheckForOutdatedMods();
            //CheckIfMissingDependency();
            //CheckActiveModsAgainstLocalMods();
        }

        public static void MoveRimthredidIncompatibleMods(string pathTo)
        {
            if (!Directory.Exists(pathTo)) Directory.CreateDirectory(pathTo);

            foreach (var modFolder in _loadedModFolders.Where(modFolder =>
                !IsRimThreadedCompatible(modFolder, true)))
            {
                if (Directory.Exists(pathTo + modFolder.Name)) Directory.Delete(pathTo + modFolder.Name, true);
                Directory.Move(modFolder.Path, pathTo + modFolder.Name);
            }
        }

        public static void ParseModFolders()
        {
            foreach (var modFolders in _loadedModFolders)
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
                            //Extensions.Log($"curfilename: {curFile.Name}", "info");
                            break;
                    }
                }
            }
        }

        public static List<ModFolder> GetModsNotInRimThreadidModList()
        {
            const string rimThreadedModlistUri = @"https://raw.githubusercontent.com/pastorismylord/Community-Modpack/main/Community-modpack.xml";
            var modList = LoadModConfigFromUri(rimThreadedModlistUri);

             return _loadedModFolders.Where(mod => 
                 !IsPackageIdInModList(mod, modList)).ToList();
        }

        public static List<ModFolder> GetModsInRimThreadidIncompatibleList()
        {
            const string rimThreadedIncompatibleList = @"https://raw.githubusercontent.com/wiki/cseelhoff/RimThreaded/Mod-Compatibility.md";
            var incompatibleList = GetIncompatibleList(rimThreadedIncompatibleList);

            return _loadedModFolders.Where(mod =>
                IsModNameInNamesList(mod, incompatibleList)).ToList();
        }

        public static ModsConfigData LoadModConfigFromUri(string urlToRaw)
        {
            var result = Web.GetUrlStatus(urlToRaw,
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36");

            if (result != HttpStatusCode.OK)
                Extensions.Log($"Failed to obtain search result page, resultCode: {result}", "warn");

            var webClient = new WebClient();
            var doc = webClient.DownloadString(urlToRaw);

            webClient.Dispose();
            return LoadModConfig(doc);
        }

        public static List<string> GetIncompatibleList(string urlToRaw)
        {
            var list = new List<string>();

            if (!new FileInfo(PathToMods + "incompatible.txt").Exists)
            {
                var result = Web.GetUrlStatus(urlToRaw,
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36");
                if (result != HttpStatusCode.OK)
                    Extensions.Log($"Failed to obtain search result page, resultCode: {result}", "warn");
                var webClient = new WebClient();
                webClient.DownloadFile(urlToRaw, PathToMods + "incompatible.txt");
                webClient.Dispose();
            }

            using var sr = new StreamReader(PathToMods + "incompatible.txt");
            while (sr.Peek() >= 0)
            {
                var line = sr.ReadLine();
                while (line != null && !line.Equals("### A")) line = sr.ReadLine();
                while (line != null && !line.Equals("------------------------------------------------------"))
                {
                    if (line.StartsWith("* ["))
                    {
                        var modName = line.StringBetweenStrings("* [", "](");
                        //Extensions.Log($"Adding mod: {modName}", "warn", true);
                        list.Add(modName);
                    }
                    line = sr.ReadLine();
                }
            }

            return list;
        }

        public static bool IsRimThreadedCompatible(ModFolder modFolder, bool strictComparison)
        {
            var _modsNotInRimThreadidModList = GetModsNotInRimThreadidModList();
            var _modsFoundInRimThreadidIncompatibleList = GetModsInRimThreadidIncompatibleList();

            if (_modsFoundInRimThreadidIncompatibleList.Contains(modFolder)) //clearly not compatible
            {
                //Extensions.Log($"Mod not compatible: {modFolder.ModMetaData.Name}", "warn", true);
                return false;
            }
            if (_modsNotInRimThreadidModList.Contains(modFolder) && strictComparison) //unclear
            {
                //Extensions.Log($"Mod likely not compatible: {modFolder.ModMetaData.Name}", "warn", true);
                return false;
            }

            return  true;
        }

        public static bool IsModNameInNamesList(ModFolder modFolder, IEnumerable<string> modNames)
        {
            return modNames.Contains(modFolder.ModMetaData.Name);
        }

        public static bool IsPackageIdInModList(ModFolder modFolder, ModsConfigData modList)
        {
            var packageIdLower = string.Empty;

            if (modFolder.ModMetaData.PackageId != null)
                packageIdLower = modFolder.ModMetaData.PackageId.ToLower();

            return modList.ActiveMods.Contains(packageIdLower);
        }

        //"api.github.com/<GitHubUsername>/<RepoName>"
        public static void DownloadModFromGithub(string githubUri, string outputPath)
        {
            var urlSplit = githubUri.Split("/");

            var httpClient = new HttpClient();
            var author = urlSplit[1];
            var repo = urlSplit[2];

            IDownloaderSettings settings = new DownloaderSettings(httpClient, author, repo, true, outputPath);
            IDownloader downloader = new Downloader.Downloader(settings);

            if (downloader.CheckIfReleasesExist())
            {
                downloader.DownloadLatestRelease();
                var assetName = downloader.GetAssetName();
                if (string.IsNullOrEmpty(assetName)) return;

                var fullFileName = outputPath + assetName;
                var modDirName = fullFileName.Replace(".zip", string.Empty);
                if (new DirectoryInfo(modDirName).Exists)
                    Directory.Delete(modDirName, true);

                try
                {
                    ZipFile.ExtractToDirectory(fullFileName, modDirName);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }

                if (File.Exists(fullFileName)) File.Delete(fullFileName);
            }
            if (downloader.CheckIfTagsExist())
            {
                downloader.DownloadLatestTag();
                var assetName = downloader.GetAssetName();
                if (string.IsNullOrEmpty(assetName)) return;

                var fullFileName = outputPath + assetName;
                var modDirName = fullFileName.Replace(".zip", string.Empty);
                if (new DirectoryInfo(modDirName).Exists)
                    Directory.Delete(modDirName, true);

                try
                {
                    ZipFile.ExtractToDirectory(fullFileName, modDirName);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }

                if (File.Exists(fullFileName)) File.Delete(fullFileName);
                if (Directory.Exists(modDirName))
                {
                    var dirInside = Directory.GetDirectories(modDirName).First();
                    var tempDirPath = outputPath + "temp";

                    if (Directory.Exists(tempDirPath)) Directory.Delete(tempDirPath);
                    Directory.Move(new DirectoryInfo(dirInside).FullName, tempDirPath);

                    if (Directory.Exists(modDirName)) Directory.Delete(modDirName);
                    Directory.Move(tempDirPath, modDirName);
                }
            }
            downloader.DeInit();
            httpClient.Dispose();
        }

        //Todo: What if mod has no release?
        //download repo
        //compile src via devenv.exe
        //create modFolder from compiled src

        public static void DownloadModsFromSteam(IEnumerable<uint> workshopIds)
        {
            var dllPath = PathToSteamCmd + @"steamapps\workshop\content\" + CmdArgs.Steam.RimAppId.Trim() + @"\";

            var downloadArgs = workshopIds.Aggregate("", (current, id) =>
                current + CmdArgs.Steam.DownloadWorkshopItem + CmdArgs.Steam.RimAppId + id);
            var cmdArgs = CmdArgs.Steam.AnonLogin + downloadArgs + CmdArgs.Steam.Quit;

            var steamCmd = new Launcher(PathToSteamCmd + "steamcmd.exe", cmdArgs);
            steamCmd.Launch();

            if (steamCmd.Finished)
                foreach (var modDir in new DirectoryInfo(dllPath).GetDirectories())
                    modDir.MoveTo($@"{PathToMods}\{modDir.Name}");
        }

        public static int GetWorkshopIdFromPackageId(string packageId) //TODO: may not be reliable
        {
            const string rimSearchUri = @"https://steamcommunity.com/workshop/browse/?appid=294100&searchtext=";
            var packageIdSearch = packageId.Replace(".", "+");
            var modSearchUri = rimSearchUri + packageIdSearch;
            var result = Web.GetUrlStatus(modSearchUri,
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36");
            if (result != HttpStatusCode.OK)
            {
                Extensions.Log($"Failed to obtain search result page, resultCode: {result}", "warn");
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

                    if (modName.Contains(likelyName, StringComparison.InvariantCultureIgnoreCase) 
                        || modNameNoWs.Contains(likelyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Extensions.Log($"likelyName: {likelyName} modName: {modName}", "warn");

                        var hrefNode = modTitle.ParentNode.OuterHtml;
                        var workshopId = hrefNode.StringBetweenStrings("?id=", "&");

                        Extensions.Log($"Likely we have found the workshopId: {workshopId} for mod: {modName} ", "success");
                        return int.Parse(workshopId);
                    }
                }
            }

            Extensions.Log("Failed to find the mod among results!", "warn");
            return 0;
        }

        public static void CheckActiveModsAgainstLocalMods()
        {
            var activeMods = _loadedModConfig.ActiveMods;
            var localMods = _loadedModFolders.Select(modFolder =>
                modFolder.ModMetaData).Select(metaData =>
                metaData.PackageId).ToList();
            var modsNotFound = activeMods.Where(activeMod =>
                !localMods.Contains(activeMod));

            foreach (var notFoundMod in modsNotFound)
            {
                if (notFoundMod.Equals("ludeon.rimworld") || notFoundMod.Equals("ludeon.rimworld.royalty")) continue;
                Extensions.Log($"Modlist references mod not found localy: {notFoundMod}", "warn");
                var response = AnsiConsole.Confirm("[green]Would you like to download missing mods?[/]:");

                if (response)
                {
                    var workshopId = GetWorkshopIdFromPackageId(notFoundMod);
                    //TODO: download via steamCMD
                }
            }
        }

        public static void LoadLocalModConfig()
        {
            const string pathToLocalLow = @"%userprofile%\appdata\locallow";
            const string pathToConfig = @"\Ludeon Studios\RimWorld by Ludeon Studios\Config\";
            const string configName = "ModsConfig.xml";
            var path = Environment.ExpandEnvironmentVariables(pathToLocalLow + pathToConfig + configName);

            _loadedModConfig = LoadModConfig(path);
        }

        public static ModsConfigData LoadModConfig(string xml, string path = "")
        {
            if (path.Valid()) xml = File.ReadAllText(path);

            return ModsConfigData.GetFromXml(xml);
        }

        public static IEnumerable<string> GetDependenciesPackageIds()
        {
            var result = new List<string>();
            foreach (var modFolder in _loadedModFolders)
            {
                var metaData = modFolder.ModMetaData;
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
            var modPackageIds = _loadedModFolders.Select(modFolder => 
                modFolder.ModMetaData).Select(metaData => metaData.PackageId).ToList();
            var dataPackageIds = _loadedDataFolders.Select(dataFolder =>
                dataFolder.ModMetaData).Select(metaData => metaData.PackageId).ToList();

            foreach (var dependencyId in dependenciesIds.Where(dependencyId => !modPackageIds.Contains(dependencyId)))
            {
                const string rimCore = "Ludeon.RimWorld";
                const string rimRoyalty = "Ludeon.RimWorld.Royalty";

                if (dependencyId.Equals(rimCore) || dependencyId.Equals(rimRoyalty))
                {
                    if (!dataPackageIds.Contains(rimCore) || !dataPackageIds.Contains(rimRoyalty))
                        Extensions.Log($"Core dependency not found: {dependencyId}", "warn");
                    continue;
                }
                Extensions.Log($"Mod dependency not found: {dependencyId}", "warn");
            }
        }

        private static void CheckForOutdatedMods()
        {
            foreach (var modFolder in _loadedModFolders.Where(modFolder => modFolder.IsOutdated())) 
                Extensions.Log($"Outdated mod: {modFolder.Name}", "info");
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
                            //Extensions.Log($"not recognized: {currentDataDir.Name} path:{currentDataDir.ToString()}", "warn");
                            break;
                    }
                }

                _loadedDataFolders.Add(dataFolder);
            }
        }

        private static void LoadModDirs(string dirPath)
        {
            foreach (var modDirPath in Directory.GetDirectories(dirPath))
            {
                if (modDirPath.Equals(PathToMods + "IncompatibleWithRimThreadid")) continue;
                var dirInfo = new DirectoryInfo(modDirPath);
                var modFolder = new ModFolder(dirInfo.Name, dirInfo.FullName);
                if (!modFolder.IsValid()) 
                    Extensions.Log($"Invalid mod: {modFolder.Name}", "info");

                foreach (var folder in Directory.GetDirectories(modDirPath))
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
                            //Extensions.Log($"not recognized: {currentModDir.Name} path:{currentModDir.ToString()}", "warn");
                            break;
                    }
                }

                _loadedModFolders.Add(modFolder);
            }
        }

        private static void RenameWorkshopIdToModName()
        {
            foreach (var modFolder in _loadedModFolders)
            {
                if (!modFolder.Name.IsDigitOnly()) continue;

                var modName = modFolder.ModMetaData.Name;

                string[] illegalChars = { "<", ">", ":", "\"", "/", "\\", "|", "?", "*"};
                foreach (var @char in illegalChars)
                    if (modName.Contains(@char))
                        modName = modName.Replace(@char, "");
                
                Extensions.Log($"Renaming mod: {modFolder.Name} to {modName}", "info", true);
                modFolder.Name = modName;

                new DirectoryInfo(modFolder.Path).Rename(modName);
            }
        }

        private static void CheckForIncompatibleMods()
        {
            var modNames = new List<string>();
            var modPackages = new List<string>();
            var modIncompatibleLists = new List<Tuple<List<string>, string>>();
            foreach (var modFolder in _loadedModFolders)
            {
                var modMetaData = modFolder.ModMetaData;
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
                            Extensions.Log($"Incompatible mods found! Mod1: {modName} with Mod2: {curModName}", "warn");
                        }
                    }
                }
            }
        }
    }
}
