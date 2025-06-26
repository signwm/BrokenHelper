using System.Collections.Generic;
using System;
using BrokenHelper.PacketHandlers;
using BrokenHelper.Helpers;

namespace BrokenHelper
{
    internal static class PacketProcessor
    {
        internal static readonly HashSet<string> RelevantPrefixes = new()
        {
            "1;118;",
            "3;1;",
            "3;19;",
            "5;5;",
            "36;0;",
            "50;0;"
        };

        public static void Process(string prefix, string rest, DateTime time,
            InstanceHandler instanceHandler, FightHandler fightHandler)
        {
            rest = rest.Replace("%20", " ");
            Logger.Add(prefix, rest, time);

            if (prefix == "1;118;")
            {
                SafeHandle(() => instanceHandler.HandleInstanceMessage(rest, time), prefix);
            }
            else if (prefix == "3;2;")
            {
                if (Preferences.SoundSignals)
                    SoundHelper.PlayAct();
            }
            else if (prefix == "3;1;")
            {
                SafeHandle(() => fightHandler.HandleFightStart(time), prefix);
            }
            else if (prefix == "3;19;")
            {
                SafeHandle(() => fightHandler.HandleFightSummary(rest, time), prefix);
            }
            else if (prefix == "5;5;")
            {
                if (Preferences.SoundSignals)
                    SoundHelper.PlayAct();
                SafeHandle(() => fightHandler.HandleFightEnd(time), prefix);
            }
            else if (prefix == "36;0;")
            {
                SafeHandle(() => PriceHandler.HandleItemPriceMessage(rest), prefix);
            }
            else if (prefix == "50;0;")
            {
                SafeHandle(() => PriceHandler.HandleArtifactPriceMessage(rest), prefix);
            }
        }

        private static void SafeHandle(Action action, string prefix)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling packet {prefix}: {ex.Message} ({ex.StackTrace})");
            }
        }
    }
}
