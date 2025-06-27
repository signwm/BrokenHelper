using System;
using System.Linq;

namespace BrokenHelper.PacketHandlers
{
    internal static class PriceHandler
    {
        public static void HandleItemPriceMessage(string message)
        {
            ParsePrices(message, false);
        }

        public static void HandleArtifactPriceMessage(string message)
        {
            ParsePrices(message, true);
        }

        private static void ParsePrices(string message, bool artifact)
        {
            var entries = message.Split("[&&]", StringSplitOptions.None);

            using var context = new Models.GameDbContext();
            foreach (var entryRaw in entries)
            {
                var entry = entryRaw.Trim();
                if (!entry.Contains(','))
                    continue;

                var parts = entry.Split(',', StringSplitOptions.None);

                if (artifact)
                {
                    if (parts.Length < 2)
                        continue;

                    var code = parts[0];
                    if (!int.TryParse(parts[1], out var value))
                        value = 0;

                    var name = parts.Length >= 5 ? string.Join(',', parts.Skip(4)) : parts[^1];

                    var existing = context.ArtifactPrices.Local
                        .FirstOrDefault(p => p.Code == code || p.Name == name);
                    existing ??= context.ArtifactPrices
                        .FirstOrDefault(p => p.Code == code || p.Name == name);

                    if (existing == null)
                    {
                        var price = new Models.ArtifactPriceEntity
                        {
                            Code = code,
                            Value = value,
                            Name = name
                        };
                        context.ArtifactPrices.Add(price);
                    }
                    else
                    {
                        existing.Code = code;
                        existing.Value = value;
                        existing.Name = name;
                    }

                    var item = context.Items.Local.FirstOrDefault(i => i.Name == name) ??
                               context.Items.FirstOrDefault(i => i.Name == name);
                    if (item == null)
                    {
                        item = new Models.ItemEntity
                        {
                            Name = name,
                            Code = code,
                            Value = value,
                            Type = Models.DropType.Drif
                        };
                        context.Items.Add(item);
                    }
                    else
                    {
                        item.Code = code;
                        item.Value = value;
                        item.Type = Models.DropType.Drif;
                    }
                }
                else
                {
                    if (parts.Length < 3)
                        continue;

                    var name = parts[1];
                    if (!int.TryParse(parts[2], out var value))
                        value = 0;

                    var existing = context.ItemPrices.Local
                        .FirstOrDefault(p => p.Name == name);
                    existing ??= context.ItemPrices
                        .FirstOrDefault(p => p.Name == name);

                    if (existing == null)
                    {
                        var price = new Models.ItemPriceEntity
                        {
                            Name = name,
                            Value = value
                        };
                        context.ItemPrices.Add(price);
                    }
                    else
                    {
                        existing.Value = value;
                    }

                    var item = context.Items.Local.FirstOrDefault(i => i.Name == name) ??
                               context.Items.FirstOrDefault(i => i.Name == name);
                    if (item == null)
                    {
                        item = new Models.ItemEntity
                        {
                            Name = name,
                            Value = value,
                            Type = Models.DropType.Item
                        };
                        context.Items.Add(item);
                    }
                    else
                    {
                        item.Value = value;
                        item.Type = Models.DropType.Item;
                    }
                }
            }

            context.SaveChanges();
        }
    }
}
