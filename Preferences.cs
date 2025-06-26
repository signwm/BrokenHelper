using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrokenHelper.Models;

namespace BrokenHelper
{
    public static class Preferences
    {
        private static readonly string Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private static readonly string FilePath = Path.Combine(Dir, "preferences.cfg");

        private static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase);

        static Preferences()
        {
            Load();
        }

        private static void Load()
        {
            if (!File.Exists(FilePath))
                return;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('#') || string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                    Values[parts[0].Trim()] = parts[1].Trim();
            }
        }

        public static void Save()
        {
            Directory.CreateDirectory(Dir);
            var lines = Values.Select(kv => $"{kv.Key}={kv.Value}");
            File.WriteAllLines(FilePath, lines);
        }

        public static string? Get(string key)
        {
            Values.TryGetValue(key, out var value);
            return value;
        }

        public static void Set(string key, string value)
        {
            Values[key] = value;
        }

        public static int? GetInt(string key)
        {
            if (Values.TryGetValue(key, out var value) && int.TryParse(value, out var result))
                return result;
            return null;
        }

        public static void SetInt(string key, int value)
        {
            Values[key] = value.ToString();
        }

        public static string PlayerName
        {
            get
            {
                var name = Get("playername");
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
                name = "Unknown";
                return name;
            }
            set => Set("playername", value);
        }

        public static bool SoundSignals
        {
            get => Get("sound_signals") == "1";
            set => Set("sound_signals", value ? "1" : "0");
        }
    }
}

