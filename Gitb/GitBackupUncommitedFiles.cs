﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Gitb
{
    public class GitBackupUncommitedFiles
    {
        public List<string> GitAffectedFilesList = new List<string>();

        public string _GitRepositoryPath { get; set; }

        public string CurrentBranch { get; set; }

        public string CopiedDirectory { get; set; }

        public string GitRepositoryPath
        {

            get
            {
                if (string.IsNullOrWhiteSpace(_GitRepositoryPath))
                {
                    throw new Exception("Unspecified Git Repository");
                }
                return _GitRepositoryPath;
            }
            set
            {
                _GitRepositoryPath = value;
            }

        }
        public bool SkipConfirmation { get; set; } = false;
        public bool SkipCompression { get; set; } = false;
        public string VersionFile { get; set; } = null;

        public GitBackupUncommitedFiles(string[] args)
        {
            if (args.Contains("git-repo-path"))
                this.GitRepositoryPath = args.FirstOrDefault(x => x.Equals("git-repo-path", StringComparison.OrdinalIgnoreCase));
            else
                GitRepositoryPath = AppDomain.CurrentDomain.BaseDirectory;
            if (args.Contains("version-file"))
                this.VersionFile = args.FirstOrDefault(x => x.Equals("version-file", StringComparison.OrdinalIgnoreCase));
            else
                this.GitRepositoryPath = AppDomain.CurrentDomain.BaseDirectory;
#if DEBUG
            this.GitRepositoryPath = GitRepositoryPath.Replace("Gitb\\bin\\Debug\\", string.Empty);
#endif

            if (args.Contains("skip-confirmation"))
                this.SkipConfirmation = true;
            if (args.Contains("skip-compression"))
                this.SkipCompression = true;
        }

        public void StartBackup()
        {
            try
            {
                GetAffectedFiles();
                //Clean up
                GitAffectedFilesList = GitAffectedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (GitAffectedFilesList.Count >= 1)
                {
                    CurrentBranch = CmdRunCommands.RunCommands(new List<string> { GitRepositoryPath.Substring(0, 2), $@"cd {GitRepositoryPath}", @"git branch --show-current" });

                    if (!this.SkipConfirmation)
                    {
                        ConsoleX.WriteLine($"Discovered {GitAffectedFilesList.Count} added/modified file(s) proceed? Y/N", ConsoleColor.Green);
                        bool confirmed = ConsoleX.ReadLineYesNoConfirmed();
                        if (!confirmed)
                            ConsoleX.WriteLine("Task Cancelled", ConsoleColor.Yellow);
                    }
                    //Proceed
                    BackupFiles();
                }
                else
                {
                    ConsoleX.WriteLine("No Changed Files", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                ConsoleX.WriteLine(ex.Message, ConsoleColor.Yellow);
            }
        }

        public void GetAffectedFiles()
        {
            GitAffectedFilesList.Clear();
            Console.WriteLine($"Scanning dir: {GitRepositoryPath}");
            // Get added modified files list
            List<string> getAffectedFilesStrings = new List<string> {
                CmdRunCommands.RunCommands(new List<string> { GitRepositoryPath.Substring(0, 2), $@"cd {GitRepositoryPath}", @"git diff --cached --name-only --diff-filter=A" }),
                CmdRunCommands.RunCommands(new List<string> { GitRepositoryPath.Substring(0, 2), $@"cd {GitRepositoryPath}", @"git diff --cached --name-only --diff-filter=M" }),
                CmdRunCommands.RunCommands(new List<string> { GitRepositoryPath.Substring(0, 2), $@"cd {GitRepositoryPath}", @"git ls-files -m --others --exclude-standard" })
            };

            for (int i = 0; i < getAffectedFilesStrings.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(getAffectedFilesStrings[i]) && i < 2)
                {
                    GitAffectedFilesList.AddRange(getAffectedFilesStrings[i].Split('\n').ToList());
                }

                if (GitAffectedFilesList.Count == 0 && i > 1)
                {
                    GitAffectedFilesList.AddRange(getAffectedFilesStrings[i].Split('\n').ToList());
                }
            }
        }

        public void BackupFiles()
        {

            GitAffectedFilesList = GitAffectedFilesList.Select(s => $@"{GitRepositoryPath}\{s.Replace("/", "\\")}").ToList();
            //Check Version
            int currentVersion = 0;
            this.VersionFile = string.IsNullOrWhiteSpace(this.VersionFile) ? "current-version.html" : this.VersionFile.Trim();
            string versionNoFile = $"{AppDomain.CurrentDomain.BaseDirectory}\\{this.VersionFile}";
            //Check Last Version
            if (!File.Exists(versionNoFile))
                File.WriteAllText(versionNoFile, $"{currentVersion}");
            else
            {
                string lastBackupContents = File.ReadAllText(versionNoFile);
                if (string.IsNullOrWhiteSpace(lastBackupContents) || !int.TryParse(lastBackupContents, out currentVersion))
                    File.WriteAllText(versionNoFile, $"{currentVersion}"); //Problem with the file needs force-update to default
            }
            //Auto Increase new Version
            currentVersion++;
            ConsoleX.WriteLine($"-------- Preparing version v.{currentVersion} --------", ConsoleColor.Gray);
            string versionFileName = $"update_v{currentVersion}";
            string directory = $"{AppDomain.CurrentDomain.BaseDirectory}\\Updates\\{versionFileName}";
            string zipFileSave = $"{directory}.zip";

            //Proceed
            ConsoleX.WriteLine("Preparing directory....");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            //Create Directory
            Directory.CreateDirectory(directory);
            ConsoleX.WriteLine("Backing up uncommitted files....");

            List<string> copyCommands = new List<string>();
            foreach (string affectedFilePath in GitAffectedFilesList)
            {
                ConsoleX.WriteLine($"Backing up: {affectedFilePath}");
                copyCommands.Add($"xcopy \"{affectedFilePath}\" \"{directory}\" ");
            }
            CmdRunCommands.RunCommands(copyCommands);
            //#Zipping
            ConsoleX.WriteLine("Compressing changed files.....", ConsoleColor.Cyan);
            ICSharpCode.SharpZipLib.Zip.FastZip z = new ICSharpCode.SharpZipLib.Zip.FastZip();
            z.CreateEmptyDirectories = true;
            z.CompressionLevel = ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.BEST_COMPRESSION;
            z.CreateZip(zipFileSave, directory, true, string.Empty);
            ConsoleX.WriteLine($"Compression Success, Zip file: {zipFileSave}", ConsoleColor.Cyan);

            //Update Version
            ConsoleX.WriteLine("Updating version File....");
            File.WriteAllText(versionNoFile, $"{currentVersion}");
            //Proceed
            ConsoleX.WriteLine("Cleaning up....");
            Thread.Sleep(1500); //allow time for Zip to release file
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            ConsoleX.WriteLine($"Backup Successfully | Version v.{currentVersion} :-)", ConsoleColor.Green);
        }
    }
}
