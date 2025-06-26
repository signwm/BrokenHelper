using System;
using System.Collections.Generic;
using System.Linq;
using BrokenHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace BrokenHelper
{
    public record InstanceInfo(int Id, DateTime StartTime, string Name,
        int Difficulty, string Duration, int EarnedExp, int EarnedPsycho,
        int FoundGold, int DropValue, int FightCount);

    public record FightInfo(int Id, DateTime Time, List<string> Players,
        List<string> Opponents, int EarnedExp, int EarnedPsycho,
        int FoundGold, int DropValue, string Drops, int? InstanceId,
        string InstanceName)
    {
        public string PlayersText => string.Join(", ", Players);
        public string OpponentsText => string.Join(", ", Opponents);
    }

    public record DropSummary(string Name, int Quantity, int Value);

    public record FightsSummary(int EarnedExp, int EarnedPsycho, int FoundGold,
        int DropValue, int FightCount, List<DropSummary> Drops);

    public static class StatsService
    {
        public static string LastInstanceName { get; set; } = string.Empty;
        private static int GetDropValue(DropEntity drop,
            IReadOnlyDictionary<string, int> itemPrices,
            IReadOnlyDictionary<string, int> artifactPrices)
        {
            int price = drop.DropType switch
            {
                DropType.Item => itemPrices.TryGetValue(drop.Name, out var v) ? v : 0,
                DropType.Drif or DropType.Orb =>
                    artifactPrices.TryGetValue(drop.Name, out var an) ? an : 0,
                DropType.Equipment => drop.Value ?? 0,
                _ => 0
            };
            return price * drop.Quantity;
        }

        public static List<InstanceInfo> GetInstances(string playerName,
            DateTime from, DateTime to)
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var instances = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Players).ThenInclude(fp => fp.Player)
                .Include(i => i.Fights).ThenInclude(f => f.Players).ThenInclude(fp => fp.Drops)
                .Where(i => i.StartTime >= from && i.StartTime <= to)
                .OrderBy(i => i.StartTime)
                .ToList();

            var result = new List<InstanceInfo>();
            foreach (var instance in instances)
            {
                var fights = instance.Fights;
                var playerFights = fights.SelectMany(f => f.Players)
                    .Where(fp => fp.Player.Name == playerName)
                    .ToList();

                int exp = playerFights.Sum(fp => fp.Exp);
                int psycho = playerFights.Sum(fp => fp.Psycho);
                int gold = playerFights.Sum(fp => fp.Gold);
                int dropsValue = playerFights.SelectMany(fp => fp.Drops)
                    .Sum(d => GetDropValue(d, itemPrices, artifactPrices));

                string duration = instance.EndTime.HasValue
                    ? (instance.EndTime.Value - instance.StartTime).ToString(@"mm\:ss")
                    : (DateTime.Now - instance.StartTime).ToString(@"mm\:ss");

                result.Add(new InstanceInfo(
                    instance.Id,
                    instance.StartTime,
                    instance.Name,
                    instance.Difficulty,
                    duration,
                    exp,
                    psycho,
                    gold,
                    dropsValue,
                    fights.Count));
            }

            return result;
        }

        public static List<FightInfo> GetFights(string playerName,
            DateTime from, DateTime to, bool onlyWithoutInstance)
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var fightsQuery = context.Fights
                .Include(f => f.Instance)
                .Include(f => f.Players).ThenInclude(fp => fp.Player)
                .Include(f => f.Players).ThenInclude(fp => fp.Drops)
                .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
                .Where(f => f.EndTime >= from && f.EndTime <= to);

            if (onlyWithoutInstance)
            {
                fightsQuery = fightsQuery.Where(f => f.InstanceId == null);
            }

            var fights = fightsQuery.OrderBy(f => f.EndTime).ToList();

            var result = new List<FightInfo>();
            foreach (var fight in fights)
            {
                var players = fight.Players.Select(fp => fp.Player.Name).ToList();
                var opponents = fight.Opponents
                    .GroupBy(o => o.OpponentType.Name)
                    .Select(g => g.Count() > 1 ? $"{g.Key} ({g.Sum(o => o.Quantity)})" : g.Key)
                    .ToList();

                var my = fight.Players.FirstOrDefault(fp => fp.Player.Name == playerName);
                if (my == null)
                {
                    result.Add(new FightInfo(
                        fight.Id,
                        fight.EndTime,
                        players,
                        opponents,
                        0,
                        0,
                        0,
                        0,
                        string.Empty,
                        fight.InstanceId,
                        fight.Instance?.Name ?? string.Empty));
                    continue;
                }

                int dropsValue = my.Drops.Sum(d => GetDropValue(d, itemPrices, artifactPrices));
                string dropsText = string.Join(", ", my.Drops.Select(d =>
                {
                    var name = d.Name;
                    if (d.Quantity > 1) name += $" ({d.Quantity})";
                    return name;
                }));

                result.Add(new FightInfo(
                    fight.Id,
                    fight.EndTime,
                    players,
                    opponents,
                    my.Exp,
                    my.Psycho,
                    my.Gold,
                    dropsValue,
                    dropsText,
                    fight.InstanceId,
                    fight.Instance?.Name ?? string.Empty));
            }

            return result;
        }

        public static List<FightInfo> GetFights(string playerName, int instanceId)
        {
            using var context = new GameDbContext();
            var instance = context.Fights
                .Include(f => f.Instance)
                .Include(f => f.Players).ThenInclude(fp => fp.Player)
                .Include(f => f.Players).ThenInclude(fp => fp.Drops)
                .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
                .Where(f => f.InstanceId == instanceId)
                .OrderBy(f => f.EndTime)
                .ToList();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var result = new List<FightInfo>();
            foreach (var fight in instance)
            {
                var players = fight.Players.Select(fp => fp.Player.Name).ToList();
                var opponents = fight.Opponents
                    .GroupBy(o => o.OpponentType.Name)
                    .Select(g => g.Count() > 1 ? $"{g.Key} ({g.Sum(o => o.Quantity)})" : g.Key)
                    .ToList();

                var my = fight.Players.FirstOrDefault(fp => fp.Player.Name == playerName);
                if (my == null)
                {
                    result.Add(new FightInfo(
                        fight.Id,
                        fight.EndTime,
                        players,
                        opponents,
                        0,
                        0,
                        0,
                        0,
                        string.Empty,
                        fight.InstanceId,
                        fight.Instance?.Name ?? string.Empty));
                    continue;
                }

                int dropsValue = my.Drops.Sum(d => GetDropValue(d, itemPrices, artifactPrices));
                string dropsText = string.Join(", ", my.Drops.Select(d =>
                {
                    var name = d.Name;
                    if (d.Quantity > 1) name += $" ({d.Quantity})";
                    return name;
                }));

                result.Add(new FightInfo(
                    fight.Id,
                    fight.EndTime,
                    players,
                    opponents,
                    my.Exp,
                    my.Psycho,
                    my.Gold,
                    dropsValue,
                    dropsText,
                    fight.InstanceId,
                    fight.Instance?.Name ?? string.Empty));
            }

            return result;
        }

        public static FightsSummary SummarizeFights(string playerName, IEnumerable<int> fightIds)
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var players = context.FightPlayers
                .Include(fp => fp.Drops)
                .Where(fp => fightIds.Contains(fp.FightId) && fp.Player.Name == playerName)
                .ToList();

            int exp = players.Sum(p => p.Exp);
            int psycho = players.Sum(p => p.Psycho);
            int gold = players.Sum(p => p.Gold);
            int fightCount = players.Select(p => p.FightId).Distinct().Count();

            var dropsGrouped = players.SelectMany(p => p.Drops)
                .GroupBy(d => d.Name)
                .Select(g => new DropSummary(
                    g.Key,
                    g.Sum(d => d.Quantity),
                    g.Sum(d => GetDropValue(d, itemPrices, artifactPrices))))
                .ToList();

            int dropValue = dropsGrouped.Sum(d => d.Value);

            return new FightsSummary(exp, psycho, gold, dropValue, fightCount, dropsGrouped);
        }

        public static List<DropSummaryDetailed> GetDropDetails(string playerName, IEnumerable<int> fightIds)
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var drops = context.FightPlayers
                .Include(fp => fp.Drops)
                .Where(fp => fightIds.Contains(fp.FightId) && fp.Player.Name == playerName)
                .SelectMany(fp => fp.Drops)
                .ToList();

            var result = drops
                .Select(d =>
                {
                    int unitPrice = d.Quantity == 0 ? 0 : GetDropValue(d, itemPrices, artifactPrices) / d.Quantity;
                    string type = d.DropType switch
                    {
                        DropType.Equipment => "Equipment",
                        DropType.Item => "Item",
                        DropType.Drif or DropType.Orb => "Artifact",
                        _ => "Item"
                    };

                    return new DropSummaryDetailed
                    {
                        Name = AppendOrnamentLabel(d.Name, d.OrnamentCount),
                        Type = type,
                        Quantity = d.Quantity,
                        UnitPrice = unitPrice
                    };
                })
                .GroupBy(d => new { d.Name, d.Type, d.UnitPrice })
                .Select(g => new DropSummaryDetailed
                {
                    Name = g.Key.Name,
                    Type = g.Key.Type,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            return result;
        }

        public static string GetDefaultPlayerName()
        {
            return Preferences.PlayerName;
        }

        public static FightsSummary? GetLastFightSummary(string playerName)
        {
            using var context = new GameDbContext();

            var lastFightId = context.FightPlayers
                .Include(fp => fp.Fight)
                .Where(fp => fp.Player.Name == playerName)
                .OrderByDescending(fp => fp.Fight.EndTime)
                .Select(fp => fp.FightId)
                .FirstOrDefault();

            return lastFightId == 0 ? null : SummarizeFights(playerName, new[] { lastFightId });
        }

        public static InstanceInfo? GetCurrentOrLastInstance(string playerName)
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var instancesQuery = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Players).ThenInclude(fp => fp.Player)
                .Include(i => i.Fights).ThenInclude(f => f.Players).ThenInclude(fp => fp.Drops)
                .Where(i => i.Fights.Any(f => f.Players.Any(fp => fp.Player.Name == playerName)))
                .OrderByDescending(i => i.StartTime);

            var instance = instancesQuery.FirstOrDefault(i => i.EndTime == null) ?? instancesQuery.FirstOrDefault();
            if (instance == null)
                return null;

            var fights = instance.Fights;
            var playerFights = fights.SelectMany(f => f.Players)
                .Where(fp => fp.Player != null && fp.Player.Name == playerName)
                .ToList();
            if (playerFights.Count == 0)
                return null;

            int exp = playerFights.Sum(fp => fp.Exp);
            int psycho = playerFights.Sum(fp => fp.Psycho);
            int gold = playerFights.Sum(fp => fp.Gold);
            int dropsValue = playerFights.SelectMany(fp => fp.Drops)
                .Sum(d => GetDropValue(d, itemPrices, artifactPrices));

            string duration = instance.EndTime.HasValue
                ? (instance.EndTime.Value - instance.StartTime).ToString(@"mm\:ss")
                : (DateTime.Now - instance.StartTime).ToString(@"mm\:ss");

            return new InstanceInfo(
                instance.Id,
                instance.StartTime,
                instance.Name,
                instance.Difficulty,
                duration,
                exp,
                psycho,
                gold,
                dropsValue,
                fights.Count);
        }

        public static InstanceInfo? GetLastFinishedInstance(string playerName)
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var instance = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Players).ThenInclude(fp => fp.Player)
                .Include(i => i.Fights).ThenInclude(f => f.Players).ThenInclude(fp => fp.Drops)
                .Where(i => i.EndTime != null)
                .OrderByDescending(i => i.EndTime)
                .FirstOrDefault();

            if (instance == null)
                return null;

            var fights = instance.Fights;
            var playerFights = fights.SelectMany(f => f.Players)
                .Where(fp => fp.Player.Name == playerName)
                .ToList();
            if (playerFights.Count == 0)
                return null;

            int exp = playerFights.Sum(fp => fp.Exp);
            int psycho = playerFights.Sum(fp => fp.Psycho);
            int gold = playerFights.Sum(fp => fp.Gold);
            int dropsValue = playerFights.SelectMany(fp => fp.Drops)
                .Sum(d => GetDropValue(d, itemPrices, artifactPrices));

            string duration = (instance.EndTime!.Value - instance.StartTime).ToString(@"mm\:ss");

            return new InstanceInfo(
                instance.Id,
                instance.StartTime,
                instance.Name,
                instance.Difficulty,
                duration,
                exp,
                psycho,
                gold,
                dropsValue,
                fights.Count);
        }

        public static void CreateInstance(string name, int difficulty,
            DateTime startTime, DateTime? endTime, IEnumerable<int> fightIds)
        {
            using var context = new GameDbContext();

            string publicId = "999" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var instance = new InstanceEntity
            {
                PublicId = publicId,
                Name = name,
                Difficulty = difficulty,
                StartTime = startTime,
                EndTime = endTime
            };

            context.Instances.Add(instance);
            context.SaveChanges();

            var fights = context.Fights.Where(f => fightIds.Contains(f.Id)).ToList();
            foreach (var fight in fights)
            {
                fight.InstanceId = instance.Id;
            }

            context.SaveChanges();
        }

        public static void DeleteFights(IEnumerable<int> fightIds)
        {
            using var context = new GameDbContext();

            var fights = context.Fights
                .Include(f => f.Players).ThenInclude(fp => fp.Drops)
                .Include(f => f.Opponents)
                .Where(f => fightIds.Contains(f.Id))
                .ToList();

            foreach (var fight in fights)
            {
                context.Drops.RemoveRange(fight.Players.SelectMany(p => p.Drops));
                context.FightPlayers.RemoveRange(fight.Players);
                context.FightOpponents.RemoveRange(fight.Opponents);
                context.Fights.Remove(fight);
            }

            context.SaveChanges();
        }

        public static void DeleteInstances(IEnumerable<int> instanceIds)
        {
            using var context = new GameDbContext();

            var instances = context.Instances
                .Include(i => i.Fights)
                .Where(i => instanceIds.Contains(i.Id))
                .ToList();

            foreach (var instance in instances)
            {
                foreach (var fight in instance.Fights)
                {
                    fight.InstanceId = null;
                }
                context.Instances.Remove(instance);
            }

            context.SaveChanges();
        }

        public static TimeSpan GetInstanceDuration(int instanceId)
        {
            using var context = new GameDbContext();
            var instance = context.Instances
                .FirstOrDefault(i => i.Id == instanceId);
            if (instance == null)
                return TimeSpan.Zero;

            var end = instance.EndTime ?? DateTime.Now;
            return end - instance.StartTime;
        }

        private static readonly int[,] QuoteItemCoefficients = new int[,]
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

        private static readonly string[] OrnamentLabels =
        {
            "[B1]",
            "[B2]",
            "[B3]",
            "[S1]",
            "[S2]",
            "[S3]",
            "[G1]",
            "[G2]",
            "[G3]"
        };

        private static string AppendOrnamentLabel(string name, int? ornamentCount)
        {
            if (!name.Contains('"') || ornamentCount == null)
                return name;

            int count = ornamentCount.Value;
            if (count < 0 || count >= OrnamentLabels.Length)
                return name;

            return $"{name} {OrnamentLabels[count]}";
        }

        public static void RecalculateDropPrices()
        {
            using var context = new GameDbContext();

            var itemPrices = context.ItemPrices.ToDictionary(p => p.Name, p => p.Value);
            var artifactPrices = context.ArtifactPrices.ToDictionary(p => p.Name, p => p.Value);

            var shardPrice = itemPrices.TryGetValue("Odłamek", out var sp) ? sp : 0;
            var essencePrice = itemPrices.TryGetValue("Esencja", out var ep) ? ep : 0;

            var drops = context.Drops.ToList();
            foreach (var drop in drops)
            {
                switch (drop.DropType)
                {
                    case DropType.Item:
                        drop.Value = itemPrices.TryGetValue(drop.Name, out var iv) ? iv : 0;
                        break;
                    case DropType.Drif:
                    case DropType.Orb:
                        drop.Value = artifactPrices.TryGetValue(drop.Name, out var av) ? av : 0;
                        break;
                    case DropType.Equipment:
                        drop.Value = CalculateEquipmentValue(drop, shardPrice, essencePrice);
                        break;
                }
            }

            context.SaveChanges();
        }

        private static int CalculateEquipmentValue(DropEntity drop, int shardPrice, int essencePrice)
        {
            double multiplier = drop.Name.Count(c => c == ' ') > 2 ? 0.025 : 0.3;
            int baseValue = drop.Value ?? 0;
            int result = (int)Math.Round(baseValue * multiplier);

            if (drop.Name.Contains('"'))
            {
                if (drop.OrnamentCount.HasValue && drop.Rank.HasValue &&
                    drop.OrnamentCount.Value >= 0 && drop.OrnamentCount.Value < QuoteItemCoefficients.GetLength(0) &&
                    drop.Rank.Value >= 1 && drop.Rank.Value <= QuoteItemCoefficients.GetLength(1))
                {
                    int coef = QuoteItemCoefficients[drop.OrnamentCount.Value, drop.Rank.Value - 1];
                    var basePrice = drop.Rank.Value >= 7 ? shardPrice : essencePrice;
                    result = coef * basePrice;
                }
            }
            else if (drop.Name.Contains("Smoków"))
            {
                result = 12 * shardPrice;
            }
            else if (drop.Name.Contains("Vorlingów") || drop.Name.Contains("Lodu"))
            {
                result = 30 * shardPrice;
            }
            else if (drop.Name.Contains("Władców"))
            {
                result = 150 * shardPrice;
            }
            else if (drop.Name.Contains("Dawnych Orków"))
            {
                result = 60 * shardPrice;
            }

            return result;
        }
    }
}
