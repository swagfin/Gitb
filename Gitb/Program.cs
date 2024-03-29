﻿using CommandLine;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Gitb
{
    public class Program
    {
        public class ArgsOptions
        {
            [Option('p', "git-repo-path", Required = false, HelpText = "Git Repository Path")]
            public string GitRepositoryPath { get; set; } = null;

            [Option('c', "skip-compression", Required = false, HelpText = "Skip Backup Compression")]
            public bool SkipCompression { get; set; } = false;

            [Option('v', "version-file", Required = false, HelpText = "Backup Versioning File, used to save last version number")]
            public string VersionFile { get; set; } = null;

            [Option('x', "exclude-version-files", Required = false, HelpText = "Exclude Version Files from Backup and Compression, Default true (will not include VersionFiles to backup)")]
            public bool ExcludeVersionFiles { get; set; } = true;

            [Option('u', "skip-user-prompts", Required = false, HelpText = "Skip User Prompts and Confirmation Y/N")]
            public bool SkipUserPrompts { get; set; } = false;
        }

        [STAThread]
        static void Main(string[] args)
        {
            ConsoleX.WriteLine(" ** Git Backup :-)");
            if (args.Length > 0 && args.Contains("--version"))
            {
                ConsoleX.WriteLine($"Current Version: v.{AppVersion}", ConsoleColor.Cyan);
                Environment.Exit(0);
            }
            ArgsOptions options = new ArgsOptions();
            //Parse arguments
            Parser.Default.ParseArguments<ArgsOptions>(args).WithParsed(x => { options = x; });
            GitBackupUncommitedFiles backupModifiedGitFiles = new GitBackupUncommitedFiles(options);

            //** check git status
            if (!backupModifiedGitFiles.IsGitInstalled())
            {
                ConsoleX.WriteLine("Git is not installed. Opening browser to download...", ConsoleColor.Yellow);
                Process.Start("https://git-scm.com/");
            }
            else
            {
                backupModifiedGitFiles.StartBackup();
                Console.WriteLine("--- DONE ---");
            }

#if DEBUG
            Console.ReadLine();
#endif
        }

        public static string AppVersion { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
        public static string AssemblyDirectory { get { return Environment.CurrentDirectory; } }
    }
}
