using System;
using System.Collections.Generic;
using System.Linq;
using BrokenHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace BrokenHelper
{
    public record InstanceInfo(int Id, DateTime StartTime, DateTime? EndTime, string Name,
        int Difficulty, string Duration, int EarnedExp, int EarnedPsycho,
        int FoundGold, int DropValue, int FightCount);

    public record FightInfo(int Id, DateTime StartTime, DateTime EndTime,
        List<string> Opponents, int EarnedExp, int EarnedPsycho,
        int FoundGold, int DropValue, string Drops, int? InstanceId,
        string InstanceName)
    {
        public string OpponentsText => string.Join(", ", Opponents);
    }

    public record FightsSummary(int EarnedExp, int EarnedPsycho, int FoundGold,
        int DropValue, int FightCount);

    public static class StatsService
    {
        public static string LastInstanceName { get; set; } = string.Empty;
        private static int GetDropValue(DropEntity drop)
        {
            int price = drop.Item.Value ?? 0;
            return price * drop.Quantity;
        }

        public static List<InstanceInfo> GetInstances(string playerName,
            DateTime from, DateTime to)
        {
            using var context = new GameDbContext();

            var instances = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Drops).ThenInclude(d => d.Item)
                .Where(i => i.StartTime >= from && i.StartTime <= to)
                .OrderByDescending(i => i.StartTime)
                .ToList();

            var result = new List<InstanceInfo>();
            foreach (var instance in instances)
            {
                var fights = instance.Fights.Where(f => f.PlayerName == playerName).ToList();

                int exp = fights.Sum(f => f.Exp);
                int psycho = fights.Sum(f => f.Psycho);
                int gold = fights.Sum(f => f.Gold);
                int dropsValue = fights.SelectMany(f => f.Drops)
                    .Sum(GetDropValue);

                string duration = instance.EndTime.HasValue
                    ? (instance.EndTime.Value - instance.StartTime).ToString(@"mm\:ss")
                    : (DateTime.Now - instance.StartTime).ToString(@"mm\:ss");

                result.Add(new InstanceInfo(
                    instance.Id,
                    instance.StartTime,
                    instance.EndTime,
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

            var fightsQuery = context.Fights
                .Include(f => f.Instance)
                .Include(f => f.Drops).ThenInclude(d => d.Item)
                .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
                .Where(f => f.PlayerName == playerName && f.StartTime >= from && f.EndTime <= to);

            if (onlyWithoutInstance)
            {
                fightsQuery = fightsQuery.Where(f => f.InstanceId == null);
            }

            var fights = fightsQuery
                .OrderByDescending(f => f.EndTime ?? f.StartTime)
                .ToList();

            var result = new List<FightInfo>();
            foreach (var fight in fights)
            {
                var opponents = fight.Opponents
                    .GroupBy(o => o.OpponentType.Name)
                    .Select(g => g.Count() > 1 ? $"{g.Key} ({g.Sum(o => o.Quantity)})" : g.Key)
                    .ToList();

                int dropsValue = fight.Drops.Sum(GetDropValue);
                string dropsText = string.Join(", ", fight.Drops.Select(d =>
                {
                    var name = d.Item.Name;
                    if (d.Quantity > 1) name += $" ({d.Quantity})";
                    return name;
                }));

                result.Add(new FightInfo(
                    fight.Id,
                    fight.StartTime,
                    fight.EndTime ?? fight.StartTime,
                    opponents,
                    fight.Exp,
                    fight.Psycho,
                    fight.Gold,
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
                .Include(f => f.Drops).ThenInclude(d => d.Item)
                .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
                .Where(f => f.InstanceId == instanceId && f.PlayerName == playerName)
                .OrderByDescending(f => f.StartTime)
                .ToList();

            var result = new List<FightInfo>();
            foreach (var fight in instance)
            {
                var opponents = fight.Opponents
                    .GroupBy(o => o.OpponentType.Name)
                    .Select(g => g.Count() > 1 ? $"{g.Key} ({g.Sum(o => o.Quantity)})" : g.Key)
                    .ToList();

                int dropsValue = fight.Drops.Sum(GetDropValue);
                string dropsText = string.Join(", ", fight.Drops.Select(d =>
                {
                    var name = d.Item.Name;
                    if (d.Quantity > 1) name += $" ({d.Quantity})";
                    return name;
                }));

                result.Add(new FightInfo(
                    fight.Id,
                    fight.StartTime,
                    fight.EndTime ?? fight.StartTime,
                    opponents,
                    fight.Exp,
                    fight.Psycho,
                    fight.Gold,
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

            var fights = context.Fights
                .Include(f => f.Drops).ThenInclude(d => d.Item)
                .Where(f => fightIds.Contains(f.Id) && f.PlayerName == playerName)
                .ToList();

            int exp = fights.Sum(f => f.Exp);
            int psycho = fights.Sum(f => f.Psycho);
            int gold = fights.Sum(f => f.Gold);
            int fightCount = fights.Count;

            int dropValue = fights.SelectMany(f => f.Drops)
                .Sum(GetDropValue);

            return new FightsSummary(exp, psycho, gold, dropValue, fightCount);
        }

        public static List<DropSummaryDetailed> GetDropDetails(string playerName, IEnumerable<int> fightIds)
        {
            using var context = new GameDbContext();

            var drops = context.Fights
                .Include(f => f.Drops).ThenInclude(d => d.Item)
                .Where(f => fightIds.Contains(f.Id) && f.PlayerName == playerName)
                .SelectMany(f => f.Drops)
                .ToList();

            var result = drops
                .Select(d =>
                {
                    int unitPrice = d.Quantity == 0 ? 0 : GetDropValue(d) / d.Quantity;
                    string type = d.Item.DropType switch
                    {
                        DropType.Rar or DropType.Syng or DropType.Trash => "Equipment",
                        DropType.Item => "Item",
                        DropType.Drif or DropType.Orb => "Artifact",
                        _ => "Item"
                    };

                    return new DropSummaryDetailed
                    {
                        Name = AppendOrnamentLabel(d.Item.Name, d.OrnamentCount),
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

        public static List<DropSummaryDetailed> GetLastFightDropDetails(string playerName)
        {
            using var context = new GameDbContext();

            var lastFightId = context.Fights
                .Where(f => f.PlayerName == playerName)
                .OrderByDescending(f => f.StartTime)
                .Select(f => f.Id)
                .FirstOrDefault();

            return lastFightId == 0
                ? []
                : GetDropDetails(playerName, [lastFightId]);
        }

        public static List<DropSummaryDetailed> GetCurrentOrLastInstanceDropDetails(string playerName)
        {
            using var context = new GameDbContext();

            var instanceQuery = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Drops).ThenInclude(d => d.Item)
                .Where(i => i.Fights.Any(f => f.PlayerName == playerName))
                .OrderByDescending(i => i.StartTime);

            var instance = instanceQuery.FirstOrDefault(i => i.EndTime == null) ?? instanceQuery.FirstOrDefault();
            if (instance == null)
                return [];

            var fightIds = instance.Fights.Where(f => f.PlayerName == playerName).Select(f => f.Id).ToList();
            return GetDropDetails(playerName, fightIds);
        }

        public static string GetDefaultPlayerName()
        {
            return Preferences.PlayerName;
        }

        public static FightsSummary? GetLastFightSummary(string playerName)
        {
            using var context = new GameDbContext();

            var lastFightId = context.Fights
                .Where(f => f.PlayerName == playerName)
                .OrderByDescending(f => f.StartTime)
                .Select(f => f.Id)
                .FirstOrDefault();

            return lastFightId == 0 ? null : SummarizeFights(playerName, [lastFightId]);
        }

        public static InstanceInfo? GetCurrentOrLastInstance(string playerName)
        {
            using var context = new GameDbContext();

            var instancesQuery = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Drops).ThenInclude(d => d.Item)
                .Where(i => i.Fights.Any(f => f.PlayerName == playerName))
                .OrderByDescending(i => i.StartTime);

            var instance = instancesQuery.FirstOrDefault(i => i.EndTime == null) ?? instancesQuery.FirstOrDefault();
            if (instance == null)
                return null;

            var fights = instance.Fights.Where(f => f.PlayerName == playerName).ToList();
            if (fights.Count == 0)
                return null;

            int exp = fights.Sum(f => f.Exp);
            int psycho = fights.Sum(f => f.Psycho);
            int gold = fights.Sum(f => f.Gold);
            int dropsValue = fights.SelectMany(f => f.Drops)
                .Sum(GetDropValue);

            string duration = instance.EndTime.HasValue
                ? (instance.EndTime.Value - instance.StartTime).ToString(@"mm\:ss")
                : (DateTime.Now - instance.StartTime).ToString(@"mm\:ss");

            return new InstanceInfo(
                instance.Id,
                instance.StartTime,
                instance.EndTime,
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

            var instance = context.Instances
                .Include(i => i.Fights).ThenInclude(f => f.Drops).ThenInclude(d => d.Item)
                .Where(i => i.EndTime != null && i.Fights.Any(f => f.PlayerName == playerName))
                .OrderByDescending(i => i.EndTime)
                .FirstOrDefault();

            if (instance == null)
                return null;

            var fights = instance.Fights.Where(f => f.PlayerName == playerName).ToList();
            if (fights.Count == 0)
                return null;

            int exp = fights.Sum(f => f.Exp);
            int psycho = fights.Sum(f => f.Psycho);
            int gold = fights.Sum(f => f.Gold);
            int dropsValue = fights.SelectMany(f => f.Drops)
                .Sum(GetDropValue);

            string duration = (instance.EndTime!.Value - instance.StartTime).ToString(@"mm\:ss");

            return new InstanceInfo(
                instance.Id,
                instance.StartTime,
                instance.EndTime,
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
                .Include(f => f.Drops)
                .Include(f => f.Opponents)
                .Where(f => fightIds.Contains(f.Id))
                .ToList();

            foreach (var fight in fights)
            {
                context.Drops.RemoveRange(fight.Drops);
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

        private static readonly string[] OrnamentLabels =
        [
            "[B1]",
            "[B2]",
            "[B3]",
            "[S1]",
            "[S2]",
            "[S3]",
            "[G1]",
            "[G2]",
            "[G3]"
        ];

        private static string AppendOrnamentLabel(string name, int? ornamentCount)
        {
            if (!name.Contains('"') || ornamentCount == null)
                return name;

            int count = ornamentCount.Value;
            if (count < 0 || count >= OrnamentLabels.Length)
                return name;

            return $"{name} {OrnamentLabels[count]}";
        }
    }
}
