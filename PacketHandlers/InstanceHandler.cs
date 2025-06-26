using System;
using System.Collections.Generic;
using System.Linq;

namespace BrokenHelper.PacketHandlers
{
    internal class InstanceHandler
    {
        private int? _currentInstanceId;
        private HashSet<string>[] _currentGroupProgress = PacketListener.BossGroups.Select(g => new HashSet<string>()).ToArray();
        private readonly Dictionary<string, int> _currentMultiKillCounts = new();
        private bool _pendingClose;

        public int? CurrentInstanceId => _currentInstanceId;

        public void LoadOpenInstance(Models.GameDbContext context)
        {
            var openInstance = context.Instances.FirstOrDefault(i => i.EndTime == null);
            if (openInstance != null)
            {
                _currentInstanceId = openInstance.Id;
                _pendingClose = false;
            }
        }

        public void HandleInstanceMessage(string message, DateTime? time = null)
        {
            var parts = message.Split("[$]", StringSplitOptions.None);
            if (parts.Length <= 9)
                return;

            if (string.IsNullOrEmpty(parts[7]))
                return;

            var publicId = parts[4];
            using var context = new Models.GameDbContext();

            if (context.Instances.Any(i => i.PublicId == publicId))
                return;

            var startTime = time ?? DateTime.Now;

            var openInstances = context.Instances.Where(i => i.EndTime == null).ToList();
            foreach (var inst in openInstances)
            {
                inst.EndTime = startTime;
            }

            var instance = new Models.InstanceEntity
            {
                PublicId = publicId,
                Name = parts[10],
                Difficulty = int.TryParse(parts[3], out var diff) ? diff : 0,
                StartTime = startTime,
                EndTime = null
            };

            context.Instances.Add(instance);
            context.SaveChanges();

            _currentInstanceId = instance.Id;
            _pendingClose = false;
            _currentGroupProgress = PacketListener.BossGroups.Select(g => new HashSet<string>()).ToArray();
            _currentMultiKillCounts.Clear();
        }

        public void CheckInstanceCompletion(IEnumerable<string> opponentNames, DateTime fightTime, Models.GameDbContext context)
        {
            if (_currentInstanceId == null)
                return;

            foreach (var name in opponentNames)
            {
                if (PacketListener.MultiKillBosses.TryGetValue(name, out var required))
                {
                    _currentMultiKillCounts.TryGetValue(name, out var count);
                    count++;
                    _currentMultiKillCounts[name] = count;
                    if (count >= required)
                    {
                        _pendingClose = true;
                    }
                    continue;
                }

                bool grouped = false;
                for (int i = 0; i < PacketListener.BossGroups.Length; i++)
                {
                    if (PacketListener.BossGroups[i].Contains(name))
                    {
                        _currentGroupProgress[i].Add(name);
                        if (_currentGroupProgress[i].Count == PacketListener.BossGroups[i].Length)
                        {
                            _pendingClose = true;
                        }
                        grouped = true;
                        break;
                    }
                }

                if (!grouped && PacketListener.SingleBosses.Contains(name))
                {
                    _pendingClose = true;
                }
            }
        }

        public void CloseIfPending(DateTime time, Models.GameDbContext context)
        {
            if (_pendingClose)
            {
                CloseCurrentInstance(time, context);
                _pendingClose = false;
            }
        }

        public void CloseCurrentInstance(DateTime time, Models.GameDbContext context)
        {
            if (_currentInstanceId == null)
                return;

            var instance = context.Instances.FirstOrDefault(i => i.Id == _currentInstanceId.Value);
            if (instance != null && instance.EndTime == null)
            {
                instance.EndTime = time;
                context.SaveChanges();
                if (Preferences.SoundSignals)
                    Helpers.SoundHelper.PlayInstanceEnded();
            }

            _currentInstanceId = null;
            _currentMultiKillCounts.Clear();
            _currentGroupProgress = PacketListener.BossGroups.Select(g => new HashSet<string>()).ToArray();
            _pendingClose = false;
        }
    }
}
