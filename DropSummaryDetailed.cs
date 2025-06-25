namespace BrokenHelper
{
    public class DropSummaryDetailed
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Equipment, Artifact, Item
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
        public int TotalValue => Quantity * UnitPrice;
        public int TotalPrice => TotalValue;
    }
}
