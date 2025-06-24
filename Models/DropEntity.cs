namespace BrokenHelper.Models
{
    public class DropEntity
    {
        public int Id { get; set; }

        public int FightPlayerId { get; set; }
        public FightPlayerEntity FightPlayer { get; set; }

        public DropType DropType { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? Value { get; set; }

        // Equipment-specific
        public int? Rank { get; set; }
        public int? OrnamentCount { get; set; }

        // Orb/Drif-specific
        public string? Code { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
