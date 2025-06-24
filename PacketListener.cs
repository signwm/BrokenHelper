using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BrokenHelper
{
    internal class PacketListener
    {
        private ICaptureDevice? _device;
        private readonly List<byte> _buffer = new();
        private readonly string _dataPath = Path.Combine("data", "packets");

        public void Start()
        {
            Directory.CreateDirectory(_dataPath);
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
                    HandleInstanceMessage(rest);
                }
                else if (prefix == "36;0;")
                {
                    HandlePriceMessage(rest);
                }
            }
        }

        private void HandleInstanceMessage(string message)
        {
            var parts = message.Split('$');
            if (parts.Length <= 9)
                return;

            if (!string.IsNullOrEmpty(parts[7]))
                return;

            var publicId = parts[4];
            using var context = new Models.GameDbContext();

            if (context.Instances.Any(i => i.PublicId == publicId))
                return;

            var startTime = DateTime.Now;

            var openInstances = context.Instances.Where(i => i.EndTime == null).ToList();
            foreach (var inst in openInstances)
            {
                inst.EndTime = startTime;
            }

            var instance = new Models.InstanceEntity
            {
                PublicId = publicId,
                Name = parts[9],
                Difficulty = int.TryParse(parts[3], out var diff) ? diff : 0,
                StartTime = startTime,
                EndTime = null
            };

            context.Instances.Add(instance);
            context.SaveChanges();
        }

        private void HandlePriceMessage(string message)
        {
            var parts = message.Split("[&&]", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            using var context = new Models.GameDbContext();

            foreach (var part in parts)
            {
                var fields = part.Split(',');
                if (fields.Length < 3)
                    continue;

                var name = fields[1];
                if (!int.TryParse(fields[2], out var value))
                    continue;

                var existing = context.Prices.FirstOrDefault(p => p.Name == name);
                if (existing == null)
                {
                    context.Prices.Add(new Models.PriceEntity
                    {
                        Name = name,
                        Value = value
                    });
                }
                else
                {
                    existing.Value = value;
                }
            }

            context.SaveChanges();
        }
    }
}
