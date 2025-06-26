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

        private ICaptureDevice? _device;
        private readonly List<byte> _buffer = new();
        private readonly string _dataPath = Path.Combine("data", "packets");
        private readonly InstanceHandler _instanceHandler = new();
        private readonly FightHandler _fightHandler;

        public PacketListener()
        {
            _fightHandler = new FightHandler(_instanceHandler);
        }

        public void Start()
        {
            Directory.CreateDirectory(_dataPath);
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
                rest = rest.Replace("%20", " ");

                var fileName = prefix.Replace(';', '_').TrimEnd('_') + ".txt";
                var filePath = Path.Combine(_dataPath, fileName);
                var line = $"{rest} {DateTime.Now:O}";
                File.AppendAllText(filePath, line + Environment.NewLine);

                if (prefix == "1;118;")
                {
                    SafeHandle(() => _instanceHandler.HandleInstanceMessage(rest), prefix);
                }
                else if (prefix == "3;2;")
                {
                    if (Preferences.SoundSignals)
                        SoundHelper.PlayBeep();
                }
                else if (prefix == "3;19;")
                {
                    SafeHandle(() => _fightHandler.HandleFightMessage(rest), prefix);
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
