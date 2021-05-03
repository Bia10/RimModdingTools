﻿using RimModdingTools.Utils;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace RimModdingTools
{
    public class Util
    {
        public static bool IsDigitOnly(string str)
        {
            return str.All(char.IsDigit);
        }

        public static string StringBetweenStrings(string input, string start, string end)
        {
            var indexOfStart = input.IndexOf(start, StringComparison.Ordinal) + start.Length;
            var indexOfEnd = input.IndexOf(end, indexOfStart, StringComparison.Ordinal);

            return string.IsNullOrEmpty(end) ? input[indexOfStart..] : input[indexOfStart..indexOfEnd];
        }


        public static bool Contains(string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }


        public static HttpStatusCode GetUrlStatus(string url, string userAgent)
        {
            var result = default(HttpStatusCode);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = userAgent;
            request.Method = "HEAD";

            try
            {
                using var response = request.GetResponse() as HttpWebResponse;
                if (response != null)
                {
                    result = response.StatusCode;
                    response.Close();
                }
            }
            catch (WebException we)
            {
                if (we.Response != null)
                    result = ((HttpWebResponse)we.Response).StatusCode;
            }

            return result;
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