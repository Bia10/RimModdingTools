using Spectre.Console;
using System;
using System.Diagnostics;
using System.IO;
using AnsiConsoleExtensions = RimModdingTools.Utils.AnsiConsoleExtensions;

namespace RimModdingTools
{
    public class ProcLauncher
    {
        private static string _exePath;
        private readonly string _cmdArgs;

        public struct CmdArguments
        {
            public const string AnonLogin = "+login anonymous";
            public const string ForceInstallDir = " +force_install_dir ";
            public const string DownloadWorkshopItem = " +workshop_download_item";
            public const string RimAppId = " 294100 ";
            public const string Quit = " +quit";
        }

        public ProcLauncher(string exePath, string cmdArgs)
        {
            _exePath = exePath;
            _cmdArgs = cmdArgs;
        }

        public void Launch()
        {
            AnsiConsoleExtensions.Log($"Attempt to start:\n {_exePath} || {_cmdArgs}", "info");

            var exeFile = new FileInfo(_exePath);
            var workDir = exeFile.DirectoryName;
            if (workDir == null)
            {
                AnsiConsoleExtensions.Log("Work directory null, cannot launch!", "error");
                return;
            }

            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = _exePath,
                        Arguments = _cmdArgs,
                        WorkingDirectory = workDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.OutputDataReceived += OutputHandler;
                process.ErrorDataReceived += ErrorHandler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            AnsiConsoleExtensions.Log(outLine.Data, "info");
        }

        private static void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            AnsiConsoleExtensions.Log(outLine.Data, "error");
        }
    }
}