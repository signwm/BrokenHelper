using System;
using System.Linq;

namespace BrokenHelper.PacketHandlers
{
    public static class EquipmentValueCalculator
    {
        public static int? CalculateValue(Models.GameDbContext context, Models.DropType type, EquipmentInfo info)
        {
            return type switch
            {
                Models.DropType.Rar => CalculateRarValue(context, info),
                Models.DropType.Syng => CalculateSyngValue(context, info),
                Models.DropType.Trash => CalculateTrashValue(info),
                _ => CalculateTrashValue(info)
            };
        }

        private static int? CalculateRarValue(Models.GameDbContext context, EquipmentInfo info)
        {
            var shardPrice = context.Items.FirstOrDefault(i => i.Name == "Odłamek")?.Value ?? 0;
            var essencePrice = context.Items.FirstOrDefault(i => i.Name == "Esencja")?.Value ?? 0;

            if (info.OrnamentField is int orn && info.Quality is int qual &&
                orn >= 0 && orn < PacketListener.QuoteItemCoefficients.GetLength(0) &&
                qual >= 7 && qual <= PacketListener.QuoteItemCoefficients.GetLength(1))
            {
                int coef = PacketListener.QuoteItemCoefficients[orn, qual - 1];
                int basePrice = qual >= 7 ? shardPrice : essencePrice;
                return coef * basePrice;
            }

            return null;
        }

        private static int? CalculateSyngValue(Models.GameDbContext context, EquipmentInfo info)
        {
            var shardPrice = context.Items.FirstOrDefault(i => i.Name == "Odłamek")?.Value ?? 0;

            if (info.Name.Contains("Smoków"))
                return 12 * shardPrice;
            if (info.Name.Contains("Vorlingów") || info.Name.Contains("Lodu"))
                return 30 * shardPrice;
            if (info.Name.Contains("Władców"))
                return 150 * shardPrice;
            if (info.Name.Contains("Dawnych Orków"))
                return 60 * shardPrice;

            return info.ParsedVal.HasValue ? (int?)(0.3 * info.ParsedVal.Value) : null;
        }

        private static int? CalculateTrashValue(EquipmentInfo info)
        {
            return info.ParsedVal.HasValue
                ? (int)Math.Round(0.025 * info.ParsedVal.Value)
                : null;
        }

    }
}