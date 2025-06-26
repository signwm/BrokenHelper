using System.Media;

namespace BrokenHelper.Helpers
{
    internal static class SoundHelper
    {
        public static void PlayBeep()
        {
            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
                // ignore sound errors
            }
        }
    }
}
