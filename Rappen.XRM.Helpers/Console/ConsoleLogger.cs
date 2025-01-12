using Rappen.XRM.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Helpers.Console
{
    public class ConsoleLogger : ILogger
    {
        private readonly string logpath;
        private int section;

        public ConsoleLogger(string workingfolder)
        {
            var logfile = $"ConsoleLogger_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            logpath = System.IO.Path.Combine(workingfolder, logfile);
            section = 0;
            Write($"Created log file: {logpath}", true);
        }

        public void EndSection()
        {
            if (section > 0)
            {
                section--;
            }
            Write("/");
        }

        public void Log(string message)
        {
            Write(message);
        }

        public void Log(Exception ex)
        {
            Write(ex.Message);
        }

        public void StartSection(string name = null)
        {
            Write("\\" + (name ?? ""));
            section++;
        }

        private void Write(string message, bool raw = false)
        {
            if (!raw)
            {
                message = $"{DateTime.Now:HH:mm:ss.fff}{new string(' ', 1 + section * 2)}{message}";
            }
            System.Console.WriteLine(message);
            try
            {
                System.IO.File.AppendAllText(logpath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error writing to {logpath}:{Environment.NewLine}{ex.Message}");
            }
        }
    }
}