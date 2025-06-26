using System.Media;
using System.IO;

namespace BrokenHelper.Helpers
{
    internal static class SoundHelper
    {
        private static readonly string _resourceDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

        private static void PlaySound(string fileName)
        {
            try
            {
                var path = Path.Combine(_resourceDir, fileName);
                if (File.Exists(path))
                {
                    using var player = new SoundPlayer(path);
                    player.Play();
                }
            }
            catch
            {
                // ignore sound errors
            }
        }

        public static void PlayBeep() => PlaySound("start_round.wav");

        public static void PlayInstanceEnded() => PlaySound("instance_ended.wav");
    }
}
