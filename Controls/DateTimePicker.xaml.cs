using System;
using System.Windows;
using System.Windows.Controls;

namespace BrokenHelper
{
    public partial class DateTimePicker : UserControl
    {
        public DateTimePicker()
        {
            InitializeComponent();
        }

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
            }
        }

        private void DatePart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // keep Value in sync if needed
        }

        private void TimePart_TextChanged(object sender, TextChangedEventArgs e)
        {
            // keep Value in sync if needed
        }
    }
}
