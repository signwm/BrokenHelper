using System;
using System.Windows;

namespace BrokenHelper
{
    public partial class CreateInstanceWindow : Window
    {
        public string InstanceName => nameBox.Text;
        public int Difficulty => int.TryParse(difficultyBox.Text, out var d) ? d : 0;
        public DateTime StartTime => DateTime.TryParse(startBox.Text, out var s) ? s : DateTime.Now;
        public DateTime? EndTime => DateTime.TryParse(endBox.Text, out var e) ? e : (DateTime?)null;

        public CreateInstanceWindow(string defaultName, DateTime start, DateTime end)
        {
            InitializeComponent();
            nameBox.Text = defaultName;
            difficultyBox.Text = "1";
            startBox.Text = start.ToString("yyyy-MM-dd HH:mm:ss");
            endBox.Text = end.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
