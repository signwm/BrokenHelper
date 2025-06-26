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
        }

        public void HandleFightSummary(string message, DateTime? time = null)
        {
            if (_currentFightId == null)
            {
                HandleFightStart(time);
            }

            using var context = new Models.GameDbContext();

            var fight = context.Fights.FirstOrDefault(f => f.Id == _currentFightId);
            if (fight == null)
                return;

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
                    HandleFightPlayer(fields, fight, context);
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
        }

        public void HandleFightEnd(DateTime? time = null)
        {
            if (_currentFightId == null)
                return;

            using var context = new Models.GameDbContext();

            var fight = context.Fights.FirstOrDefault(f => f.Id == _currentFightId);
            if (fight == null)
            {
                _currentFightId = null;
                return;
            }

            fight.EndTime = time ?? DateTime.Now;
            context.SaveChanges();

            _instanceHandler.CloseIfPending(fight.EndTime, context);

            _currentFightId = null;
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
                    Level = level,
                    IsBoss = false
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

            var player = context.Players.FirstOrDefault(pl => pl.Name == name);
            if (player == null)
            {
                player = new Models.PlayerEntity { Name = name };
                context.Players.Add(player);
                context.SaveChanges();
            }

            var fightPlayer = new Models.FightPlayerEntity
            {
                Fight = fight,
                Player = player,
                Exp = exp,
                Gold = gold,
                Psycho = psycho
            };

            context.FightPlayers.Add(fightPlayer);

            ParseItems(parts.ElementAtOrDefault(9), fightPlayer, context);
            ParseDrifs(parts.ElementAtOrDefault(25), fightPlayer, context);
            ParseEquipment(parts.ElementAtOrDefault(7), fightPlayer, context, 0.3, true);
            ParseEquipment(parts.ElementAtOrDefault(27), fightPlayer, context, 0.025, false);

            context.SaveChanges();
        }

        private static string[] SplitEntries(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var parts = value.Split(new[] { "  " }, StringSplitOptions.None);
            var result = new List<string>();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }

            return result.ToArray();
        }

        private static void ParseDrifs(string? value, Models.FightPlayerEntity fightPlayer, Models.GameDbContext context)
        {
            foreach (var part in SplitEntries(value))
            {
                var name = part.Split("[-]")[0];
                var drop = new Models.DropEntity
                {
                    FightPlayer = fightPlayer,
                    DropType = Models.DropType.Drif,
                    Name = name
                };
                context.Drops.Add(drop);
            }
        }

        private static void ParseItems(string? value, Models.FightPlayerEntity fightPlayer, Models.GameDbContext context)
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

                var drop = new Models.DropEntity
                {
                    FightPlayer = fightPlayer,
                    DropType = Models.DropType.Item,
                    Name = name,
                    Quantity = qty
                };
                context.Drops.Add(drop);
            }
        }

        private static void ParseEquipment(string? value,
            Models.FightPlayerEntity fightPlayer,
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
                int? ornamentField = null;
                string? orbCode = null;
                string? orbName = null;
                int? orbPrice = null;
                if (valueSegments.Length >= 3)
                {
                    var third = valueSegments[2];
                    var thirdParts = third.Split(',');
                    if (thirdParts.Length >= 4 && int.TryParse(thirdParts[3], out var val))
                    {
                        valueField = (int)Math.Round(val * multiplier);
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
                            var artifact = context.ArtifactPrices.FirstOrDefault(a => a.Code == orbCode);
                            if (artifact != null)
                            {
                                orbName = artifact.Name;
                                orbPrice = artifact.Value;
                            }
                        }
                    }
                }

                if (special)
                {
                    var shardPrice = context.ItemPrices.FirstOrDefault(p => p.Name == "Odłamek")?.Value ?? 0;
                    var essencePrice = context.ItemPrices.FirstOrDefault(p => p.Name == "Esencja")?.Value ?? 0;

                    if (name.Contains('"'))
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

                var drop = new Models.DropEntity
                {
                    FightPlayer = fightPlayer,
                    DropType = Models.DropType.Equipment,
                    Name = name,
                    Rank = quality,
                    Value = valueField,
                    OrnamentCount = ornamentField
                };
                context.Drops.Add(drop);

                if (!string.IsNullOrEmpty(orbCode))
                {
                    var orbDrop = new Models.DropEntity
                    {
                        FightPlayer = fightPlayer,
                        DropType = Models.DropType.Orb,
                        Code = orbCode,
                        Name = orbName ?? string.Empty,
                        Value = orbPrice,
                        Quantity = 1
                    };
                    context.Drops.Add(orbDrop);
                }
            }
        }
    }
}
