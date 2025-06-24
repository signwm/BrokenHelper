using System;
using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class InstanceEntity
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<FightEntity> Fights { get; set; } = new();
    }
}
