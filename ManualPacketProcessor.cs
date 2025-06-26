using System;
using BrokenHelper.PacketHandlers;
using BrokenHelper.Helpers;

namespace BrokenHelper
{
    internal static class ManualPacketProcessor
    {
        private static readonly InstanceHandler _instanceHandler = new();
        private static readonly FightHandler _fightHandler = new(_instanceHandler);

        public static void Process(string prefix, string rest) => Process(prefix, rest, DateTime.Now);

        public static void Process(string prefix, string rest, DateTime time)
        {
            using (var context = new Models.GameDbContext())
            {
                _instanceHandler.LoadOpenInstance(context);
            }

            rest = rest.Replace("%20", " ");
            var snippet = rest.Length > 60 ? rest.Substring(0, 60) : rest;
            Logger.Add(prefix, rest, time);

            if (prefix == "1;118;")
            {
                SafeHandle(() => _instanceHandler.HandleInstanceMessage(rest, time), prefix);
            }
            else if (prefix == "3;2;")
            {
                if (Preferences.SoundSignals)
                    SoundHelper.PlayAct();
            }
            else if (prefix == "3;1;")
            {
                SafeHandle(() => _fightHandler.HandleFightStart(time), prefix);
            }
            else if (prefix == "3;19;")
            {
                SafeHandle(() => _fightHandler.HandleFightSummary(rest, time), prefix);
            }
            else if (prefix == "6;43;")
            {
                if (Preferences.SoundSignals)
                    SoundHelper.PlayAct();
                SafeHandle(() => _fightHandler.HandleFightEnd(time), prefix);
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
                // ignore or log elsewhere
            }
        }
    }
}
