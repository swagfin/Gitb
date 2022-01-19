using System;

namespace Gitb
{
    public static class ConsoleX
    {
        public static void WriteLine(string message, ConsoleColor consoleColor = ConsoleColor.White)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static bool ReadLineYesNoConfirmed()
        {
            string response = Console.ReadLine();
            if (string.IsNullOrEmpty(response))
                return false;
            return response.Trim().ToLower() == "y" || response.Trim() == "1";
        }
    }
}
