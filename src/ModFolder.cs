using System.IO;
using System.Linq;
using RimModdingTools.XmlDocuments;

namespace RimModdingTools
{
    public class ModFolder
    {
        public string Name;
        public string Path;
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

        public ModFolder(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string GetModName()
        {
            var aboutFile = Directory.GetFiles(About.FullName).First(file => file.Contains("About.xml"));
            var xmlText = File.ReadAllText(aboutFile);
            var modMetaData = ModMetaData.GetFromXml(xmlText);

            return modMetaData.Name;
        }

        //Todo: other checks
        public bool IsValid()
        {
            return VersionOneZero != null || VersionOneOne != null || VersionOneTwo != null 
                   || Assemblies != null || Defs != null;
        }
    }
}