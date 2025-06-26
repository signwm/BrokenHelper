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

            PacketProcessor.Process(prefix, rest, time, _instanceHandler, _fightHandler);
        }

    }
}
