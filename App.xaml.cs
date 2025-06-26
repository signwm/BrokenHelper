using System.IO;
using System.Windows;
using BrokenHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace BrokenHelper
{
    public partial class App : Application
    {
        private HudWindow? _hud;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.CreateDirectory("data");
            using var context = new GameDbContext();
            context.Database.EnsureCreated();
            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            context.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");


            var player = StatsService.GetDefaultPlayerName();
            _hud = new HudWindow(player);
            _hud.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hud?.Close();
            Preferences.Save();
            base.OnExit(e);
        }
    }
}
