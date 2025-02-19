using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Rappen.XRM.Helpers.Console
{
    public class ConsoleLogger : ILogger
    {
        private readonly string logpath;
        private int section;

        public ConsoleLogger(string workingfolder)
        {
            if (!System.IO.Directory.Exists(workingfolder))
            {
                System.IO.Directory.CreateDirectory(workingfolder);
            }
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
            if (string.IsNullOrEmpty(name))
            {
                name = GetOrigin()?.Name ?? $"Section {section + 1}";
            }
            Write($"\\ {name}");
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

        private MethodBase GetOrigin()
        {
            StackFrame[] stackFrames = new StackTrace(true).GetFrames();
            return stackFrames.Select(s => s.GetMethod()).FirstOrDefault(m =>
                !m.IsVirtual &&
                !m.ReflectedType.FullName.StartsWith("Rappen.XRM.Helpers") &&
                !m.ReflectedType.FullName.StartsWith("xxxxxxxxxxxxx"));
        }
    }
}