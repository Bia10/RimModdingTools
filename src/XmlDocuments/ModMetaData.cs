using Microsoft.Language.Xml;
using RimModdingTools.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimModdingTools.XmlDocuments
{
    public class ModMetaData
    {
        public class Version
        {
            public int Major;
            public int Minor;

            public Version(int major, int minor)
            {
                Major = major;
                Minor = minor;
            }
        }

        public class ModDependency
        {
            public string PackageId;
            public string DisplayName;
            public Uri SteamWorkshopUrl;
            public Uri DownloadUrl;
        }

        public string Name;
        public string Author;
        public string PackageId;
        public Uri Url;
        public List<Version> SupportedVersions;
        public List<string> LoadAfter;
        public List<string> LoadBefore;
        public List<string> IncompatibleWith;
        public List<ModDependency> ModDependencies;
        public string Description;

        public void PrintHeader()
        {
            AnsiConsoleExtensions.Log($"Name: {Name} " + 
                                      $"\n\t\t\t Author: {Author} " +
                                      $"\n\t\t\t PackageId: {PackageId} " +
                                      $"\n\t\t\t Url: {Url?.AbsolutePath}", "info");
        }

        public static ModMetaData GetFromXml(string xml)
        {
            var root = Parser.ParseText(xml);
            var descendantList = root.Descendants().Select(x => x.AsElement).ToList();

            ModMetaData modInfo = null;
            foreach (var descendant in descendantList)
            {
                if (descendant.Name.Equals("ModMetaData"))
                    modInfo = new ModMetaData()
                    {
                        SupportedVersions = new List<Version>(),
                        LoadBefore = new List<string>(),
                        LoadAfter = new List<string>(),
                        IncompatibleWith = new List<string>(),
                        ModDependencies = new List<ModDependency>()
                    };
                if (modInfo == null) return null;

                switch (descendant.Name)
                {
                    case "name":
                        modInfo.Name = descendant.Value;
                        break;
                    case "author":
                        modInfo.Author = descendant.Value;
                        break;
                    case "packageId":
                        modInfo.PackageId = descendant.Value;
                        break;
                    case "url":
                        if (descendant.Value == "") break;
                        modInfo.Url = new Uri(descendant.Value);
                        break;
                    case "supportedVersions":
                        foreach (var supportedVersion in descendant.Elements)
                        {
                            var majorAndMinor = supportedVersion.Value.Split(".", 2);
                            var major = int.Parse(majorAndMinor[0]);
                            var minor = int.Parse(majorAndMinor[1]);

                            modInfo.SupportedVersions.Add(new Version(major, minor));
                        }
                        break;
                    case "loadAfter":
                        foreach (var afterElement in descendant.Elements)
                            modInfo.LoadAfter.Add(afterElement.Value);
                        break;
                    case "loadBefore":
                        foreach (var beforeElement in descendant.Elements)
                            modInfo.LoadBefore.Add(beforeElement.Value);
                        break;
                    case "incompatibleWith":
                        foreach (var incompatibleElement in descendant.Elements)
                            modInfo.IncompatibleWith.Add(incompatibleElement.Value);
                        break;
                    case "modDependencies":
                        foreach (var modDependency in descendant.Elements)
                        {
                            ModDependency curDependency = null;
                            if (modDependency.Name.Equals("li"))
                                curDependency = new ModDependency();
                            if (curDependency == null) return null;

                            foreach (var dependencyElement in modDependency.Elements)
                            {
                                switch (dependencyElement.Name)
                                {
                                    case "packageId":
                                        curDependency.PackageId = dependencyElement.Value;
                                        break;
                                    case "displayName":
                                        curDependency.DisplayName = dependencyElement.Value;
                                        break;
                                    case "steamWorkshopUrl":
                                        curDependency.SteamWorkshopUrl = new Uri(dependencyElement.Value);
                                        break;
                                    case "downloadUrl":
                                        curDependency.DownloadUrl = new Uri(dependencyElement.Value);
                                        break;
                                }
                            }
                            modInfo.ModDependencies.Add(curDependency);
                        }
                        break;
                    case "description":
                        modInfo.Description = descendant.Value;
                        break;
                }
            }

            return modInfo;
        }
    }
}