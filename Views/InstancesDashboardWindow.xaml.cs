using System;
using System.Linq;
using System.Windows;
using BrokenHelper.Models;

namespace BrokenHelper
{
    public partial class InstancesDashboardWindow : Window
    {
        public InstancesDashboardWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => RefreshData();
        }

        private string GetPlayerName()
        {
            using var ctx = new GameDbContext();
            return ctx.Players.FirstOrDefault()?.Name ?? string.Empty;
        }

        private void RefreshData()
        {
            grid.ItemsSource = null;
            var from = fromPicker.SelectedDate ?? DateTime.Today;
            var to = (toPicker.SelectedDate ?? DateTime.Today).AddDays(1);
            var data = StatsService.GetInstances(GetPlayerName(), from, to);
            grid.ItemsSource = data;
        }

        private void DateChanged(object sender, EventArgs e) => RefreshData();
        private void DayBefore_Click(object sender, RoutedEventArgs e)
        {
            fromPicker.SelectedDate = (fromPicker.SelectedDate ?? DateTime.Today).AddDays(-1);
            toPicker.SelectedDate = (toPicker.SelectedDate ?? DateTime.Today).AddDays(-1);
            RefreshData();
        }
        private void DayAfter_Click(object sender, RoutedEventArgs e)
        {
            fromPicker.SelectedDate = (fromPicker.SelectedDate ?? DateTime.Today).AddDays(1);
            toPicker.SelectedDate = (toPicker.SelectedDate ?? DateTime.Today).AddDays(1);
            RefreshData();
        }
        private void Today_Click(object sender, RoutedEventArgs e)
        {
            fromPicker.SelectedDate = DateTime.Today;
            toPicker.SelectedDate = DateTime.Today;
            RefreshData();
        }
        private void Summary_Click(object sender, RoutedEventArgs e)
        {
            var selected = grid.SelectedItems.Cast<InstanceInfo>().ToList();
            if (selected.Count == 0) return;

            var fightIds = selected.SelectMany(i => StatsService.GetFights(GetPlayerName(), i.Id))
                .Select(f => f.Id)
                .ToList();
            var summary = StatsService.SummarizeFights(GetPlayerName(), fightIds);
            var details = StatsService.GetDropDetails(GetPlayerName(), fightIds);

            var vm = new PodsumowanieViewModel
            {
                InstanceCount = selected.Count,
                FightCount = summary.FightCount,
                TotalGold = summary.FoundGold,
                TotalExp = summary.EarnedExp,
                TotalPsycho = summary.EarnedPsycho,
                TotalProfit = summary.FoundGold + summary.DropValue
            };
            vm.LoadData(details);

            var window = new Views.PanelPodsumowania
            {
                DataContext = vm,
                Owner = this
            };
            window.ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selected = grid.SelectedItems.Cast<InstanceInfo>().ToList();
            if (selected.Count == 0)
                return;

            var result = MessageBox.Show($"Czy na pewno chcesz usunąć {selected.Count} instancji?", "Potwierdź", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
                return;

            StatsService.DeleteInstances(selected.Select(i => i.Id));
            RefreshData();
        }
    }
}
