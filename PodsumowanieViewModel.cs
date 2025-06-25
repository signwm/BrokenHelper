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

        public List<string> EquipmentList { get; private set; } = new();
        public List<string> ArtifactList { get; private set; } = new();
        public List<string> ItemList { get; private set; } = new();

        public int EquipmentSum { get; private set; }
        public int ArtifactSum { get; private set; }
        public int ItemSum { get; private set; }

        public void LoadData(List<DropSummaryDetailed> drops)
        {
            SetListAndSum(drops.Where(d => d.Type == "Equipment"), out var eqList, out var eqSum);
            EquipmentList = eqList;
            EquipmentSum = eqSum;

            SetListAndSum(drops.Where(d => d.Type == "Artifact"), out var artList, out var artSum);
            ArtifactList = artList;
            ArtifactSum = artSum;

            SetListAndSum(drops.Where(d => d.Type == "Item"), out var itemList, out var itemSum);
            ItemList = itemList;
            ItemSum = itemSum;
        }

        private static void SetListAndSum(IEnumerable<DropSummaryDetailed> source, out List<string> result, out int sum)
        {
            var sorted = source
                .OrderByDescending(d => d.TotalValue)
                .Select(d => $"{d.Name,-28} {d.Quantity} x {d.UnitPrice:N0} = {d.TotalValue:N0}".Replace(',', ' '))
                .ToList();
            result = sorted;
            sum = source.Sum(d => d.TotalValue);
        }
    }
}
