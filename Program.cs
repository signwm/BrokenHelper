using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BrokenHelper.Models;

namespace BrokenHelper
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Directory.CreateDirectory("data");
            using (var context = new GameDbContext())
            {
                context.Database.EnsureCreated();
            }

            Application.Run(new MainForm());
        }
    }
}
