using RimModdingTools.XmlDocuments;
using System.Collections.Generic;
using System.IO;
using AnsiConsoleExtensions = RimModdingTools.Utils.AnsiConsoleExtensions;

namespace RimModdingTools
{
    static class Program
    {
        public class ModFolder
        {
            public DirectoryInfo VersionOneZero;
            public DirectoryInfo VersionOneOne;
            public DirectoryInfo VersionOneTwo;
            public DirectoryInfo About;
            public DirectoryInfo Assemblies;
            public DirectoryInfo Defs;
            public DirectoryInfo Languages;
            public DirectoryInfo News;
            public DirectoryInfo Patches;
            public DirectoryInfo Sounds;
            public DirectoryInfo Textures; 
            public DirectoryInfo Source;
        }

        static List<ModFolder> LoadedModFolders = new();

        static void Main()
        {
            var dirPath = @"C:\Games\RimWorld\Mods\";
            LoadModDirs(dirPath);

            AnsiConsoleExtensions.Log($"{LoadedModFolders.Count}", "warn");
            foreach (var modFolders in LoadedModFolders)
            {
                foreach (var file in Directory.GetFiles(modFolders.About.Name))
                {
                    var curFile = new FileInfo(file);
                    if (curFile.Name != "About.xml") continue;

                    var xml = File.ReadAllText(curFile.FullName);
                    var modInfo = ModMetaData.GetFromXml(xml);

                    modInfo.PrintHeader();
                }
            }
        }

        private static void LoadModDirs(string dirPath)
        {
            var modDirs = Directory.GetDirectories(dirPath);

            foreach (var modDir in modDirs)
            {
                var modFolder = new ModFolder();
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
                        case "Source" or "source" or"src":
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
    }
}
