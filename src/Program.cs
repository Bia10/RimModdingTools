using RimModdingTools.XmlDocuments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using SixLabors.ImageSharp.Processing;
//using Spectre.Console;
using AnsiConsoleExtensions = RimModdingTools.Utils.AnsiConsoleExtensions;

namespace RimModdingTools
{
    static class Program
    {
        static List<ModFolder> LoadedModFolders = new();

        static void Main()
        {
            LoadModDirs(@"C:\Games\RimWorld\Mods\");
            AnsiConsoleExtensions.Log($"{LoadedModFolders.Count}", "warn");

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

            ScanForIncompatibleMods();
            CheckForOutdatedMods();
            RenameWorkshopIdToModName();
            CheckIfMissingDependency();
        }

        public static List<string> GetDependenciesPackageIds()
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

        //TODO: reference to original game data core, roaylty
        public static void CheckIfMissingDependency()
        {
            var dependenciesIds = GetDependenciesPackageIds();
            var packageIds = LoadedModFolders.Select(modFolder => modFolder.LoadModMetaData()).Select(metaData => metaData.PackageId).ToList();

            foreach (var dependencyId in dependenciesIds.Where(dependencyId => !packageIds.Contains(dependencyId)))
            {
                AnsiConsoleExtensions.Log($"Dependency not found: {dependencyId}", "warn");
            }
        }

        private static void CheckForOutdatedMods()
        {
            foreach (var modFolder in LoadedModFolders.Where(modFolder => modFolder.IsOutdated())) 
                AnsiConsoleExtensions.Log($"Outdated mod: {modFolder.Name}", "info");
        }

        private static void LoadModDirs(string dirPath)
        {
            var modDirs = Directory.GetDirectories(dirPath);

            foreach (var modDir in modDirs)
            {
                var dirInfo = new DirectoryInfo(modDir);
                var modFolder = new ModFolder(dirInfo.Name, dirInfo.FullName);

                if (!modFolder.IsValid()) AnsiConsoleExtensions.Log($"Invalid mod: {modFolder.Name}", "info");


                var foldersInside = Directory.GetDirectories(modDir);

                foreach (var folder in foldersInside)
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

        private static void ScanForIncompatibleMods()
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
