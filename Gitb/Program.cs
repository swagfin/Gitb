using System;

namespace Gitb
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ConsoleX.WriteLine("(-: Git Backup :-)");
            GitBackupUncommitedFiles backupModifiedGitFiles = new GitBackupUncommitedFiles(args);
            //Start Backup
            backupModifiedGitFiles.StartBackup();
            Console.WriteLine("--- DONE ---");
            Console.ReadLine();
        }
    }
}
