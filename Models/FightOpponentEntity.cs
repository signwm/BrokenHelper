namespace BrokenHelper.Models
{
    public class FightOpponentEntity
    {
        public int Id { get; set; }

        public int FightId { get; set; }
        public FightEntity Fight { get; set; }

        public int OpponentTypeId { get; set; }
        public OpponentTypeEntity OpponentType { get; set; }

        public int Quantity { get; set; }
    }
}
