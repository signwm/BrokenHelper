using System;

namespace BrokenHelper
{
    internal static class GameEvents
    {
        public static event Action? FightStarted;
        public static event Action? FightSummary;
        public static event Action? FightEnded;
        public static event Action? InstanceStarted;
        public static event Action? PlayerDied;

        internal static void OnFightStarted()
        {
            FightStarted?.Invoke();
        }

        internal static void OnFightSummary()
        {
            FightSummary?.Invoke();
        }

        internal static void OnFightEnded()
        {
            FightEnded?.Invoke();
        }

        internal static void OnInstanceStarted()
        {
            InstanceStarted?.Invoke();
        }

        internal static void OnPlayerDied()
        {
            PlayerDied?.Invoke();
        }
    }
}
