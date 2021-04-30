using Microsoft.Language.Xml;
using System.Collections.Generic;
using System.Linq;

namespace RimModdingTools.XmlDocuments
{
    public class ModsConfigData
    {
        public class Version
        {
            public int Major;
            public int Minor;
            public int BuildNumber;
            public string Revision;

            public Version(int major, int minor, int buildNumber, string revision)
            {
                Major = major;
                Minor = minor;
                BuildNumber = buildNumber;
                Revision = revision;
            }
        }

        public Version GameVersion;
        public List<string> ActiveMods;
        public List<string> KnownExpansions;

        public static ModsConfigData GetFromXml(string xml)
        {
            var root = Parser.ParseText(xml);
            var descendantList = root.Descendants().Select(x => x.AsElement).ToList();

            ModsConfigData modConfigInfo = null;
            foreach (var descendant in descendantList)
            {
                if (descendant.Name.Equals("ModsConfigData"))
                    modConfigInfo = new ModsConfigData()
                    {
                        GameVersion = null,
                        ActiveMods = new List<string>(),
                        KnownExpansions = new List<string>()
                    };
                if (modConfigInfo == null) return null;

                switch (descendant.Name)
                {
                    case "version":
                        var majorAndMinor = descendant.Value.Split(".", 3);
                        var major = majorAndMinor[0];
                        var minor = majorAndMinor[1];
                        var buildAndRev = majorAndMinor[2].Split(" ", 2);
                        var buildNum = buildAndRev[0];
                        var rev = buildAndRev[1];

                        modConfigInfo.GameVersion =
                            new Version(int.Parse(major), int.Parse(minor), int.Parse(buildNum), rev);
                        break;
                    case "activeMods":
                        foreach (var activeModElement in descendant.Elements)
                            if (activeModElement.Name.Equals("li"))
                                modConfigInfo.ActiveMods.Add(activeModElement.Value);
                        break;
                    case "knownExpansions":
                        foreach (var knownExpansionElement in descendant.Elements)
                            if (knownExpansionElement.Name.Equals("li"))
                                modConfigInfo.KnownExpansions.Add(knownExpansionElement.Value);
                        break;
                }
            }

            return modConfigInfo;
        }
    }
}