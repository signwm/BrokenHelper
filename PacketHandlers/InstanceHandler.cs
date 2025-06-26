using System;
using System.Collections.Generic;
using System.Linq;
using BrokenHelper;

namespace BrokenHelper.PacketHandlers
{
    internal class InstanceHandler
    {
        private int? _currentInstanceId;
        private HashSet<string>[] _currentGroupProgress = Preferences.BossGroups.Select(g => new HashSet<string>()).ToArray();
        private readonly Dictionary<string, int> _currentMultiKillCounts = new();

        public int? CurrentInstanceId => _currentInstanceId;

        public void LoadOpenInstance(Models.GameDbContext context)
        {
            var openInstance = context.Instances.FirstOrDefault(i => i.EndTime == null);
            if (openInstance != null)
            {
                _currentInstanceId = openInstance.Id;
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
            _currentGroupProgress = Preferences.BossGroups.Select(g => new HashSet<string>()).ToArray();
            _currentMultiKillCounts.Clear();
        }

        public void CheckInstanceCompletion(IEnumerable<string> opponentNames, DateTime fightTime, Models.GameDbContext context)
        {
            if (_currentInstanceId == null)
                return;

            foreach (var name in opponentNames)
            {
                if (Preferences.MultiKillBosses.TryGetValue(name, out var required))
                {
                    _currentMultiKillCounts.TryGetValue(name, out var count);
                    count++;
                    _currentMultiKillCounts[name] = count;
                    if (count >= required)
                    {
                        CloseCurrentInstance(fightTime, context);
                    }
                    continue;
                }

                bool grouped = false;
                for (int i = 0; i < Preferences.BossGroups.Length; i++)
                {
                    if (Preferences.BossGroups[i].Contains(name))
                    {
                        _currentGroupProgress[i].Add(name);
                        if (_currentGroupProgress[i].Count == Preferences.BossGroups[i].Length)
                        {
                            CloseCurrentInstance(fightTime, context);
                        }
                        grouped = true;
                        break;
                    }
                }

                if (!grouped && Preferences.SingleBosses.Contains(name))
                {
                    CloseCurrentInstance(fightTime, context);
                }
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
            }

            _currentInstanceId = null;
            _currentMultiKillCounts.Clear();
            _currentGroupProgress = Preferences.BossGroups.Select(g => new HashSet<string>()).ToArray();
        }
    }
}
