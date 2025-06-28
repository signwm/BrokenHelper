using System;
using System.Collections.Generic;
using System.Linq;

namespace BrokenHelper.PacketHandlers
{
    internal partial class FightHandler
    {
        private readonly InstanceHandler _instanceHandler;
        private int? _currentFightId;

        public FightHandler(InstanceHandler instanceHandler)
        {
            _instanceHandler = instanceHandler;
        }

        public void HandleFightStart(DateTime? time = null)
        {
            using var context = new Models.GameDbContext();

            var startTime = time ?? DateTime.Now;

            var instance = context.Instances
                .FirstOrDefault(i => i.StartTime <= startTime &&
                    (i.EndTime == null || startTime <= i.EndTime));

            if (instance != null && instance.EndTime == null)
            {
                typeof(InstanceHandler)
                    .GetField("_currentInstanceId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .SetValue(_instanceHandler, instance.Id);
            }

            var fight = new Models.FightEntity
            {
                StartTime = startTime,
                InstanceId = instance?.Id
            };

            context.Fights.Add(fight);
            context.SaveChanges();

            _currentFightId = fight.Id;

            GameEvents.OnFightStarted();
        }

        public void HandleFightSummary(string message, DateTime? time = null)
        {
            using var context = new Models.GameDbContext();
            var fight = EnsureFightExists(time, context);
            var fightTime = time ?? DateTime.Now;

            var entries = message.Split("[--]", StringSplitOptions.None);
            var opponentNames = new List<string>();
            foreach (var entry in entries)
            {
                var fields = entry.Split('&');
                if (fields.Length == 0)
                    continue;

                if (fields[0] == "1")
                {
                    var name = fields.ElementAtOrDefault(1) ?? string.Empty;
                    if (string.Equals(name, Preferences.PlayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleFightPlayer(fields, fight, context);
                    }
                }
                else if (fields[0] == "2")
                {
                    var name = fields.ElementAtOrDefault(1) ?? string.Empty;
                    opponentNames.Add(name);
                    HandleFightOpponent(fields, fight, context);
                }
            }

            context.SaveChanges();
            _instanceHandler.CheckInstanceCompletion(opponentNames, fightTime, context);

            GameEvents.OnFightSummary();
        }

        public void HandleFightEnd(DateTime? time = null)
        {
            using var context = new Models.GameDbContext();
            var fight = EnsureFightExists(time, context);

            fight.EndTime = time ?? DateTime.Now;
            context.SaveChanges();

            _instanceHandler.CloseIfPending(fight.EndTime!.Value, context);

            _currentFightId = null;

            GameEvents.OnFightEnded();
        }

        private Models.FightEntity EnsureFightExists(DateTime? time, Models.GameDbContext context)
        {
            if (_currentFightId != null)
            {
                var existing = context.Fights.FirstOrDefault(f => f.Id == _currentFightId);
                if (existing != null) return existing;
            }

            var openFight = context.Fights.FirstOrDefault(f => f.EndTime == null);
            if (openFight != null)
            {
                _currentFightId = openFight.Id;
                return openFight;
            }

            HandleFightStart(time);
            return context.Fights.First(f => f.Id == _currentFightId);
        }

        private static void HandleFightOpponent(string[] parts, Models.FightEntity fight, Models.GameDbContext context)
        {
            var name = parts.ElementAtOrDefault(1) ?? string.Empty;
            var level = int.TryParse(parts.ElementAtOrDefault(15), out var lvl) ? lvl : 0;

            var type = context.OpponentTypes.FirstOrDefault(o => o.Name == name && o.Level == level);
            if (type == null)
            {
                type = new Models.OpponentTypeEntity
                {
                    Name = name,
                    Level = level
                };
                context.OpponentTypes.Add(type);
                context.SaveChanges();
            }

            var localOpponent = context.FightOpponents.Local
                .FirstOrDefault(o => o.FightId == fight.Id && o.OpponentTypeId == type.Id);

            if (localOpponent != null)
            {
                localOpponent.Quantity += 1;
            }
            else
            {
                var existingOpponent = context.FightOpponents
                    .FirstOrDefault(o => o.FightId == fight.Id && o.OpponentTypeId == type.Id);

                if (existingOpponent != null)
                {
                    existingOpponent.Quantity += 1;
                }
                else
                {
                    var opponent = new Models.FightOpponentEntity
                    {
                        Fight = fight,
                        OpponentType = type,
                        Quantity = 1
                    };

                    context.FightOpponents.Add(opponent);
                }
            }
        }

        private static void HandleFightPlayer(string[] parts, Models.FightEntity fight, Models.GameDbContext context)
        {
            var name = parts.ElementAtOrDefault(1) ?? string.Empty;
            var exp = int.TryParse(parts.ElementAtOrDefault(2), out var e) ? e : 0;
            var gold = int.TryParse(parts.ElementAtOrDefault(4), out var g) ? g : 0;
            var psycho = int.TryParse(parts.ElementAtOrDefault(24), out var p) ? p : 0;

            fight.PlayerName = name;
            fight.Exp = exp;
            fight.Gold = gold;
            fight.Psycho = psycho;

            ParseItems(parts.ElementAtOrDefault(9), fight, context);
            ParseDrifs(parts.ElementAtOrDefault(25), fight, context);
            ParseEquipment(parts.ElementAtOrDefault(7), fight, context, true);
            ParseEquipment(parts.ElementAtOrDefault(27), fight, context, false);

            context.SaveChanges();
        }

        private static string[] SplitEntries(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var parts = value.Split(["  "], StringSplitOptions.None);
            var result = new List<string>();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }

            return result.ToArray();
        }

        private static Models.ItemEntity GetOrCreateItem(Models.GameDbContext context, Models.DropType type, string name, int? value, int? rank, string? code)
        {
            var item = context.Items.Local.FirstOrDefault(i => i.Name == name) ??
                       context.Items.FirstOrDefault(i => i.Name == name);
            if (item == null)
            {
                item = new Models.ItemEntity
                {
                    DropType = type,
                    Name = name,
                    Value = value,
                    Rank = rank,
                    Code = code
                };
                context.Items.Add(item);
            }
            else
            {
                item.DropType = type;
                if (value.HasValue && !item.LockPrice) item.Value = value;
                if (rank.HasValue) item.Rank = rank;
                if (code != null) item.Code = code;
            }
            return item;
        }

        private static void ParseDrifs(string? value, Models.FightEntity fight, Models.GameDbContext context)
        {
            foreach (var part in SplitEntries(value))
            {
                var name = part.Split("[-]")[0];
                var item = GetOrCreateItem(context, Models.DropType.Drif, name, null, null, null);
                var drop = new Models.DropEntity
                {
                    Fight = fight,
                    Item = item
                };
                context.Drops.Add(drop);
            }
        }

        private static void ParseItems(string? value, Models.FightEntity fight, Models.GameDbContext context)
        {
            foreach (var part in SplitEntries(value))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                var name = part;
                var qty = 1;
                var open = part.LastIndexOf('(');
                var close = part.LastIndexOf(')');
                if (open > 0 && close > open && int.TryParse(part.Substring(open + 1, close - open - 1), out var q))
                {
                    name = part.Substring(0, open);
                    qty = q;
                }

                var item = GetOrCreateItem(context, Models.DropType.Item, name, null, null, null);
                var drop = new Models.DropEntity
                {
                    Fight = fight,
                    Item = item,
                    Quantity = qty
                };
                context.Drops.Add(drop);
            }
        }

