using System.IO;

namespace RimModdingTools
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

        //Todo: other checks
        public bool IsValid()
        {
            return VersionOneZero != null || VersionOneOne != null || VersionOneTwo != null 
                   || Assemblies != null || Defs != null;
        }
    }
}