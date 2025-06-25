using System.Windows;

namespace BrokenHelper
{
    public partial class MainWindow : Window
    {
        private PacketListener? _listener;
        private HudWindow? _hud;
        private FightsDashboardWindow? _fightsDashboard;
        private InstancesDashboardWindow? _instancesDashboard;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listener == null)
            {
                _listener = new PacketListener();
                _listener.Start();
                startStopButton.Content = "Stop";
                statusLabel.Content = "Listening";
            }
            else
            {
                _listener.Stop();
                _listener = null;
                startStopButton.Content = "Start";
                statusLabel.Content = "Stopped";
            }
        }

        private void HudCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (hudCheckBox.IsChecked == true)
            {
                var player = StatsService.GetDefaultPlayerName();
                _hud = new HudWindow(player);
                _hud.Show();
            }
            else
            {
                _hud?.Close();
                _hud = null;
            }
        }

        private void FightsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _fightsDashboard ??= new FightsDashboardWindow();
            _fightsDashboard.Show();
            _fightsDashboard.Activate();
        }

        private void InstancesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _instancesDashboard ??= new InstancesDashboardWindow();
            _instancesDashboard.Show();
            _instancesDashboard.Activate();
        }
    }
}
