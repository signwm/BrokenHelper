using System;
using System.Windows.Forms;

namespace BrokenHelper
{
    public class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _startStopMenuItem;
        private readonly ToolStripMenuItem _hudMenuItem;

        private PacketListener? _listener;
        private FightsDashboard? _fightsDashboard;
        private InstancesDashboard? _instancesDashboard;
        private HudForm? _hudForm;

        public TrayAppContext()
        {
            _startStopMenuItem = new ToolStripMenuItem("Zacznij nasłuchiwanie");
            _startStopMenuItem.Click += StartStopMenuItem_Click;

            _hudMenuItem = new ToolStripMenuItem("Włącz HUD");
            _hudMenuItem.Click += HudMenuItem_Click;

            var menu = new ContextMenuStrip();
            menu.Items.Add(_startStopMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Instancje", null, (_, _) => ShowInstances());
            menu.Items.Add("Walki", null, (_, _) => ShowFights());
            menu.Items.Add(_hudMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Wyłącz aplikację", null, (_, _) => ExitThread());

            _notifyIcon = new NotifyIcon
            {
                Text = "BRoken Helper",
                Icon = System.Drawing.SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };
        }

        private void StartStopMenuItem_Click(object? sender, EventArgs e)
        {
            if (_listener == null)
            {
                _listener = new PacketListener();
                _listener.Start();
                _startStopMenuItem.Text = "Zatrzymaj nasłuchiwanie";
            }
            else
            {
                _listener.Stop();
                _listener = null;
                _startStopMenuItem.Text = "Zacznij nasłuchiwanie";
            }
        }

        private void ShowFights()
        {
            _fightsDashboard ??= new FightsDashboard();
            _fightsDashboard.Show();
            _fightsDashboard.BringToFront();
        }

        private void ShowInstances()
        {
            _instancesDashboard ??= new InstancesDashboard();
            _instancesDashboard.Show();
            _instancesDashboard.BringToFront();
        }

        private void HudMenuItem_Click(object? sender, EventArgs e)
        {
            if (_hudForm == null || _hudForm.IsDisposed)
            {
                _hudForm = new HudForm();
                _hudForm.FormClosed += (_, _) => { _hudMenuItem.Text = "Włącz HUD"; _hudForm = null; };
                _hudForm.Show();
                _hudMenuItem.Text = "Wyłącz HUD";
            }
            else
            {
                _hudForm.Close();
                _hudForm = null;
                _hudMenuItem.Text = "Włącz HUD";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
