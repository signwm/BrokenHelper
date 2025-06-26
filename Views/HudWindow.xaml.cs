using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private readonly MenuItem _listenMenuItem;

        private TextBlock _fightExpValue = null!;
        private TextBlock _fightPsychoValue = null!;
        private TextBlock _fightGoldValue = null!;
        private TextBlock _fightDropValue = null!;
        private TextBlock _instanceNameValue = null!;
        private TextBlock _instanceExpValue = null!;
        private TextBlock _instancePsychoValue = null!;
        private TextBlock _instanceGoldValue = null!;
        private TextBlock _instanceDropValue = null!;
        private TextBlock _instanceDurationValue = null!;

        public HudWindow(string playerName)
        {
            _playerName = playerName;
            InitializeComponent();
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = (SystemParameters.WorkArea.Height - Height) / 2;

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

            menu.Items.Add(new Separator());
            var exit = new MenuItem { Header = "Zako\u0144cz" };
            exit.Click += (_, _) => Application.Current.Shutdown();
            menu.Items.Add(exit);

            rootBorder.ContextMenu = menu;
            MouseLeftButtonDown += HudWindow_MouseLeftButtonDown;

            // start listener by default
            ToggleListener();

            var fightPanel = CreateHudTable();
            var instancePanel = CreateHudTable();
            fightPanel.Margin = new Thickness(10, 0, 10, 5);
            instancePanel.Margin = new Thickness(10, 5, 10, 0);
            container.Children.Add(fightPanel);
            container.Children.Add(instancePanel);

            AddFightRows(fightPanel);
            AddInstanceRows(instancePanel);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => UpdateData();
            _timer.Start();

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

        private static string FormatNumber(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
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
                _fightExpValue.Text = FormatNumber(fight.EarnedExp);
                _fightPsychoValue.Text = FormatNumber(fight.EarnedPsycho);
                _fightGoldValue.Text = FormatNumber(fight.FoundGold);
                _fightDropValue.Text = FormatNumber(fight.DropValue);
            }

            var instance = StatsService.GetCurrentOrLastInstance(player);
            if (instance == null)
            {
                ClearInstance();
            }
            else
            {
                _instanceNameValue.Text = instance.Name;
                _instanceExpValue.Text = FormatNumber(instance.EarnedExp);
                _instancePsychoValue.Text = FormatNumber(instance.EarnedPsycho);
                _instanceGoldValue.Text = FormatNumber(instance.FoundGold);
                _instanceDropValue.Text = FormatNumber(instance.DropValue);
                _instanceDurationValue.Text = instance.Duration;

                var lastFinished = StatsService.GetLastFinishedInstance(player);
                bool finished = lastFinished != null && lastFinished.Id == instance.Id;
                _instanceDurationValue.Foreground = finished ? Brushes.LightGreen : Brushes.White;
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

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            _listener?.Stop();
            _fightsWindow?.Close();
            _instancesWindow?.Close();
            base.OnClosed(e);
        }
    }
}
