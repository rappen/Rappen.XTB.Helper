﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rappen.XRM.RappSack
{
    public abstract class RappSackTracerCore
    {
        private TraceTiming timing = TraceTiming.ElapsedSinceLast;
        private DateTime timeStart = DateTime.Now;
        private DateTime timeLast = DateTime.Now;
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

        public void Trace(string message, TraceLevel level = TraceLevel.Information) => TraceInternal(message, GetTime(), blocks.Count, level);

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
            TraceInternal($"\n*** Error: {exception.GetType()} ***\n{excstr}\n", "", 0, TraceLevel.Error);
        }

        public void TraceRaw(string message, TraceLevel level = TraceLevel.Information) => TraceInternal(message, string.Empty, 0, level);

        public void TraceIn(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                name = CallerMethodName() ?? $"Block {blocks.Count + 1}";
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

        internal string CallerMethodName()
        {
            var stackFrames = new StackTrace(true).GetFrames();
            if (stackFrames != null && stackFrames.Count() > 0)
            {
                var caller = stackFrames
                    .Select(s => s.GetMethod()).FirstOrDefault(m =>
                    m != null &&
                    !m.IsVirtual &&
                    m.ReflectedType != null &&
                    m.ReflectedType.FullName != null &&
                    !m.ReflectedType.FullName.StartsWith("System") &&
                    !m.ReflectedType.FullName.StartsWith("Microsoft") &&
                    !m.ReflectedType.FullName.StartsWith("Rappen.XRM.RappSack"));
                if (caller != null)
                {
                    if (caller.IsConstructor && caller.ReflectedType != null)
                    {
                        return caller.ReflectedType.Name;
                    }
                    if (!string.IsNullOrWhiteSpace(caller.Name))
                    {
                        return caller.Name;
                    }
                }
            }
            return null;
        }

        protected abstract void TraceInternal(string message, string timestamp, int indent, TraceLevel level = TraceLevel.Information);

        private string GetTime()
        {
            var timePrev = timeLast;
            timeLast = DateTime.Now;
            if (timePrev == DateTime.MinValue)
            {
                return $"{timeStart:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}";
            }
            switch (timing)
            {
                case TraceTiming.ElapsedSinceStart:
                    return $"{string.Format("{0,5}", (int)(timeLast - timeStart).TotalMilliseconds)} ";

                case TraceTiming.ElapsedSinceLast:
                    return $"{string.Format("{0,5}", (int)(timeLast - timePrev).TotalMilliseconds)} ";

                case TraceTiming.CurrentTime:
                    return $"{DateTime.Now:HH:mm:ss.fff} ";

                default:
                    return string.Empty;
            }
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