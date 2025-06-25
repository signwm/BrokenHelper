using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BrokenHelper.Models;

namespace BrokenHelper
{
    public partial class FightsDashboardWindow : Window
    {
        public FightsDashboardWindow()
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
            var fights = StatsService.GetFights(GetPlayerName(), from, to, withoutInstance.IsChecked == true);
            grid.ItemsSource = fights;
        }

        private void DateChanged(object sender, EventArgs e) => RefreshData();
        private void FilterChanged(object sender, RoutedEventArgs e) => RefreshData();
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
            var selected = grid.SelectedItems.Cast<FightInfo>().ToList();
            if (selected.Count == 0) return;
            var summary = StatsService.SummarizeFights(GetPlayerName(), selected.Select(f => f.Id));
            var drops = string.Join("\n", summary.Drops.Select(d => $"{d.Name}: {d.Quantity} (wartość {d.Value})"));
            MessageBox.Show($"Walk: {summary.FightCount}\nExp: {summary.EarnedExp}\nPsycho: {summary.EarnedPsycho}\nGold: {summary.FoundGold}\nDrop: {summary.DropValue}\n\n{drops}", "Podsumowanie");
        }

        private void CreateInstance_Click(object sender, RoutedEventArgs e)
        {
            var selected = grid.SelectedItems.Cast<FightInfo>().ToList();
            if (selected.Count == 0)
                return;

            if (selected.Any(f => f.InstanceId != null))
            {
                MessageBox.Show("Wszystkie zaznaczone walki muszą być bez instancji");
                return;
            }

            var start = selected.Min(f => f.Time).AddSeconds(-10);
            var end = selected.Max(f => f.Time);
            var window = new CreateInstanceWindow(StatsService.LastInstanceName, start, end)
            {
                Owner = this
            };
            if (window.ShowDialog() == true)
            {
                StatsService.LastInstanceName = window.InstanceName;
                StatsService.CreateInstance(window.InstanceName, window.Difficulty, window.StartTime, window.EndTime, selected.Select(f => f.Id));
                RefreshData();
            }
        }
    }
}
