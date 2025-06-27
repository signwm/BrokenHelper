using System.Collections.Generic;

namespace BrokenHelper.Models
{
    public class ItemEntity
    {
        public int Id { get; set; }
        public DropType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Value { get; set; }
        public int? Rank { get; set; }
        public string? Code { get; set; }

        public List<DropEntity> Drops { get; set; } = [];
    }
}
