using Microsoft.Language.Xml;
using System.Collections.Generic;
using System.Linq;

namespace RimModdingTools.XmlDocuments
{
    public class ModsConfigData
    {
        public string Version;
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
                        Version = string.Empty,
                        ActiveMods = new List<string>(),
                        KnownExpansions = new List<string>()
                    };
                if (modConfigInfo == null) return null;

                switch (descendant.Name)
                {
                    case "version":
                        var majorAndMinor = descendant.Value.Split(".", 2);
                            modConfigInfo.Version = $"{majorAndMinor[0]}.{majorAndMinor[1]}";
                        break;
                    case "activeMods":
                        foreach (var activeModElement in descendant.Elements)
                            if (activeModElement.Name.Equals("li"))
                                modConfigInfo.ActiveMods.Add(activeModElement.Value);
                        break;
                    case "knownExpansions":
                        foreach (var activeModElement in descendant.Elements)
                            if (activeModElement.Name.Equals("li"))
                                modConfigInfo.KnownExpansions.Add(activeModElement.Value);
                        break;
                }
            }

            return modConfigInfo;
        }
    }
}