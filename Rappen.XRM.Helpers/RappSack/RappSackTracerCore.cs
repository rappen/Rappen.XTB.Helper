using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Rappen.XRM.Helpers.RappSack
{
    public abstract class RappSackTracerCore
    {
        private TraceTiming timing = TraceTiming.ElapsedSinceLast;
        private DateTime timeStart = DateTime.Now;
        private DateTime timeLast = DateTime.MinValue;
        private List<string> blocks = new List<string>();

        public TraceTiming Timing
        {
            get => timing;
            set => timing = value;
        }

        public RappSackTracerCore(TraceTiming timing)
        {
            this.timing = timing;
        }

        public void Trace(string message, TraceLevel level = TraceLevel.Information) => InternalTrace(message, GetTime(), blocks.Count, level);

        public void Trace(Exception exception)
        {
            var excstr = exception.ToString().Trim(Environment.NewLine.ToCharArray());
            if (!excstr.Contains(exception.Message))
            {
                excstr += Environment.NewLine + exception.Message;
            }
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                excstr += Environment.NewLine + exception.StackTrace;
            }
            InternalTrace($"\n*** Error: {exception.GetType()} ***\n{excstr}\n", "", 0, TraceLevel.Error);
        }

        public void TraceIn(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                name = GetOrigin()?.Name ?? $"Block {blocks.Count + 1}";
            }
            Trace($"\\ {name}");
            blocks.Add(name);
        }

        public void TraceOut()
        {
            if (blocks.Count == 0)
            {
                Trace("Exiting unknown block");
                return;
            }
            var name = blocks.Last();
            blocks.RemoveAt(blocks.Count - 1);
            Trace($"/ {name}");
        }

        protected abstract void InternalTrace(string message, string timestamp, int indent, TraceLevel level = TraceLevel.Information);

        private string GetTime()
        {
            if (timing == TraceTiming.None)
            {
                return string.Empty;
            }
            if (timeLast == DateTime.MinValue)
            {
                timeLast = DateTime.Now;
                return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ";
            }
            switch (timing)
            {
                case TraceTiming.ElapsedSinceStart:
                    return $"{(DateTime.Now - timeStart).TotalMilliseconds:0000} ";

                case TraceTiming.ElapsedSinceLast:
                    var time = DateTime.Now;
                    var result = $"{(time - timeLast).TotalMilliseconds:0000} ";
                    timeLast = time;
                    return result;

                case TraceTiming.CurrentTime:
                    return $"{DateTime.Now:HH:mm:ss.fff} ";

                default:
                    return string.Empty;
            }
        }

        private MethodBase GetOrigin()
        {
            var stackFrames = new StackTrace(true).GetFrames();
            if (stackFrames == null || stackFrames.Count() == 0)
            {
                return null;
            }
            return stackFrames
                .Select(s => s.GetMethod()).FirstOrDefault(m =>
                m != null &&
                !m.IsVirtual &&
                !m.ReflectedType.FullName.StartsWith("Rappen.XRM.Helpers"));
        }
    }

    public enum TraceTiming
    {
        None,
        ElapsedSinceStart,
        ElapsedSinceLast,
        CurrentTime
    }

    public enum TraceLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical,
        None
    }
}