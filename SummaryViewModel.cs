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

        public List<string> FormattedEquipmentList { get; private set; } = new();
        public List<string> FormattedArtifactList { get; private set; } = new();
        public List<string> FormattedItemList { get; private set; } = new();

        public int EquipmentSum { get; private set; }
        public int ArtifactSum { get; private set; }
        public int ItemSum { get; private set; }

        public string EquipmentSumFormatted => EquipmentSum.ToString("N0").Replace(',', ' ');
        public string ArtifactSumFormatted => ArtifactSum.ToString("N0").Replace(',', ' ');
        public string ItemSumFormatted => ItemSum.ToString("N0").Replace(',', ' ');

        public void LoadData(List<DropSummaryDetailed> drops)
        {
            SetListAndSum(drops.Where(d => d.Type == "Equipment"), out var eqList, out var eqSum);
            FormattedEquipmentList = eqList;
            EquipmentSum = eqSum;

            SetListAndSum(drops.Where(d => d.Type == "Artifact"), out var artList, out var artSum);
            FormattedArtifactList = artList;
            ArtifactSum = artSum;

            SetListAndSum(drops.Where(d => d.Type == "Item"), out var itemList, out var itemSum);
            FormattedItemList = itemList;
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
