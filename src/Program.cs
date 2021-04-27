using RimModdingTools.XmlDocuments;
using System.IO;

namespace RimModdingTools
{
    static class Program
    {
        static void Main()
        {
            var xmlPath = @"C:\Games\RimWorld\Mods\2038409475\About\About.xml";
            var xml = File.ReadAllText(xmlPath);
            var modInfo = ModMetaData.GetFromXml(xml);

            modInfo.PrintHeader();
        }
    }
}
