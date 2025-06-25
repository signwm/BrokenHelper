using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace BrokenHelper
{
    internal class TrayIcon : IDisposable
    {
        private readonly TaskbarIcon _notifyIcon;
        private PacketListener? _listener;
        private HudWindow? _hud;
        private FightsDashboardWindow? _fightsWindow;
        private InstancesDashboardWindow? _instancesWindow;
        private readonly MenuItem _listenMenuItem;
        private readonly MenuItem _hudMenuItem;

        public TrayIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visibility = Visibility.Visible
            };

            var menu = new ContextMenu();

            _listenMenuItem = new MenuItem();
            _listenMenuItem.Click += (_, _) => ToggleListener();
            menu.Items.Add(_listenMenuItem);
            menu.Items.Add(new Separator());

            _hudMenuItem = new MenuItem();
            _hudMenuItem.Click += (_, _) => ToggleHud();
            menu.Items.Add(_hudMenuItem);

            var instances = new MenuItem { Header = "Instancje" };
            instances.Click += (_, _) => ShowInstances();
            menu.Items.Add(instances);

            var fights = new MenuItem { Header = "Walki" };
            fights.Click += (_, _) => ShowFights();
            menu.Items.Add(fights);

            menu.Items.Add(new Separator());
            var exit = new MenuItem { Header = "Zako\u0144cz" };
            exit.Click += (_, _) => Application.Current.Shutdown();
            menu.Items.Add(exit);

            _notifyIcon.ContextMenu = menu;

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
                _listenMenuItem.Header = "Wy\u0142\u0105cz nas\u0142uchiwanie";
            }
            else
            {
                _listener.Stop();
                _listener = null;
                _listenMenuItem.Header = "W\u0142\u0105cz nas\u0142uchiwanie";
            }
        }

        private void ToggleHud()
        {
            if (_hud == null)
            {
                var player = StatsService.GetDefaultPlayerName();
                _hud = new HudWindow(player);
                _hud.Show();
                _hudMenuItem.Header = "Wy\u0142\u0105cz HUD";
            }
            else
            {
                _hud.Close();
                _hud = null;
                _hudMenuItem.Header = "W\u0142\u0105cz HUD";
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
            _notifyIcon.Visibility = Visibility.Collapsed;
            _notifyIcon.Dispose();
            _listener?.Stop();
            _hud?.Close();
        }
    }
}
