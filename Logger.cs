using System;
using System.Collections.Generic;
using System.Linq;

namespace BrokenHelper
{
    internal record LogEntry(DateTime Time, string Prefix, string Message);

    internal static class Logger
    {
        private static readonly List<LogEntry> _entries = [];
        public static event Action<LogEntry>? LogAdded;

        public static IReadOnlyList<LogEntry> Entries
        {
            get { lock (_entries) { return _entries.ToList(); } }
        }

        public static void Add(string prefix, string message, DateTime time)
        {
            var entry = new LogEntry(time, prefix, message);
            lock (_entries)
            {
                _entries.Add(entry);
            }
            LogAdded?.Invoke(entry);
        }
    }
}
