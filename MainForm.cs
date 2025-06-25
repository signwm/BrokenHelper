using System;
using System.Windows.Forms;

namespace BrokenHelper
{
    public partial class MainForm : Form
    {
        private PacketListener? _listener;
        private HudForm? _hud;

        private FightsDashboard? _fightsDashboard;
        private InstancesDashboard? _instancesDashboard;

        public MainForm()
        {
            InitializeComponent();
        }

        private void startStopButton_Click(object sender, EventArgs e)
        {
            if (_listener == null)
            {
                _listener = new PacketListener();
                _listener.Start();
                startStopButton.Text = "Stop";
                statusLabel.Text = "Listening";
            }
            else
            {
                _listener.Stop();
                _listener = null;
                startStopButton.Text = "Start";
                statusLabel.Text = "Stopped";
            }
        }

        private void hudCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (hudCheckBox.Checked)
            {
                var player = StatsService.GetDefaultPlayerName();
                _hud = new HudForm(player);
                _hud.Show();
            }
            else
            {
                _hud?.Close();
                _hud = null;
            }
        }
        
        private void fightsMenuItem_Click(object sender, EventArgs e)
        {
            _fightsDashboard ??= new FightsDashboard();
            _fightsDashboard.Show();
            _fightsDashboard.BringToFront();
        }

        private void instancesMenuItem_Click(object sender, EventArgs e)
        {
            _instancesDashboard ??= new InstancesDashboard();
            _instancesDashboard.Show();
            _instancesDashboard.BringToFront();
        }
    }
}
