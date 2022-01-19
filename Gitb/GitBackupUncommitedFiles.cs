using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public GitBackupUncommitedFiles(string[] args)
        {
            if (args.Contains("p"))
                GitRepositoryPath = args.FirstOrDefault(x => x.Equals("p", StringComparison.OrdinalIgnoreCase));
            else
                GitRepositoryPath = AppDomain.CurrentDomain.BaseDirectory;
#if DEBUG
            GitRepositoryPath = GitRepositoryPath.Replace("Gitb\\bin\\Debug\\", string.Empty);
#endif

        }

        public void StartBackup()
        {
            try
            {
                GetAffectedFiles();

                if (GitAffectedFilesList.Count >= 1)
                {
                    CurrentBranch = CmdRunCommands.RunCommands(new List<string> { GitRepositoryPath.Substring(0, 2), $@"cd {GitRepositoryPath}", @"git branch --show-current" });

                    ConsoleX.WriteLine($"Discovered {GitAffectedFilesList.Count} added/modified file(s) proceed? Y/N", ConsoleColor.Green);
                    bool confirmed = ConsoleX.ReadLineYesNoConfirmed();
                    if (!confirmed)
                    {
                        ConsoleX.WriteLine("Task Cancelled", ConsoleColor.Yellow);
                        Environment.Exit(0);
                    }
                    //Proceed
                    BackupFiles();
                }
                else
                {
                    ConsoleX.WriteLine("No Changed Files", ConsoleColor.Yellow);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                ConsoleX.WriteLine(ex.Message, ConsoleColor.Yellow);
                Environment.Exit(0);
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
            string versionNoFile = $"{AppDomain.CurrentDomain.BaseDirectory}\\current-version.txt";
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
            string versionFileName = $"update_v{currentVersion}";
            string directory = $"{AppDomain.CurrentDomain.BaseDirectory}\\Updates\\{versionFileName}";
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

            ConsoleX.WriteLine($"Copied Successfully :-)");
        }
    }
}
