using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace BrokenHelper
{
    public partial class LogsWindow : Window
    {
        private readonly ObservableCollection<string> _prefixes = [];

        public LogsWindow()
        {
            InitializeComponent();
            prefixList.ItemsSource = _prefixes;
            Logger.LogAdded += Logger_LogAdded;
            Loaded += (_, _) => LoadInitial();
            Closed += (_, _) => Logger.LogAdded -= Logger_LogAdded;
        }

        private void LoadInitial()
        {
            foreach (var entry in Logger.Entries)
            {
                EnsurePrefix(entry.Prefix);
            }
            UpdateLogs();
        }

        private void Logger_LogAdded(LogEntry entry)
        {
            Dispatcher.Invoke(() =>
            {
                EnsurePrefix(entry.Prefix);
                if (ShouldShow(entry.Prefix))
                {
                    AppendEntry(entry);
                }
            });
        }

        private bool ShouldShow(string prefix)
        {
            if (prefixList.SelectedItems.Count == 0)
                return false;
            return prefixList.SelectedItems.Cast<string>().Contains(prefix);
        }

        private void EnsurePrefix(string prefix)
        {
            if (!_prefixes.Contains(prefix))
            {
                _prefixes.Add(prefix);
                prefixList.SelectedItems.Add(prefix);
            }
        }

        private void AppendEntry(LogEntry entry)
        {
            var p = new Paragraph { Margin = new Thickness(0) };
            p.Inlines.Add(new Run(entry.Time.ToString("HH:mm:ss.fff") + " ") { Foreground = Brushes.Gray });
            p.Inlines.Add(new Run(entry.Prefix + " ") { Foreground = Brushes.Gray });
            p.Inlines.Add(new Run(entry.Message));
            logBox.Document.Blocks.Add(p);
            if (autoScroll.IsChecked == true)
                logBox.ScrollToEnd();
        }

        private void prefixList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateLogs();
        }

        private void UpdateLogs()
        {
            logBox.Document.Blocks.Clear();
            var selected = prefixList.SelectedItems.Cast<string>().ToList();
            if (selected.Count == 0)
                return;

            foreach (var entry in Logger.Entries.Where(l => selected.Contains(l.Prefix)))
            {
                AppendEntry(entry);
            }
        }
    }
}
