using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BrokenHelper.Models;

namespace BrokenHelper
{
    public static class Preferences
    {
        private static readonly string Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private static readonly string FilePath = Path.Combine(Dir, "config.cfg");

        private static readonly string[][] DefaultBossGroups = new[]
        {
            new[] { "Duch Ognia", "Duch Energii", "Duch Zimna" },
            new[] { "Babadek", "Gregorius", "Ghadira" },
            new[] { "Mahet", "Tarul" },
            new[] { "Lugus", "Morana" },
            new[] { "Fyodor", "Gmo" }
        };

        private static readonly Dictionary<string, int> DefaultMultiKillBosses = new()
        {
            { "Konstrukt", 3 },
            { "Osłabiony Konstrukt", 3 }
        };

        private static readonly string[] DefaultSingleBosses = new[]
        {
            "Admirał Utoru", "Angwalf-Htaga", "Aqua Regis", "Bibliotekarz",
            "Draugul", "Duch Zamku", "Garthmog", "Geomorph", "Herszt",
            "Heurokratos", "Hvar", "Ichtion", "Ivravul", "Jaskółka",
            "Jastrzębior", "Krzyżak", "Modliszka", "Mortus", "Nidhogg",
            "Niedźwiedź", "Obserwator", "Ropucha", "Selena", "Sidraga",
            "Tygrys", "Utor Komandor", "Valdarog", "Vidvar", "Vough",
            "Wendigo", "Władca Marionetek"
        };

        private static readonly int[,] DefaultQuoteItemCoefficients = new int[,]
        {
            { 4, 4, 4, 20, 20, 20, 67, 67, 67, 351, 351, 351 },
            { 7, 7, 7, 27, 27, 27, 90, 90, 90, 585, 585, 585 },
            { 10, 10, 10, 54, 54, 54, 180, 180, 180, 1170, 1170, 1170 },
            { 16, 16, 16, 81, 81, 81, 270, 270, 270, 1755, 1755, 1755 },
            { 27, 27, 27, 135, 135, 135, 450, 450, 450, 2925, 2925, 2925 },
            { 40, 40, 40, 202, 202, 202, 675, 675, 675, 4680, 4680, 4680 },
            { 67, 67, 67, 337, 337, 337, 1125, 1125, 1125, 7605, 7605, 7605 },
            { 135, 135, 135, 675, 675, 675, 2250, 2250, 2250, 14625, 14625, 14625 },
            { 337, 337, 337, 1687, 1687, 1687, 5850, 5850, 5850, 35100, 35100, 35100 }
        };

        public static string[][] BossGroups { get; private set; } = DefaultBossGroups;
        public static Dictionary<string, int> MultiKillBosses { get; private set; } = new(DefaultMultiKillBosses);
        public static HashSet<string> SingleBosses { get; private set; } = new(DefaultSingleBosses);
        public static int[,] QuoteItemCoefficients { get; private set; } = DefaultQuoteItemCoefficients;

        private static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase);

        private class ConfigFile
        {
            public Dictionary<string, string>? Values { get; set; }
            public string[][]? BossGroups { get; set; }
            public Dictionary<string, int>? MultiKillBosses { get; set; }
            public string[]? SingleBosses { get; set; }
            public int[][]? QuoteItemCoefficients { get; set; }
        }

        static Preferences()
        {
            Load();
        }

        private static void Load()
        {
            if (!File.Exists(FilePath))
                return;

            try
            {
                var json = File.ReadAllText(FilePath);
                var cfg = System.Text.Json.JsonSerializer.Deserialize<ConfigFile>(json);
                if (cfg != null)
                {
                    if (cfg.Values != null)
                    {
                        foreach (var kv in cfg.Values)
                            Values[kv.Key] = kv.Value;
                    }
                    if (cfg.BossGroups != null)
                        BossGroups = cfg.BossGroups;
                    if (cfg.MultiKillBosses != null)
                        MultiKillBosses = new Dictionary<string, int>(cfg.MultiKillBosses);
                    if (cfg.SingleBosses != null)
                        SingleBosses = new HashSet<string>(cfg.SingleBosses);
                    if (cfg.QuoteItemCoefficients != null)
                        QuoteItemCoefficients = ToRect(cfg.QuoteItemCoefficients);
                }
            }
            catch
            {
                // ignore invalid config
            }
        }

        public static void Save()
        {
            Directory.CreateDirectory(Dir);
            var cfg = new ConfigFile
            {
                Values = Values,
                BossGroups = BossGroups,
                MultiKillBosses = MultiKillBosses,
                SingleBosses = SingleBosses.ToArray(),
                QuoteItemCoefficients = ToJagged(QuoteItemCoefficients)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(cfg,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
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

                using var ctx = new GameDbContext();
                name = ctx.Players.FirstOrDefault()?.Name ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name))
                    Set("playername", name);
                return name;
            }
            set => Set("playername", value);
        }

        public static bool SoundSignals
        {
            get => Get("sound_signals") == "1";
            set => Set("sound_signals", value ? "1" : "0");
        }

        private static int[][] ToJagged(int[,] rect)
        {
            var rows = rect.GetLength(0);
            var cols = rect.GetLength(1);
            var result = new int[rows][];
            for (int i = 0; i < rows; i++)
            {
                result[i] = new int[cols];
                for (int j = 0; j < cols; j++)
                    result[i][j] = rect[i, j];
            }
            return result;
        }

        private static int[,] ToRect(int[][] jagged)
        {
            var rows = jagged.Length;
            var cols = rows > 0 ? jagged[0].Length : 0;
            var result = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    result[i, j] = jagged[i][j];
            }
            return result;
        }
    }
}

