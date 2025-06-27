namespace BrokenHelper.Models
{
    public class DropEntity
    {
        public int Id { get; set; }

        public int FightId { get; set; }
        public FightEntity Fight { get; set; } = null!;

        public int ItemId { get; set; }
        public ItemEntity Item { get; set; } = null!;

        public int? OrnamentCount { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
