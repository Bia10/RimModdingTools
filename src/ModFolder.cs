using RimModdingTools.XmlDocuments;
using System.IO;
using System.Linq;

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

        public ModMetaData LoadModMetaData()
        {
            var aboutFile = Directory.GetFiles(About.FullName).First(file => file.Contains("About.xml"));
            var xmlText = File.ReadAllText(aboutFile);
            var modMetaData = ModMetaData.GetFromXml(xmlText);

            return modMetaData;
        }

        //Todo: other checks
        public bool IsValid()
        {
            return VersionOneZero == null || VersionOneZero != null && VersionOneZero.GetFiles().Length == 0
                   && VersionOneOne == null || VersionOneOne != null && VersionOneOne.GetFiles().Length == 0 
                   && VersionOneTwo == null || VersionOneTwo != null && VersionOneTwo.GetFiles().Length == 0
                   && Assemblies == null || Assemblies != null && Assemblies.GetFiles().Length == 0
                   && Defs == null || Defs != null && Defs.GetFiles().Length == 0;
        }
    }
}