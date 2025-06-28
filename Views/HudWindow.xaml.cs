using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BrokenHelper.Helpers;
using System.Windows.Threading;

namespace BrokenHelper
{
    public partial class HudWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly string _playerName;
        private PacketListener? _listener;
        private FightsDashboardWindow? _fightsWindow;
        private InstancesDashboardWindow? _instancesWindow;
        private LogsWindow? _logsWindow;
        private readonly MenuItem _listenMenuItem;

        private Grid _fightPanel = null!;
        private Grid _instancePanel = null!;

        private TextBlock _fightExpValue = null!;
        private TextBlock _fightPsychoValue = null!;
        private TextBlock _fightGoldValue = null!;
        private TextBlock _fightDropValue = null!;
        private TextBlock _fightDurationValue = null!;
        private TextBlock _instanceNameValue = null!;
        private TextBlock _instanceExpValue = null!;
        private TextBlock _instancePsychoValue = null!;
        private TextBlock _instanceGoldValue = null!;
        private TextBlock _instanceDropValue = null!;
        private TextBlock _instanceDurationValue = null!;

        private List<DropSummaryDetailed> _fightDrops = [];
        private List<DropSummaryDetailed> _instanceDrops = [];

        private DateTime? _fightStartTime;
        private bool _fightSummaryReceived;

        private void OnFightStarted()
        {
            Dispatcher.Invoke(() =>
            {
                ClearFight();
                _fightDrops.Clear();
                _fightStartTime = DateTime.Now;
                _fightSummaryReceived = false;
                _fightDurationValue.Text = "0:00";
                _fightDurationValue.Foreground = Brushes.White;
            });
        }

        private void OnFightSummary()
        {
            _fightSummaryReceived = true;
            Dispatcher.Invoke(UpdateData);
        }

        private void OnFightEnded()
        {
            Dispatcher.Invoke(() =>
            {
                if (_fightStartTime != null)
                {
                    var duration = DateTime.Now - _fightStartTime.Value;
                    _fightDurationValue.Text = TimeHelper.FormatDuration(duration);
                }
                _fightStartTime = null;
                _fightDurationValue.Foreground = Brushes.LightGreen;
            });
        }

        private void OnInstanceStarted()
        {
            Dispatcher.Invoke(() =>
            {
                ClearInstance();
                _instanceDrops.Clear();
                _instanceDurationValue.Foreground = Brushes.White;
                UpdateData();
            });
        }

        private void OnPlayerDied()
        {
            Dispatcher.Invoke(() =>
            {
                _fightDurationValue.Text = "Śmierć";
                _fightDurationValue.Foreground = Brushes.Red;
            });
        }

