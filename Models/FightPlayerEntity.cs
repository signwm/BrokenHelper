using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class FightPlayerEntity
    {
        public int Id { get; set; }

        public int FightId { get; set; }
        public FightEntity Fight { get; set; } = null!;

        public int PlayerId { get; set; }
        public PlayerEntity Player { get; set; } = null!;

        public int Gold { get; set; }
        public int Exp { get; set; }
        public int Psycho { get; set; }

        public List<DropEntity> Drops { get; set; } = [];
    }
}
