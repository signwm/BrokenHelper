using System.Media;

namespace BrokenHelper.Helpers
{
    internal static class SoundHelper
    {
        public static void PlayBeep()
        {
            try
            {
                // SystemSounds.Beep.Play();
                // SystemSounds.Asterisk.Play();
                // SystemSounds.Exclamation.Play();
                // SystemSounds.Hand.Play();
                // SystemSounds.Question.Play();


            }
            catch
            {
                // ignore sound errors
            }
        }
    }
}
