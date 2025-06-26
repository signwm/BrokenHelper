using System;
using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class FightEntity
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int? InstanceId { get; set; }
        public InstanceEntity? Instance { get; set; }

        public List<FightPlayerEntity> Players { get; set; } = new();
        public List<FightOpponentEntity> Opponents { get; set; } = new();
    }
}