        private static void ParseEquipment(string? value,
            Models.FightEntity fight,
            Models.GameDbContext context,
            bool special)
        {
            foreach (var part in SplitEntries(value))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                var info = ParseEquipmentPart(part, context);
                if (string.IsNullOrEmpty(info.Name))
                    continue;

                var type = GetDropType(info.Name, special);
                var valueField = CalculateEquipmentValue(context, type, info);

                AddEquipmentDrop(context, fight, type, valueField, info);
            }
        }

        private static EquipmentInfo ParseEquipmentPart(string part, Models.GameDbContext context)
        {
            var segments = part.Split("[-]");
            if (segments.Length < 4)
                return new EquipmentInfo(string.Empty, null, null, null, null, null, null);

            var name = segments[0];
            var quality = int.TryParse(segments[1], out var q) ? q : (int?)null;

            var valueSegments = segments[3].Split('$');

            double? parsedVal = null;
            int? ornamentField = null;
            string? orbCode = null;
            string? orbName = null;
            int? orbPrice = null;

            if (valueSegments.Length >= 3)
            {
                var third = valueSegments[2];
                var thirdParts = third.Split(',');
                if (thirdParts.Length >= 4 &&
                    double.TryParse(thirdParts[3], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    parsedVal = val;
                }

                if (thirdParts.Length >= 19 && int.TryParse(thirdParts[18], out var orn))
                {
                    ornamentField = orn;
                }

                if (valueSegments.Length >= 17)
                {
                    var orbSegment = valueSegments[16];
                    var orbParts = orbSegment.Split(',');
                    if (orbParts.Length >= 7)
                    {
                        orbCode = $"{orbParts[0]}_{orbParts[5]}_{orbParts[6]}";
                        var artifact = context.Items.FirstOrDefault(i => i.Code == orbCode);
                        if (artifact != null)
                        {
                            orbName = artifact.Name;
                            orbPrice = artifact.Value ?? 0;
                        }
                    }
                }
            }

            return new EquipmentInfo(name, quality, parsedVal, ornamentField, orbCode, orbName, orbPrice);
        }

        private static Models.DropType GetDropType(string name, bool special)
        {
            return special
                ? (name.Contains('"') ? Models.DropType.Rar : Models.DropType.Syng)
                : Models.DropType.Trash;
        }

        private static int? CalculateEquipmentValue(
            Models.GameDbContext context,
            Models.DropType type,
            EquipmentInfo info)
        {
            return EquipmentValueCalculator.CalculateValue(context, type, info);
        }


        private static void AddEquipmentDrop(
            Models.GameDbContext context,
            Models.FightEntity fight,
            Models.DropType type,
            int? valueField,
            EquipmentInfo info)
        {
            var item = GetOrCreateItem(context, type, info.Name, valueField, info.Quality, null);
            var drop = new Models.DropEntity
            {
                Fight = fight,
                Item = item,
                OrnamentCount = info.OrnamentField
            };
            context.Drops.Add(drop);

            if (!string.IsNullOrEmpty(info.OrbCode))
            {
                var orbItem = GetOrCreateItem(context, Models.DropType.Orb, info.OrbName ?? string.Empty, info.OrbPrice, null, info.OrbCode);
                var orbDrop = new Models.DropEntity
                {
                    Fight = fight,
                    Item = orbItem,
                    Quantity = 1
                };
                context.Drops.Add(orbDrop);
            }
        }

    }


}
