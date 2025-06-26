using System;
using BrokenHelper.PacketHandlers;

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

            if (prefix == "1;118;")
            {
                SafeHandle(() => _instanceHandler.HandleInstanceMessage(rest, time), prefix);
            }
            else if (prefix == "3;19;")
            {
                SafeHandle(() => _fightHandler.HandleFightMessage(rest, time), prefix);
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
                Console.WriteLine($"Error handling packet {prefix}: {ex.Message}");
            }
        }
    }
}
