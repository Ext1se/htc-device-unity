using HID_ViveTest.PythonLike;
using HidSharp;
using HidSharp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static VIVE_Trackers.TrackerDeviceInfo;

namespace VIVE_Trackers
{
    public class WindowsDongleHID : DongleHID
    {
        public const ushort VID_VIVE = 0x0bb4;
        public const ushort PID_DONGLE = 0x0350;
        private HidDevice device_hid1;
        private HidStream stream = null;

        public override bool IsInit => stream != null;
        private bool isStarted = false;
        private bool isDisposed = false;

        public WindowsDongleHID()
        {
            HidSharpDiagnostics.EnableTracing = true;
            HidSharpDiagnostics.PerformStrictChecks = true;
            wifi_info = WIFI_Info.Load();
        }
        public override void Init()
        {
            var list = DeviceList.Local;
            if (!list.TryGetHidDevice(out device_hid1, VID_VIVE, PID_DONGLE))
            {
                Log.dongleAPILogger?.ErrorLine("HID device VIVE DONGLE not found!");
                return;
            }
            if (!device_hid1.TryOpen(out stream))
            {
                Log.dongleAPILogger?.ErrorLine("Can't open device stream!");
                return;
            }
            sw = Stopwatch.StartNew();
            last_host_map_ask_ms = sw.ElapsedMilliseconds;

            // загружаем ранее привязанные устройства
            InitTrackers(this);

            var dev_host = GetHost();
            if (dev_host != null)
            {
                current_host_indx = dev_host.CurrentIndex;
            }

            stream.ReadTimeout = int.MaxValue;
        }

        protected override void DoLoop()
        {
            if (isDisposed) return;
            if (!IsInit)
            {
                isStarted = false;
                return; 
            }
            if (!isStarted)
            {
                ApplicationStarted();
                isStarted = true;
            }
            byte[] resp = new byte[0x400];
            try
            {
                var readCount = stream.Read(resp, 0, resp.Length);
                //UnityEngine.Debug.Log(">> " + readCount + " bytes");
                if (readCount == 0)
                    return;
                Array.Resize(ref resp, readCount);
            }
            catch
            {
                if (!isDisposed)
                {
                    // recreate device
                    Log.dongleAPILogger?.WarningLine("[RECREATE DEVICE CONNECTION]");
                    Init(); 
                }
                return;
            }
            if (isDisposed) return;
            int len = 65;
            int c = resp.Length / len;
            for (int i = 0; i < c; i++)
                ParseUSBData(resp.Skip(i * len).Take(len).ToArray());
        }

        protected override void SaveTrackerInfo(TrackerDeviceInfo info)
        {
            if (info != null)
                info.SaveToFile();
            else
            {
                if (File.Exists("trackers.json"))
                    File.Delete("trackers.json");
            }
        }

        protected override byte[] send_cmd(byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false)
        {
            return send_cmd(stream, cmd_id, data, showLog, waitAnswer);
        }
        protected override byte[] send_cmd_raw(byte[] data, bool showLog, bool waitAnswer = false)
        {
            return send_cmd_raw(stream, data, showLog, waitAnswer);
        }

        static byte[] send_cmd(HidStream stream, byte cmd_id, byte data, bool showLog, bool waitAnswer = false) => send_cmd(stream, cmd_id, new byte[] { data }, showLog, waitAnswer);
        static byte[] send_cmd(HidStream stream, byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false)
        {
            if (OnlyListenerMode)
            {
                Log.dongleAPILogger?.WarningLine("Send command is blocked");
                return new byte[0];
            }
            if (data == null)
                data = new byte[0];
            int BUFFER_SIZE = 0x41;
            List<byte> output = new List<byte>(BUFFER_SIZE); // 65 byte for command
            output.AddRange(StructConverter.Pack("<BBB", (byte)0x0, cmd_id, (byte)(data.Length + 1)));
            output.AddRange(data);
            output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65
            byte[] result = new byte[0];
            try
            {
                stream.SetFeature(output.ToArray());
                if (!waitAnswer)
                {
                    if (showLog)
                    {
                        var str = Encoding.UTF8.GetString(result);
                        Log.dongleAPILogger?.WarningLine($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
                    }
                    return result;
                }
                for (int i = 0; i < 10; i++)
                {
                    var resp = new byte[BUFFER_SIZE];
                    stream.GetFeature(resp);
                    var respData = ResponseStruct.Parse(resp, false);
                    if (respData.err != 0)
                    {
                        Log.dongleAPILogger?.ErrorLine($"Got error response: {respData.err}");
                        continue; //return new byte[0];
                    }
                    if (respData.cmd_id != cmd_id)
                    {
                        Log.dongleAPILogger?.ErrorLine($"Got error response (wrong commandID): {cmd_id}");
                        continue; //return new byte[0];
                    }
                    result = respData.ret;
                }
            }
            catch (Exception ex)
            {
                Log.dongleAPILogger?.ErrorLine(ex.ToString());
            }

            if (showLog)
            {
                var str = Encoding.UTF8.GetString(result);
                Log.dongleAPILogger?.WarningLine($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
            }
            return result;
        }
        static byte[] send_cmd_raw(HidStream stream, byte[] data, bool showLog, bool waitAnswer = false)
        {
            if (OnlyListenerMode)
            {
                Log.dongleAPILogger?.WarningLine("Send command is blocked");
                return new byte[0];
            }
            int BUFFER_SIZE = 0x41;
            List<byte> output = new List<byte>(BUFFER_SIZE); // 65 byte for command
            output.Add(0);
            output.AddRange(data);
            output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65
            byte[] result = new byte[0];
            try
            {
                stream.SetFeature(output.ToArray());
                if (!waitAnswer) return result;
                for (int i = 0; i < 10; i++)
                {
                    var resp = new byte[BUFFER_SIZE];
                    stream.GetFeature(resp);
                    var respData = ResponseStruct.Parse(resp, false);
                    if (respData.err != 0)
                    {
                        Log.dongleAPILogger?.ErrorLine($"Got error response: {respData.err}");
                        continue; //return new byte[0];
                    }
                    if (respData.cmd_id != data[0])
                    {
                        Log.dongleAPILogger?.ErrorLine($"Got error response (wrong commandID): {data[0]}");
                        continue; //return new byte[0];
                    }
                    result = respData.ret;
                }
            }
            catch (Exception ex)
            {
                Log.dongleAPILogger?.ErrorLine(ex.ToString());
            }

            if (showLog)
            {
                var str = Encoding.UTF8.GetString(result);
                Log.dongleAPILogger?.WarningLine($"[SEND RAW] <DataReq:{data.ArrayToString(true)}> DataResp:{BitConverter.ToString(result)} ... ({str})");
            }
            return result;
        }

        public override void Dispose()
        {
            isDisposed = true;
            //stream.Close();
            stream.Dispose();
            stream = null;
            device_hid1 = null;
        }
    }
}
