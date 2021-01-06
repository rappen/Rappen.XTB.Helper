using Rappen.XTB.Helpers.Interfaces;
using System;

namespace Rappen.XTB.Helpers
{
    public class VoidLogger : ILogger
    {
        public void EndSection() { }

        public void Log(string message) { }

        public void Log(Exception ex) { }

        public void StartSection(string name = null) { }
    }
}
