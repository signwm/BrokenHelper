using System;
using System.Windows;
using System.Windows.Forms;

namespace BrokenHelper
{
    internal class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private PacketListener? _listener;
        private HudWindow? _hud;
        private FightsDashboardWindow? _fightsWindow;
        private InstancesDashboardWindow? _instancesWindow;
        private readonly ToolStripMenuItem _listenMenuItem;
        private readonly ToolStripMenuItem _hudMenuItem;

        public TrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true
            };

            var menu = new ContextMenuStrip();

            _listenMenuItem = new ToolStripMenuItem();
            _listenMenuItem.Click += (_, _) => ToggleListener();
            menu.Items.Add(_listenMenuItem);
            menu.Items.Add(new ToolStripSeparator());

            _hudMenuItem = new ToolStripMenuItem();
            _hudMenuItem.Click += (_, _) => ToggleHud();
            menu.Items.Add(_hudMenuItem);

            var instances = new ToolStripMenuItem("Instancje");
            instances.Click += (_, _) => ShowInstances();
            menu.Items.Add(instances);

            var fights = new ToolStripMenuItem("Walki");
            fights.Click += (_, _) => ShowFights();
            menu.Items.Add(fights);

            menu.Items.Add(new ToolStripSeparator());
            var exit = new ToolStripMenuItem("Zako\u0144cz");
            exit.Click += (_, _) => Application.Current.Shutdown();
            menu.Items.Add(exit);

            _notifyIcon.ContextMenuStrip = menu;

            // start with listener and HUD enabled
            ToggleListener(); // this will start and set text
            ToggleHud();      // this will show and set text
        }

        private void ToggleListener()
        {
            if (_listener == null)
            {
                _listener = new PacketListener();
                _listener.Start();
                _listenMenuItem.Text = "Wy\u0142\u0105cz nas\u0142uchiwanie";
            }
            else
            {
                _listener.Stop();
                _listener = null;
                _listenMenuItem.Text = "W\u0142\u0105cz nas\u0142uchiwanie";
            }
        }

        private void ToggleHud()
        {
            if (_hud == null)
            {
                var player = StatsService.GetDefaultPlayerName();
                _hud = new HudWindow(player);
                _hud.Show();
                _hudMenuItem.Text = "Wy\u0142\u0105cz HUD";
            }
            else
            {
                _hud.Close();
                _hud = null;
                _hudMenuItem.Text = "W\u0142\u0105cz HUD";
            }
        }

        private void ShowInstances()
        {
            if (_instancesWindow == null)
            {
                _instancesWindow = new InstancesDashboardWindow();
                _instancesWindow.Closed += (_, _) => _instancesWindow = null;
            }
            _instancesWindow.Show();
            _instancesWindow.Activate();
        }

        private void ShowFights()
        {
            if (_fightsWindow == null)
            {
                _fightsWindow = new FightsDashboardWindow();
                _fightsWindow.Closed += (_, _) => _fightsWindow = null;
            }
            _fightsWindow.Show();
            _fightsWindow.Activate();
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _listener?.Stop();
            _hud?.Close();
        }
    }
}
