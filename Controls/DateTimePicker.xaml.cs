using System;
using System.Windows.Controls;

namespace BrokenHelper
{
    public partial class DateTimePicker : UserControl
    {
        public DateTimePicker()
        {
            InitializeComponent();
        }

        public event EventHandler? ValueChanged;

        public DateTime Value
        {
            get
            {
                var date = datePart.SelectedDate ?? DateTime.Now.Date;
                var time = TimeSpan.TryParse(timePart.Text, out var t) ? t : TimeSpan.Zero;
                return date.Date + time;
            }
            set
            {
                datePart.SelectedDate = value.Date;
                timePart.Text = value.ToString("HH:mm:ss");
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DatePart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TimePart_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