        public HudWindow(string playerName)
        {
            _playerName = playerName;
            InitializeComponent();
            var left = Preferences.GetInt("hud_left");
            var top = Preferences.GetInt("hud_top");
            if (left.HasValue && top.HasValue)
            {
                Left = left.Value;
                Top = top.Value;
            }
            else
            {
                Left = SystemParameters.WorkArea.Right - Width - 20;
                Top = (SystemParameters.WorkArea.Height - Height) / 2;
            }

            // context menu
            var menu = new ContextMenu();
            _listenMenuItem = new MenuItem();
            _listenMenuItem.Click += (_, _) => ToggleListener();
            menu.Items.Add(_listenMenuItem);
            menu.Items.Add(new Separator());

            var minimize = new MenuItem { Header = "Minimalizuj HUD" };
            minimize.Click += (_, _) => WindowState = WindowState.Minimized;
            menu.Items.Add(minimize);

            var instances = new MenuItem { Header = "Instancje" };
            instances.Click += (_, _) => ShowInstances();
            menu.Items.Add(instances);

            var fights = new MenuItem { Header = "Walki" };
            fights.Click += (_, _) => ShowFights();
            menu.Items.Add(fights);

            var logs = new MenuItem { Header = "Logi" };
            logs.Click += (_, _) => ShowLogs();
            menu.Items.Add(logs);

            var manual = new MenuItem { Header = "Ręczne wprowadzanie pakietu" };
            manual.Click += (_, _) => ShowManualPacketWindow();
            menu.Items.Add(manual);

            var sounds = new MenuItem
            {
                Header = "Sygnały dźwiękowe",
                IsCheckable = true,
                IsChecked = Preferences.SoundSignals
            };
            sounds.Click += (_, _) =>
            {
                Preferences.SoundSignals = sounds.IsChecked;
                Preferences.Save();
            };
            menu.Items.Add(sounds);

            menu.Items.Add(new Separator());
            var exit = new MenuItem { Header = "Zako\u0144cz" };
            exit.Click += (_, _) => Application.Current.Shutdown();
            menu.Items.Add(exit);

            rootBorder.ContextMenu = menu;
            MouseLeftButtonDown += HudWindow_MouseLeftButtonDown;

            // start listener by default
            ToggleListener();

            _fightPanel = CreateHudTable();
            _instancePanel = CreateHudTable();
            _fightPanel.Margin = new Thickness(10, 0, 10, 5);
            _instancePanel.Margin = new Thickness(10, 5, 10, 0);
            container.Children.Add(_fightPanel);
            container.Children.Add(_instancePanel);

            AddFightRows(_fightPanel);
            AddInstanceRows(_instancePanel);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => UpdateData();
            _timer.Start();

            GameEvents.FightStarted += OnFightStarted;
            GameEvents.FightSummary += OnFightSummary;
            GameEvents.FightEnded += OnFightEnded;
            GameEvents.InstanceStarted += OnInstanceStarted;
            GameEvents.PlayerDied += OnPlayerDied;

            UpdateData();
        }

