using System;
using System.Windows.Forms;

namespace BrokenHelper
{
    public partial class MainForm : Form
    {
        private PacketListener? _listener;

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
    }
}
