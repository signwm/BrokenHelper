using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BrokenHelper
{
    public class HudForm : Form
    {
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
            var screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Right - Width - 20, screen.Top + 300);

            BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;

            var panel = new Panel
            {
                BackColor = Color.FromArgb(120, 0, 0, 0),
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            Controls.Add(panel);

            _fightLabel = new Label
            {
                ForeColor = Color.White,
                AutoSize = true
            };
            panel.Controls.Add(_fightLabel);

            _instanceLabel = new Label
            {
                ForeColor = Color.White,
                AutoSize = true,
                Top = _fightLabel.Bottom + 10
            };
            panel.Controls.Add(_instanceLabel);

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

