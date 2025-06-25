using System.IO;
using System.Windows;
using BrokenHelper.Models;

namespace BrokenHelper
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.CreateDirectory("data");
            using var context = new GameDbContext();
            context.Database.EnsureCreated();
        }
    }
}
