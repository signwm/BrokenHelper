using System.Collections.Generic;
using System.Linq;

namespace BrokenHelper
{
    public class PodsumowanieViewModel
    {
        public int InstanceCount { get; set; }
        public int FightCount { get; set; }
        public int TotalGold { get; set; }
        public int TotalExp { get; set; }
        public int TotalPsycho { get; set; }
        public int TotalProfit { get; set; }

        public List<string> EquipmentList { get; set; } = new();
        public List<string> ArtifactList { get; set; } = new();
        public List<string> ItemList { get; set; } = new();

        public void LoadData(List<DropSummaryDetailed> drops)
        {
            var equipment = drops.Where(d => d.Type == "Equipment");
            var artifacts = drops.Where(d => d.Type == "Artifact");
            var items = drops.Where(d => d.Type == "Item");

            EquipmentList = FormatList(equipment);
            ArtifactList = FormatList(artifacts);
            ItemList = FormatList(items);
        }

        private List<string> FormatList(IEnumerable<DropSummaryDetailed> drops)
        {
            return drops
                .OrderByDescending(d => d.TotalValue)
                .Select(d => $"{d.Name,-30} {d.Quantity} x {d.UnitPrice:N0} = {d.TotalValue:N0}")
                .ToList();
        }
    }
}
