using System.Collections.Generic;
using System.Linq;

namespace BrokenHelper
{
    public class SummaryViewModel
    {
        public int InstanceCount { get; set; }
        public int FightCount { get; set; }
        public int TotalGold { get; set; }
        public int TotalExp { get; set; }
        public int TotalPsycho { get; set; }
        public int TotalDropValue { get; set; }
        public int TotalProfit { get; set; }
        public string TotalInstanceTime { get; set; } = string.Empty;

        public List<DropSummaryDetailed> EquipmentList { get; private set; } = new();
        public List<DropSummaryDetailed> ArtifactList { get; private set; } = new();
        public List<DropSummaryDetailed> ItemList { get; private set; } = new();

        public int EquipmentSum { get; private set; }
        public int ArtifactSum { get; private set; }
        public int ItemSum { get; private set; }

        public string EquipmentSumFormatted => EquipmentSum.ToString("N0").Replace(',', ' ');
        public string ArtifactSumFormatted => ArtifactSum.ToString("N0").Replace(',', ' ');
        public string ItemSumFormatted => ItemSum.ToString("N0").Replace(',', ' ');

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

        private static void SetListAndSum(IEnumerable<DropSummaryDetailed> source, out List<DropSummaryDetailed> result, out int sum)
        {
            var sorted = source
                .OrderByDescending(d => d.TotalValue)
                .ToList();
            result = sorted;
            sum = sorted.Sum(d => d.TotalValue);
        }
    }
}
