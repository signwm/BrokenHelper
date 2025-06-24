using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class FightPlayerEntity
    {
        public int Id { get; set; }

        public int FightId { get; set; }
        public FightEntity Fight { get; set; }

        public int PlayerId { get; set; }
        public PlayerEntity Player { get; set; }

        public int Gold { get; set; }
        public int Exp { get; set; }
        public int Psycho { get; set; }

        public List<DropEntity> Drops { get; set; } = new();
    }
}
