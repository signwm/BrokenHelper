using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BrokenHelper
{
    public class HudForm : Form
    {
        private readonly Panel _fightPanel;
        private readonly Panel _instancePanel;
        private readonly Label _fightLabel;
        private readonly Label _instanceLabel;
        private readonly Timer _timer;
        private readonly string _playerName;

        public HudForm(string playerName)
        {
            _playerName = playerName;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 250;
            Height = 170;
            StartPosition = FormStartPosition.Manual;
            var screen = Screen.PrimaryScreen?.WorkingArea ?? Rectangle.Empty;
            Location = new Point(screen.Right - Width - 20, (screen.Bottom + Height) / 2);

            BackColor = Color.Black;
            TransparencyKey = Color.Black;

            var panel = new Panel
            {
                BackColor = Color.FromArgb(120, 30, 30, 30),
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            Controls.Add(panel);

            _instancePanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            panel.Controls.Add(_instancePanel);

            _fightPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            panel.Controls.Add(_fightPanel);

            _fightLabel = new Label
            {
                ForeColor = Color.White,
                AutoSize = true,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.TopRight
            };
            _fightPanel.Controls.Add(_fightLabel);

            _instanceLabel = new Label
            {
                ForeColor = Color.White,
                AutoSize = true,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.TopRight
            };
            _instancePanel.Controls.Add(_instanceLabel);

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
                _fightLabel.Text = "";
                _instanceLabel.Text = "";
                return;
            }

            var fight = StatsService.GetLastFightSummary(player);
            if (fight == null)
            {
                _fightLabel.Text = "Brak danych o walce";
            }
            else
            {
                _fightLabel.Text = $"Ostatnia walka:\nEXP: {fight.EarnedExp}\nPsycho: {fight.EarnedPsycho}\nGold: {fight.FoundGold}\nPrzedmioty: {fight.DropValue}";
            }

            var instance = StatsService.GetCurrentOrLastInstance(player);
            if (instance == null)
            {
                _instanceLabel.Text = "Brak instancji";
            }
            else
            {
                _instanceLabel.Text = $"Instancja: {instance.Name}\nEXP: {instance.EarnedExp}\nPsycho: {instance.EarnedPsycho}\nGold: {instance.FoundGold}\nPrzedmioty: {instance.DropValue}\nCzas: {instance.Duration}";
            }
        }
    }
}
