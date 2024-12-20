using Rappen.XRM.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Helpers.Console
{
    public class ConsoleLogger : ILogger
    {
        public void EndSection()
        {
            System.Console.WriteLine("/");
        }

        public void Log(string message)
        {
            System.Console.WriteLine(message);
        }

        public void Log(Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }

        public void StartSection(string name = null)
        {
            System.Console.WriteLine("\\" + (name ?? ""));
        }
    }
}