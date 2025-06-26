namespace BrokenHelper.Helpers
{
    public static class TimeHelper
    {
        public static string FormatDuration(System.TimeSpan time)
        {
            int minutes = (int)time.TotalMinutes;
            int seconds = time.Seconds;
            return $"{minutes}:{seconds:00}";
        }
    }
}
