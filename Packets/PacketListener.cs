using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrokenHelper.PacketHandlers;


namespace BrokenHelper
{
    internal class PacketListener
    {
        // configuration values are loaded from GameConfig

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
            bool opened = false;
            try
            {
                _device.Open(DeviceModes.Promiscuous, 1000);
                opened = true;
                _device.Filter = "tcp src port 9365";
                _device.StartCapture();
            }
            catch (PcapException)
            {
                if (opened)
                    _device.Close();
                _device.OnPacketArrival -= OnPacketArrival;
                _device = null;
                throw;
            }
            catch (Exception)
            {
                if (opened)
                    _device.Close();
                _device.OnPacketArrival -= OnPacketArrival;
                _device = null;
                throw;
            }
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
            var packet = Packet.ParsePacket(raw.LinkLayerType, [.. raw.Data]);
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

                var message = Encoding.UTF8.GetString([.. messageBytes]);
                var firstSemi = message.IndexOf(';');
                if (firstSemi < 0)
                    continue;
                var secondSemi = message.IndexOf(';', firstSemi + 1);
                if (secondSemi < 0)
                    continue;

                var prefix = message.Substring(0, secondSemi + 1); // e.g. 3;19;
                var rest = message.Substring(secondSemi + 1);
                var now = DateTime.Now;

                if (PacketProcessor.RelevantPrefixes.Contains(prefix))
                {
                    var line = $"{now:O} ||| {prefix} ||| {rest} ";
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
                else
                {
                    var fileName = prefix.Replace(';', '_').TrimEnd('_') + ".txt";
                    var filePath = Path.Combine(_dataPath, fileName);
                    var line = $"{now:O} ||| {rest}";
                    File.AppendAllText(filePath, line + Environment.NewLine);
                }

                PacketProcessor.Process(prefix, rest, now, _instanceHandler, _fightHandler);
            }
        }
    }
}
