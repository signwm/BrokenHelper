using System;
using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class FightEntity
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int? InstanceId { get; set; }
        public InstanceEntity? Instance { get; set; }

        public string PlayerName { get; set; } = string.Empty;
        public int Gold { get; set; }
        public int Exp { get; set; }
        public int Psycho { get; set; }

        public List<DropEntity> Drops { get; set; } = [];
        public List<FightOpponentEntity> Opponents { get; set; } = [];
    }
}
