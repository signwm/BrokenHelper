using System.IO;
using System.Windows;
using BrokenHelper.Models;

namespace BrokenHelper
{
    public partial class App : Application
    {
        private TrayIcon? _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.CreateDirectory("data");
            using var context = new GameDbContext();
            context.Database.EnsureCreated();

            //StatsService.RecalculateDropPrices();

            _tray = new TrayIcon();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            base.OnExit(e);
        }
    }
}
