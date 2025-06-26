using System.Windows;
using System.Windows.Controls;

namespace BrokenHelper
{
    public partial class ManualPacketWindow : Window
    {
        public string Prefix => (prefixBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        public string Message => messageBox.Text;

        public ManualPacketWindow()
        {
            InitializeComponent();
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
