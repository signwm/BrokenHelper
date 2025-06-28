using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrokenHelper
{
    internal static class GameConfig
    {
        public static string[][] BossGroups { get; private set; } = [];
        public static Dictionary<string, int> MultiKillBosses { get; private set; } = [];
        public static HashSet<string> SingleBosses { get; private set; } = [];
        public static int[,] QuoteItemCoefficients { get; private set; } = new int[0,0];
        public static HashSet<string> DeathEndInstances { get; private set; } = [];

        static GameConfig()
        {
            Load();
        }

        private class ConfigModel
        {
            public string[][] BossGroups { get; set; } = [];
            public Dictionary<string,int> MultiKillBosses { get; set; } = [];
            public string[] SingleBosses { get; set; } = [];
            public int[][] QuoteItemCoefficients { get; set; } = [];
            public string[] DeathEndInstances { get; set; } = [];
        }

        private static void Load()
        {
            Directory.CreateDirectory("data");
            var path = Path.Combine("data", "config.json");
            if (!File.Exists(path))
                return;
            var json = File.ReadAllText(path);
            ConfigModel cfg;
            try
            {
                cfg = JsonSerializer.Deserialize<ConfigModel>(json) ?? new ConfigModel();
            }
            catch (Exception ex)
            {
                Logger.Add("GameConfig", $"Failed to deserialize config: {ex.Message}", DateTime.Now);
                cfg = new ConfigModel();
            }
            BossGroups = cfg.BossGroups ?? [];
            MultiKillBosses = cfg.MultiKillBosses ?? [];
            SingleBosses = cfg.SingleBosses != null ? new HashSet<string>(cfg.SingleBosses) : [];
            if (cfg.QuoteItemCoefficients != null && cfg.QuoteItemCoefficients.Length > 0)
            {
                int rows = cfg.QuoteItemCoefficients.Length;
                int cols = cfg.QuoteItemCoefficients[0].Length;
                var matrix = new int[rows, cols];
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < cols && j < cfg.QuoteItemCoefficients[i].Length; j++)
                        matrix[i, j] = cfg.QuoteItemCoefficients[i][j];
                QuoteItemCoefficients = matrix;
            }
            DeathEndInstances = cfg.DeathEndInstances != null ? new HashSet<string>(cfg.DeathEndInstances) : [];
        }
    }
}