        private static Grid CreateHudTable()
        {
            var grid = new Grid { Margin = new Thickness(10,0,10,0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return grid;
        }

        private TextBlock AddRow(Grid grid, string label)
        {
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(labelBlock, row);
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(valueBlock, row);
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);

            return valueBlock;
        }

        private void AddFightRows(Grid grid)
        {
            AddRow(grid, "Walka:");
            _fightExpValue = AddRow(grid, "EXP:");
            _fightPsychoValue = AddRow(grid, "Psycho:");
            _fightGoldValue = AddRow(grid, "Gold:");
            _fightDropValue = AddRow(grid, "Przedmioty:");
            _fightDurationValue = AddRow(grid, "Czas:");
        }

        private void AddInstanceRows(Grid grid)
        {
            _instanceNameValue = AddRow(grid, "Instancja:");
            _instanceExpValue = AddRow(grid, "EXP:");
            _instancePsychoValue = AddRow(grid, "Psycho:");
            _instanceGoldValue = AddRow(grid, "Gold:");
            _instanceDropValue = AddRow(grid, "Przedmioty:");
            _instanceDurationValue = AddRow(grid, "Czas:");
        }

        private static readonly NumberFormatInfo _nfi = new NumberFormatInfo
        {
            NumberGroupSeparator = " "
        };

        private static string FormatNumber(int value)
        {
            return value.ToString("N0", _nfi);
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
            bool showStats = _fightSummaryReceived || _fightStartTime == null;

            if (!showStats || fight == null)
            {
                _fightExpValue.Text = "-";
                _fightPsychoValue.Text = "-";
                _fightGoldValue.Text = "-";
                _fightDropValue.Text = "-";
                _fightDrops.Clear();
            }
            else
            {
                _fightExpValue.Text = FormatNumber(fight.EarnedExp);
                _fightPsychoValue.Text = FormatNumber(fight.EarnedPsycho);
                _fightGoldValue.Text = FormatNumber(fight.FoundGold);
                _fightDropValue.Text = FormatNumber(fight.DropValue);
                _fightDrops = StatsService.GetLastFightDropDetails(player);
            }

            if (_fightStartTime != null)
            {
                var duration = DateTime.Now - _fightStartTime.Value;
                _fightDurationValue.Text = TimeHelper.FormatDuration(duration);
            }
            UpdateTooltip(_fightPanel, _fightDrops);

            var instance = StatsService.GetCurrentOrLastInstance(player);
            if (instance == null)
            {
                ClearInstance();
                _instanceDrops.Clear();
            }
            else
            {
                _instanceNameValue.Text = instance.Name;
                _instanceExpValue.Text = FormatNumber(instance.EarnedExp);
                _instancePsychoValue.Text = FormatNumber(instance.EarnedPsycho);
                _instanceGoldValue.Text = FormatNumber(instance.FoundGold);
                _instanceDropValue.Text = FormatNumber(instance.DropValue);
                _instanceDurationValue.Text = instance.Duration;
                _instanceDrops = StatsService.GetCurrentOrLastInstanceDropDetails(player);

                var lastFinished = StatsService.GetLastFinishedInstance(player);
                bool finished = lastFinished != null && lastFinished.Id == instance.Id;
                _instanceDurationValue.Foreground = finished ? Brushes.LightGreen : Brushes.White;
            }
            UpdateTooltip(_instancePanel, _instanceDrops);
        }

        private void ClearFight()
        {
            _fightExpValue.Text = "-";
            _fightPsychoValue.Text = "-";
            _fightGoldValue.Text = "-";
            _fightDropValue.Text = "-";
            _fightDurationValue.Text = "-";
            _fightDurationValue.Foreground = Brushes.White;
            _fightStartTime = null;
        }

        private void ClearInstance()
        {
            _instanceNameValue.Text = "Brak";
            _instanceExpValue.Text = "-";
            _instancePsychoValue.Text = "-";
            _instanceGoldValue.Text = "-";
            _instanceDropValue.Text = "-";
            _instanceDurationValue.Text = "-";
            _instanceDurationValue.Foreground = Brushes.White;
        }

        private void UpdateTooltip(FrameworkElement element, List<DropSummaryDetailed> drops)
        {
            if (drops.Count == 0)
            {
                element.ToolTip = null;
                return;
            }

            int leftLen = drops.Max(d => $"{d.Name} ({d.Quantity})".Length);
            int rightLen = drops.Max(d => FormatNumber(d.TotalPrice).Length);
            var sb = new StringBuilder();
            foreach (var d in drops)
            {
                string left = $"{d.Name} ({d.Quantity})";
                sb.Append(left.PadRight(leftLen + 1));
                sb.Append(FormatNumber(d.TotalPrice).PadLeft(rightLen));
                sb.AppendLine();
            }

            var text = new TextBlock
            {
                Text = sb.ToString().TrimEnd(),
                FontFamily = new FontFamily("Consolas"),
                Background = Brushes.Black,
                Foreground = Brushes.White
            };

            element.ToolTip = new ToolTip
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Black,
                Foreground = Brushes.White,
                Content = text
            };
        }

        private void HudWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                DragMove();
            }
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

        private void ShowManualPacketWindow()
        {
            var window = new ManualPacketWindow { Owner = this };
            if (window.ShowDialog() == true)
            {
                ManualPacketProcessor.Process(window.Prefix, window.Message, window.Time);
            }
        }

        private void ShowLogs()
        {
            if (_logsWindow == null)
            {
                _logsWindow = new LogsWindow();
                _logsWindow.Closed += (_, _) => _logsWindow = null;
            }
            _logsWindow.Owner = this;
            _logsWindow.Show();
            _logsWindow.Activate();
        }

        protected override void OnClosed(EventArgs e)
        {
            Preferences.SetInt("hud_left", (int)Left);
            Preferences.SetInt("hud_top", (int)Top);
            Preferences.Save();
            _timer.Stop();
            _listener?.Stop();
            _fightsWindow?.Close();
            _instancesWindow?.Close();
            _logsWindow?.Close();
            GameEvents.FightStarted -= OnFightStarted;
            GameEvents.FightSummary -= OnFightSummary;
            GameEvents.FightEnded -= OnFightEnded;
            GameEvents.InstanceStarted -= OnInstanceStarted;
            GameEvents.PlayerDied -= OnPlayerDied;
            base.OnClosed(e);
        }
    }
}
