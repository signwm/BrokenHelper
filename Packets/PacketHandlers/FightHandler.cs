using System;
using System.Collections.Generic;
using System.Linq;

namespace BrokenHelper.PacketHandlers
{
    internal class FightHandler
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
            if (_currentFightId == null)
            {
                using var ctx = new Models.GameDbContext();
                var openFight = ctx.Fights.FirstOrDefault(f => f.EndTime == null);
                if (openFight != null)
                {
                    _currentFightId = openFight.Id;
                }
                else
                {
                    HandleFightStart(time);
                }
            }

            using var context = new Models.GameDbContext();

            var fight = context.Fights.FirstOrDefault(f => f.Id == _currentFightId);
            if (fight == null)
            {
                var openFight = context.Fights.FirstOrDefault(f => f.EndTime == null);
                if (openFight != null)
                {
                    _currentFightId = openFight.Id;
                    fight = openFight;
                }
                else
                {
                    HandleFightStart(time);
                    fight = context.Fights.First(f => f.Id == _currentFightId);
                }
            }

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
            if (_currentFightId == null)
            {
                using var ctx = new Models.GameDbContext();
                var openFight = ctx.Fights.FirstOrDefault(f => f.EndTime == null);
                if (openFight != null)
                {
                    _currentFightId = openFight.Id;
                }
                else
                {
                    HandleFightStart(time);
                }
            }

            using var context = new Models.GameDbContext();

            var fight = context.Fights.FirstOrDefault(f => f.Id == _currentFightId);
            if (fight == null)
            {
                var openFight = context.Fights.FirstOrDefault(f => f.EndTime == null);
                if (openFight != null)
                {
                    _currentFightId = openFight.Id;
                    fight = openFight;
                }
                else
                {
                    HandleFightStart(time);
                    fight = context.Fights.First(f => f.Id == _currentFightId);
                }
            }

            fight.EndTime = time ?? DateTime.Now;
            context.SaveChanges();

            _instanceHandler.CloseIfPending(fight.EndTime!.Value, context);

            _currentFightId = null;

            GameEvents.OnFightEnded();
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
            ParseEquipment(parts.ElementAtOrDefault(7), fight, context, 0.3, true);
            ParseEquipment(parts.ElementAtOrDefault(27), fight, context, 0.025, false);

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
                if (value.HasValue) item.Value = value;
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
            double multiplier,
            bool special)
        {
            foreach (var part in SplitEntries(value))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                var segments = part.Split("[-]");
                if (segments.Length < 4)
                    continue;

                var name = segments[0];
                var quality = int.TryParse(segments[1], out var q) ? q : (int?)null;

                var afterThird = segments[3];
                var valueSegments = afterThird.Split('$');
                int? valueField = null;
                double? parsedVal = null;
                int? ornamentField = null;
                string? orbCode = null;
                string? orbName = null;
                int? orbPrice = null;
                if (valueSegments.Length >= 3)
                {
                    var third = valueSegments[2];
                    var thirdParts = third.Split(',');
                    if (thirdParts.Length >= 4 && double.TryParse(thirdParts[3], out var val))
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

                var type = special
                    ? (name.Contains('"') ? Models.DropType.Rar : Models.DropType.Syng)
                    : Models.DropType.Trash;

                if (type == Models.DropType.Rar || type == Models.DropType.Syng)
                {
                    var shardPrice = context.Items.FirstOrDefault(i => i.Name == "Odłamek")?.Value ?? 0;
                    var essencePrice = context.Items.FirstOrDefault(i => i.Name == "Esencja")?.Value ?? 0;

                    if (type == Models.DropType.Rar)
                    {
                        if (ornamentField.HasValue && quality.HasValue &&
                            ornamentField.Value >= 0 && ornamentField.Value < PacketListener.QuoteItemCoefficients.GetLength(0) &&
                            quality.Value >= 7 && quality.Value <= PacketListener.QuoteItemCoefficients.GetLength(1))
                        {
                            int coef = PacketListener.QuoteItemCoefficients[ornamentField.Value, quality.Value - 1];
                            var basePrice = quality.Value >= 7 ? shardPrice : essencePrice;
                            valueField = coef * basePrice;
                        }
                    }
                    else if (name.Contains("Smoków"))
                    {
                        valueField = 12 * shardPrice;
                    }
                    else if (name.Contains("Vorlingów") || name.Contains("Lodu"))
                    {
                        valueField = 30 * shardPrice;
                    }
                    else if (name.Contains("Władców"))
                    {
                        valueField = 150 * shardPrice;
                    }
                    else if (name.Contains("Dawnych Orków"))
                    {
                        valueField = 60 * shardPrice;
                    }
                }

                if (valueField == null && parsedVal.HasValue)
                {
                    var val = type switch
                    {
                        Models.DropType.Trash => 0.025,
                        Models.DropType.Syng => 0.3,
                        _ => parsedVal.Value
                    };

                    valueField = (int)Math.Round(val * multiplier);
                }

                var item = GetOrCreateItem(context, type, name, valueField, quality, null);
                var drop = new Models.DropEntity
                {
                    Fight = fight,
                    Item = item,
                    OrnamentCount = ornamentField
                };
                context.Drops.Add(drop);

                if (!string.IsNullOrEmpty(orbCode))
                {
                    var orbItem = GetOrCreateItem(context, Models.DropType.Orb, orbName ?? string.Empty, orbPrice, null, orbCode);
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
}
