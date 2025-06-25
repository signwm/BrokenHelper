using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BrokenHelper
{
    public class HudForm : Form
    {
        private readonly TableLayoutPanel _fightPanel;
        private readonly TableLayoutPanel _instancePanel;
        private Label _fightExpValue = null!;
        private Label _fightPsychoValue = null!;
        private Label _fightGoldValue = null!;
        private Label _fightDropValue = null!;
        private Label _instanceNameValue = null!;
        private Label _instanceExpValue = null!;
        private Label _instancePsychoValue = null!;
        private Label _instanceGoldValue = null!;
        private Label _instanceDropValue = null!;
        private Label _instanceDurationValue = null!;
        private static readonly Font RowFont = new("Consolas", 8, FontStyle.Bold);
        private readonly Timer _timer;
        private readonly string _playerName;

        private static TableLayoutPanel CreateHudTable()
        {
            var table = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(10)
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            return table;
        }

        private Label AddRow(TableLayoutPanel table, string labelText)
        {
            var font = RowFont;
            var labelColor = Color.LightGray;
            var valueColor = Color.White;

            var label = new Label
            {
                Text = labelText,
                Font = font,
                ForeColor = labelColor,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 2, 0, 2)
            };

            var value = new Label
            {
                Text = string.Empty,
                Font = font,
                ForeColor = valueColor,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 2, 0, 2)
            };

            table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(label);
            table.Controls.Add(value);

            return value;
        }

        private void AddFightRows()
        {
            AddRow(_fightPanel, "Walka:");
            _fightExpValue = AddRow(_fightPanel, "EXP:");
            _fightPsychoValue = AddRow(_fightPanel, "Psycho:");
            _fightGoldValue = AddRow(_fightPanel, "Gold:");
            _fightDropValue = AddRow(_fightPanel, "Przedmioty:");
        }

        private void AddInstanceRows()
        {
            _instanceNameValue = AddRow(_instancePanel, "Instancja:");
            _instanceExpValue = AddRow(_instancePanel, "EXP:");
            _instancePsychoValue = AddRow(_instancePanel, "Psycho:");
            _instanceGoldValue = AddRow(_instancePanel, "Gold:");
            _instanceDropValue = AddRow(_instancePanel, "Przedmioty:");
            _instanceDurationValue = AddRow(_instancePanel, "Czas:");
        }

        public HudForm(string playerName)
        {
            _playerName = playerName;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 250;
            Height = 400;
            StartPosition = FormStartPosition.Manual;
            var screen = Screen.PrimaryScreen?.WorkingArea ?? Rectangle.Empty;
            Location = new Point(screen.Right - Width - 20, (screen.Bottom - Height) / 2);

            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;

            var container = new FlowLayoutPanel
            {
                BackColor = Color.FromArgb(160, 30, 30, 30),
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5)
            };
            Controls.Add(container);

            _fightPanel = CreateHudTable();
            _instancePanel = CreateHudTable();
            container.Controls.Add(_fightPanel);
            container.Controls.Add(_instancePanel);

            AddFightRows();
            AddInstanceRows();

            _timer = new Timer { Interval = 1000 };
            _timer.Tick += (s, e) => UpdateData();
            _timer.Start();

            UpdateData();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_LAYERED = 0x80000;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                return cp;
            }
        }

        private void UpdateData()
        {
            var player = string.IsNullOrWhiteSpace(_playerName) ? StatsService.GetDefaultPlayerName() : _playerName;
            if (string.IsNullOrWhiteSpace(player))
            {
                ClearFight();
                ClearInstance();
                return;
            }

            var fight = StatsService.GetLastFightSummary(player);
            if (fight == null)
            {
                ClearFight();
            }
            else
            {
                _fightExpValue.Text = fight.EarnedExp.ToString();
                _fightPsychoValue.Text = fight.EarnedPsycho.ToString();
                _fightGoldValue.Text = fight.FoundGold.ToString();
                _fightDropValue.Text = fight.DropValue.ToString();
            }

            var instance = StatsService.GetCurrentOrLastInstance(player);
            if (instance == null)
            {
                ClearInstance();
            }
            else
            {
                _instanceNameValue.Text = instance.Name;
                _instanceExpValue.Text = instance.EarnedExp.ToString();
                _instancePsychoValue.Text = instance.EarnedPsycho.ToString();
                _instanceGoldValue.Text = instance.FoundGold.ToString();
                _instanceDropValue.Text = instance.DropValue.ToString();
                _instanceDurationValue.Text = instance.Duration;
            }
        }

        private void ClearFight()
        {
            _fightExpValue.Text = "-";
            _fightPsychoValue.Text = "-";
            _fightGoldValue.Text = "-";
            _fightDropValue.Text = "-";
        }

        private void ClearInstance()
        {
            _instanceNameValue.Text = "Brak";
            _instanceExpValue.Text = "-";
            _instancePsychoValue.Text = "-";
            _instanceGoldValue.Text = "-";
            _instanceDropValue.Text = "-";
            _instanceDurationValue.Text = "-";
        }
    }
}
