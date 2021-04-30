﻿using RimModdingTools.Utils;
using System;
using System.IO;
using System.Linq;

namespace RimModdingTools
{
    public class Util
    {
        public static bool IsDigitOnly(string str)
        {
            return str.All(char.IsDigit);
        }

        public static bool RenameFolder(string dirFullPath, string newFolderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dirFullPath) || string.IsNullOrWhiteSpace(newFolderName)) return false;

                var oldDirectory = new DirectoryInfo(dirFullPath);
                if (!oldDirectory.Exists) return false;

                //new folder name is the same with the old one.
                if (string.Equals(oldDirectory.Name, newFolderName, StringComparison.OrdinalIgnoreCase)) return false;

                var newDirectory = Path.Combine(oldDirectory.Parent == null ?
                    dirFullPath : oldDirectory.Parent.FullName, newFolderName);

                //target directory already exists
                if (Directory.Exists(newDirectory)) return false;

                oldDirectory.MoveTo(newDirectory);
                return true;
            }
            catch
            {
                AnsiConsoleExtensions.Log($"Failed to rename Dir: {dirFullPath} to name: {newFolderName}", "warn");
                return false;
            }
        }
    }
}