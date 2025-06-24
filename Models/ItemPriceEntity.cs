using System;

namespace BrokenHelper.Models
{
    public class ItemPriceEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
