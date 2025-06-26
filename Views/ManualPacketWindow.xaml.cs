using System;
using System.Windows;
using System.Windows.Controls;

namespace BrokenHelper
{
    public partial class ManualPacketWindow : Window
    {
        private readonly DateTime _openTime;
        public string Prefix => (prefixBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        public string Message => messageBox.Text;
        public DateTime Time => timePicker.Value;

        public ManualPacketWindow()
        {
            InitializeComponent();
            _openTime = DateTime.Now;
            timePicker.Value = _openTime;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
