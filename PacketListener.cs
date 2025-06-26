using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrokenHelper.Helpers;
using BrokenHelper.PacketHandlers;

namespace BrokenHelper
{
    internal class PacketListener
    {
        internal static readonly string[][] BossGroups = new[]
        {
            new[] { "Duch Ognia", "Duch Energii", "Duch Zimna" },
            new[] { "Babadek", "Gregorius", "Ghadira" },
            new[] { "Mahet", "Tarul" },
            new[] { "Lugus", "Morana" },
            new[] { "Fyodor", "Gmo" }
        };

        internal static readonly Dictionary<string, int> MultiKillBosses = new()
        {
            { "Konstrukt", 3 },
            { "Osłabiony Konstrukt", 3 }
        };

        internal static readonly HashSet<string> SingleBosses = [.. new[]
        {
            "Admirał Utoru", "Angwalf-Htaga", "Aqua Regis", "Bibliotekarz",
            "Draugul", "Duch Zamku", "Garthmog", "Geomorph", "Herszt",
            "Heurokratos", "Hvar", "Ichtion", "Ivravul", "Jaskółka",
            "Jastrzębior", "Krzyżak", "Modliszka", "Mortus", "Nidhogg",
            "Niedźwiedź", "Obserwator", "Ropucha", "Selena", "Sidraga",
            "Tygrys", "Utor Komandor", "Valdarog", "Vidvar", "Vough",
            "Wendigo", "Władca Marionetek"
        }];

        internal static readonly int[,] QuoteItemCoefficients = new int[,]
        {
            { 4, 4, 4, 20, 20, 20, 67, 67, 67, 351, 351, 351 },
            { 7, 7, 7, 27, 27, 27, 90, 90, 90, 585, 585, 585 },
            { 10, 10, 10, 54, 54, 54, 180, 180, 180, 1170, 1170, 1170 },
            { 16, 16, 16, 81, 81, 81, 270, 270, 270, 1755, 1755, 1755 },
            { 27, 27, 27, 135, 135, 135, 450, 450, 450, 2925, 2925, 2925 },
            { 40, 40, 40, 202, 202, 202, 675, 675, 675, 4680, 4680, 4680 },
            { 67, 67, 67, 337, 337, 337, 1125, 1125, 1125, 7605, 7605, 7605 },
            { 135, 135, 135, 675, 675, 675, 2250, 2250, 2250, 14625, 14625, 14625 },
            { 337, 337, 337, 1687, 1687, 1687, 5850, 5850, 5850, 35100, 35100, 35100 }
        };

        private ICaptureDevice? _device;
        private readonly List<byte> _buffer = [];
        private readonly string _dataPath = Path.Combine("data", "packets");
        private readonly string _logPath = Path.Combine("data", "packet_log.txt");
        private readonly InstanceHandler _instanceHandler = new();
        private readonly FightHandler _fightHandler;

        public PacketListener()
        {
            _fightHandler = new FightHandler(_instanceHandler);
        }

        public void Start()
        {
            Directory.CreateDirectory(_dataPath);
            Directory.CreateDirectory(Path.Combine(_dataPath, "relevant"));
            Directory.CreateDirectory(Path.Combine(_dataPath, "other"));
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            using (var context = new Models.GameDbContext())
            {
                _instanceHandler.LoadOpenInstance(context);
            }

            var devices = CaptureDeviceList.Instance;
            _device = devices.FirstOrDefault(d => d.Description?.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) == true)
                      ?? devices.FirstOrDefault();
            if (_device == null)
                throw new InvalidOperationException("No capture device found");

            _device.OnPacketArrival += OnPacketArrival;
            _device.Open(DeviceModes.Promiscuous, 1000);
            _device.Filter = "tcp src port 9365";
            _device.StartCapture();
        }

        public void Stop()
        {
            if (_device != null)
            {
                _device.StopCapture();
                _device.Close();
                _device.OnPacketArrival -= OnPacketArrival;
                _device = null;
            }
        }

        private void OnPacketArrival(object sender, PacketCapture e)
        {
            var raw = e.GetPacket();
            var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data.ToArray());
            var tcp = packet.Extract<TcpPacket>();
            if (tcp == null)
                return;

            var data = tcp.PayloadData;
            if (data == null || data.Length == 0)
                return;
            if (data.Length == 3 && data[0] == 0x39 && data[1] == 0x39 && data[2] == 0x00)
                return;

            lock (_buffer)
            {
                _buffer.AddRange(data);
                ProcessBuffer();
            }
        }

        private void ProcessBuffer()
        {
            int index;
            while ((index = _buffer.IndexOf(0x00)) >= 0)
            {
                if (index == 0)
                {
                    _buffer.RemoveAt(0);
                    continue;
                }

                var messageBytes = _buffer.GetRange(0, index);
                _buffer.RemoveRange(0, index + 1); // remove message and zero byte

                var message = Encoding.UTF8.GetString(messageBytes.ToArray());
                var firstSemi = message.IndexOf(';');
                if (firstSemi < 0)
                    continue;
                var secondSemi = message.IndexOf(';', firstSemi + 1);
                if (secondSemi < 0)
                    continue;

                var prefix = message.Substring(0, secondSemi + 1); // e.g. 3;19;
                var rest = message.Substring(secondSemi + 1);

                var folder = PacketProcessor.RelevantPrefixes.Contains(prefix) ? "relevant" : "other";
                var fileName = prefix.Replace(';', '_').TrimEnd('_') + ".txt";
                var filePath = Path.Combine(_dataPath, folder, fileName);
                var now = DateTime.Now;
                var line = $"{rest} {now:O}";
                File.AppendAllText(filePath, line + Environment.NewLine);

                if (PacketProcessor.RelevantPrefixes.Contains(prefix))
                {
                    var logLine = $"{now:O} ||| {prefix} ||| {rest}";
                    File.AppendAllText(_logPath, logLine + Environment.NewLine);
                }

                PacketProcessor.Process(prefix, rest, now, _instanceHandler, _fightHandler);
            }
        }
    }
}
