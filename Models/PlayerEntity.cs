using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class PlayerEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<FightPlayerEntity> Fights { get; set; } = [];
    }
}
