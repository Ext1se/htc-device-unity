//#define SHOW_DUMP

using HID_ViveTest.PythonLike;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static VIVE_Trackers.TrackerDeviceInfo;

namespace VIVE_Trackers
{
    public class AndroidDongleHID : DongleHID
    {
        const string pluginName = "atom.mining.usblibrary.core.UnityAPI";//"com.ext1se.unity_activity.DonglePluginActivity";

        private bool isStarted = false;

        private AndroidJavaClass _unityClass;
        private AndroidJavaObject _unityActivity;
        private AndroidJavaObject _pluginInstance;
        private bool isDisposed = false;
        bool isInit = false;
        public override bool IsInit => isInit && _pluginInstance != null;

        public AndroidDongleHID()
        {
            tick_periodic = 0;
            sw = Stopwatch.StartNew();
            last_host_map_ask_ms = sw.ElapsedMilliseconds;
            wifi_info = WIFI_Info.Load();
        }

        public override void Init()
        {
            isInit = false;
            var _unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = _unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            _pluginInstance = new AndroidJavaObject(pluginName);
            if (_pluginInstance == null)
            {
                Log.dongleAPILogger?.WriteLine("Plugin Instance Error");
                return;
            }

            _pluginInstance.Call("setContext", _unityActivity);
            Log.dongleAPILogger?.WriteLine("Call 'prepare'");
            if (!_pluginInstance.Call<bool>("prepare"))
            {
                Log.dongleAPILogger?.WriteLine("Plugin Instance Error");
                _pluginInstance = null;
                return;
            }

            Log.dongleAPILogger?.WriteLine("Call 'startReader'");
            _pluginInstance.Call("startReader");
            isInit = true;
            // загружаем ранее привязанные устройства
            InitTrackers(this);
            var dev_host = GetHost();
            if (dev_host != null)
            {
                current_host_indx = dev_host.CurrentIndex;
            }
        }
        protected override void DoLoop()
        {
            if (isDisposed || !isInit) return;
            if (_pluginInstance == null)
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
                var sdata = _pluginInstance.Call<sbyte[]>("read"); //_pluginInstance.Call<sbyte[]>("Read");
                if (sdata != null && sdata.Length > 0)
                {
                    resp = new byte[sdata.Length];
                    Buffer.BlockCopy(sdata, 0, resp, 0, resp.Length);
                }
                else
                    return;
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

            int c = resp.Length / 64;
            for (int i = 0; i < c; i++)
                ParseUSBData(resp.Skip(i * 64).Take(64).ToArray());
        }

        protected override byte[] send_cmd(byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false)
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
            output.AddRange(StructConverter.Pack("<BB", cmd_id, (byte)(data.Length + 1)));
            output.AddRange(data);
            output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65
            byte[] result = new byte[0];
            try
            {
                SetFeature(output.ToArray());
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
                    var resp = GetFeature();
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
        protected override byte[] send_cmd_raw(byte[] data, bool showLog, bool waitAnswer = false)
        {
            if (OnlyListenerMode)
            {
                Log.dongleAPILogger?.WarningLine("Send command is blocked");
                return new byte[0];
            }
            int BUFFER_SIZE = 0x41;
            List<byte> output = new List<byte>(BUFFER_SIZE); // 65 byte for command
            output.AddRange(data);
            output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65
            byte[] result = new byte[0];
            try
            {
                SetFeature(output.ToArray());
                if (!waitAnswer) return result;
                for (int i = 0; i < 10; i++)
                {
                    var resp = GetFeature();
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

        void SetFeature(byte[] data)
        {
            sbyte[] s_data = new sbyte[data.Length];
            Buffer.BlockCopy(data, 0, s_data, 0, s_data.Length);
            _pluginInstance.Call("setFeature", s_data);
        }
        byte[] GetFeature()
        {
            return _pluginInstance.Call<byte[]>("getFeature", 64);
        }

        protected override void SaveTrackerInfo(TrackerDeviceInfo info)
        {
            if (info != null)
                info.SaveToFile();
            else
            {
                if (File.Exists(Path.Combine(Application.persistentDataPath, "trackers.json")))
                    File.Delete(Path.Combine(Application.persistentDataPath, "trackers.json"));
            }
        }

        public override void Dispose()
        {
            isDisposed = true;
        }
    }
}
