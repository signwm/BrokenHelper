using System;

namespace BrokenHelper
{
    internal static class GameEvents
    {
        public static event Action? FightStarted;
        public static event Action? FightSummary;

        internal static void OnFightStarted()
        {
            FightStarted?.Invoke();
        }

        internal static void OnFightSummary()
        {
            FightSummary?.Invoke();
        }
    }
}
