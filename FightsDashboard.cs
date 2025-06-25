using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BrokenHelper
{
    public class FightsDashboard : Form
    {
        private readonly BufferedDataGridView _grid = new();
        private readonly CheckBox _withoutInstance = new() { Text = "Tylko bez instancji" };
        private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Short };
        private readonly Button _dayBefore = new() { Text = "<" };
        private readonly Button _dayAfter = new() { Text = ">" };
        private readonly Button _today = new() { Text = "Dziś" };
        private readonly Button _summary = new() { Text = "Podsumuj" };

        private int? _lastCheckedIndex;

        public FightsDashboard()
        {
            Text = "Lista walk";
            Width = 800;
            Height = 600;

            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            var checkCol = new DataGridViewCheckBoxColumn();
            checkCol.Width = 30;
            _grid.Columns.Add(checkCol);
            _grid.Columns.Add("time", "Czas");
            _grid.Columns.Add("players", "Gracze");
            _grid.Columns.Add("opponents", "Przeciwnicy");
            _grid.Columns.Add("exp", "Exp");
            _grid.Columns.Add("psycho", "Psycho");
            _grid.Columns.Add("gold", "Gold");
            _grid.Columns.Add("drop", "Drop");
            _grid.Columns.Add("drops", "Przedmioty");

            var top = new FlowLayoutPanel();
            top.Dock = DockStyle.Top;
            top.AutoSize = true;
            top.Controls.Add(_dayBefore);
            top.Controls.Add(_datePicker);
            top.Controls.Add(_dayAfter);
            top.Controls.Add(_today);
            top.Controls.Add(_withoutInstance);
            top.Controls.Add(_summary);

            Controls.Add(_grid);
            Controls.Add(top);

            Load += (_, _) => RefreshData();
            _dayBefore.Click += (_, _) => { _datePicker.Value = _datePicker.Value.AddDays(-1); RefreshData(); };
            _dayAfter.Click += (_, _) => { _datePicker.Value = _datePicker.Value.AddDays(1); RefreshData(); };
            _today.Click += (_, _) => { _datePicker.Value = DateTime.Today; RefreshData(); };
            _withoutInstance.CheckedChanged += (_, _) => RefreshData();
            _summary.Click += (_, _) => ShowSummary();
            _grid.CellContentClick += Grid_CellContentClick;
        }

        private string GetPlayerName()
        {
            using var ctx = new Models.GameDbContext();
            return ctx.Players.FirstOrDefault()?.Name ?? string.Empty;
        }

        private void RefreshData()
        {
            _grid.Rows.Clear();
            var from = _datePicker.Value.Date;
            var to = from.AddDays(1);
            var fights = StatsService.GetFights(GetPlayerName(), from, to, _withoutInstance.Checked);
            foreach (var f in fights)
            {
                int row = _grid.Rows.Add(false, f.Time, string.Join(", ", f.Players), string.Join(", ", f.Opponents), f.EarnedExp, f.EarnedPsycho, f.FoundGold, f.DropValue, f.Drops);
                _grid.Rows[row].Tag = f.Id;
            }
        }

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 0 || e.RowIndex < 0) return;
            var current = (bool)(_grid.Rows[e.RowIndex].Cells[0].Value ?? false);
            _grid.Rows[e.RowIndex].Cells[0].Value = !current;

            if ((ModifierKeys & Keys.Shift) == Keys.Shift && _lastCheckedIndex.HasValue)
            {
                int start = Math.Min(_lastCheckedIndex.Value, e.RowIndex);
                int end = Math.Max(_lastCheckedIndex.Value, e.RowIndex);
                for (int i = start; i <= end; i++)
                {
                    _grid.Rows[i].Cells[0].Value = !current;
                }
            }
            _lastCheckedIndex = e.RowIndex;
        }

        private void ShowSummary()
        {
            var ids = new List<int>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if ((bool)(row.Cells[0].Value ?? false))
                {
                    ids.Add((int)row.Tag);
                }
            }
            if (ids.Count == 0) return;

            var summary = StatsService.SummarizeFights(GetPlayerName(), ids);
            var drops = string.Join("\n", summary.Drops.Select(d => $"{d.Name}: {d.Quantity} (wartość {d.Value})"));
            MessageBox.Show($"Walk: {summary.FightCount}\nExp: {summary.EarnedExp}\nPsycho: {summary.EarnedPsycho}\nGold: {summary.FoundGold}\nDrop: {summary.DropValue}\n\n{drops}", "Podsumowanie");
        }
    }
}
