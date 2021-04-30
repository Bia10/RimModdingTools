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
            var dirPath = @"C:\Games\RimWorld\Mods\";
            LoadModDirs(dirPath);

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
                            modInfo.PrintHeader();
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
            RenameWorkshopIdToModName();
        }

        private static bool IsDigitOnly(string str)
        {
            return str.All(char.IsDigit);
        }

        private static void RenameWorkshopIdToModName()
        {
            foreach (var modFolder in LoadedModFolders)
            {
                if (!IsDigitOnly(modFolder.Name)) continue;

                var modName = modFolder.GetModName();

                string[] illegalChars = { "<", ">", ":", "\"", "/", "\\", "|", "?", "*"};
                foreach (var @char in illegalChars)
                {
                    if (modName.Contains(@char))
                        modName = modName.Replace(@char, "");
                }

                AnsiConsoleExtensions.Log($"Renaming mod: {modFolder.Name} to {modName}", "info");
                modFolder.Name = modName;
                RenameFolder(modFolder.Path, modName);
            }
        }

        private static void LoadModDirs(string dirPath)
        {
            var modDirs = Directory.GetDirectories(dirPath);

            foreach (var modDir in modDirs)
            {
                var dirInfo = new DirectoryInfo(modDir);
                var modFolder = new ModFolder(dirInfo.Name, dirInfo.FullName);
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

        private static void ScanForIncompatibleMods()
        {
            var modNames = new List<string>();
            var modPackages = new List<string>();
            var modIncompatibleLists = new List<Tuple<List<string>, string>>();
            foreach (var modFolder in LoadedModFolders)
            {
                var aboutFile = Directory.GetFiles(modFolder.About.FullName).First(file => file.Contains("About.xml"));
                var xmlText = File.ReadAllText(aboutFile);
                var modMetaData = ModMetaData.GetFromXml(xmlText);

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

        /// <summary>
        /// Renames a folder name
        /// </summary>
        /// <param name="directory">The full directory of the folder</param>
        /// <param name="newFolderName">New name of the folder</param>
        /// <returns>Returns true if rename is successfull</returns>
        public static bool RenameFolder(string directory, string newFolderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory) ||
                    string.IsNullOrWhiteSpace(newFolderName))
                {
                    return false;
                }


                var oldDirectory = new DirectoryInfo(directory);

                if (!oldDirectory.Exists)
                {
                    return false;
                }

                if (string.Equals(oldDirectory.Name, newFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    //new folder name is the same with the old one.
                    return false;
                }

                string newDirectory;

                if (oldDirectory.Parent == null)
                {
                    //root directory
                    newDirectory = Path.Combine(directory, newFolderName);
                }
                else
                {
                    newDirectory = Path.Combine(oldDirectory.Parent.FullName, newFolderName);
                }

                if (Directory.Exists(newDirectory))
                {
                    //target directory already exists
                    return false;
                }

                oldDirectory.MoveTo(newDirectory);

                return true;
            }
            catch
            {
                //ignored
                return false;
            }
        }

    }
}
