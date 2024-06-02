using HID_ViveTest.PythonLike;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using VIVE_Trackers;
using static OVRPlugin;

namespace UnityService
{
    public class UnityActivityPlugins : MonoBehaviour
    {
        const int MAX_TRACKER_COUNT = 5;
        [SerializeField] private TextMeshProUGUI _fromAndroidText;
        [SerializeField] private TextMeshProUGUI _fromIntentText;
        [SerializeField] private TMP_InputField _inputField;

        private AndroidJavaClass _unityClass;
        private AndroidJavaObject _unityActivity;
        private AndroidJavaObject _pluginInstance;

        private byte[] _currentData;
        private long _currentTime;
        IAckable trackers;

        #region MonoBehaviour

        private void Start()
        {
            //InitAndroidPlugin("com.ext1se.unity_activity.DonglePluginActivity");
            trackers = new AndroidDongleHID();
            trackers.OnTrackerStatus += Trackers_OnTrackerStatus;
            trackers.OnConnected += Trackers_OnConnected;
            trackers.OnDisconnected += Trackers_OnDisconnected;
            trackers.OnTrack += Trackers_OnTrack;
            trackers.OnButtonClicked += Trackers_OnButtonClicked;
            trackers.Init();
        }

        private void Trackers_OnButtonClicked(int trackerIndx)
        {

        }

        private void Trackers_OnTrack(int trackerID, TrackData trackData, long time_delta)
        {
            Log.WriteLine($"{trackerID}, {trackData.pos_x}, {trackData.pos_y}, {trackData.pos_z}, {trackData.rot_x}, {trackData.rot_y}, {trackData.rot_z}, {trackData.rot_w}, {(int)trackData.status}");
        }

        private void Trackers_OnDisconnected(int indx)
        {
            Log.WriteLine($"Device indx:{indx} disconnected");
        }

        private void Trackers_OnConnected(int indx)
        {
            Log.WriteLine($"Device indx:{indx} connected");
        }

        private void Trackers_OnTrackerStatus(TrackerDeviceInfo device)
        {
            Log.WriteLine($"{device.CurrentIndex}, {device.Battery}, {(int)device.status}, {device.IsHost}");
        }

        //private void Update()
        //{
        //    if (_unityActivity == null || _intent == null)
        //    {
        //        _fromIntentText.text = $"intent = null";
        //        return;
        //    }

        //    byte[] data = _intent.Call<byte[]>("getByteArrayExtra", new object[] { "usb_raw_data" });
        //    long time = _intent.Call<long>("getLongExtra", new object[] { "usb_data_time", (long)-1 });
        //    if (time != _currentTime && data != null && data.Length > 0)
        //    {
        //        _currentData = data;
        //        _currentTime = time;

        //        _fromIntentText.text = $"{DateTime.Now}: {data.Length};\n{BitConverter.ToString(data)} ";
        //    }
        //    else
        //    {
        //        //_fromIntentText.text = $"Wait. Time : {time}; Data: {data}";
        //    }
        //}

        #endregion

        #region Core Activity

        private void InitAndroidPlugin(string pluginName)
        {
            var _unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = _unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            _pluginInstance = new AndroidJavaObject(pluginName);
            if (_pluginInstance == null)
            {
                Debugger.Log("Plugin Instance Error");
            }

            _pluginInstance.Call("Init", _unityActivity);
        }

        private void Update()
        {
            if (trackers.IsInit)
                trackers.DoLoop();
            //if (_pluginInstance != null)
            //{
            //    var sdata = _pluginInstance.Call<sbyte[]>("Read");
            //    if (sdata != null && sdata.Length > 0)
            //    {
            //        byte[] data = new byte[sdata.Length];
            //        Buffer.BlockCopy(sdata, 0, data, 0, data.Length);
            //        _fromAndroidText.text = System.BitConverter.ToString(data);
            //    }
            //}
        }

        private void OnApplicationQuit()
        {
            //if (_pluginInstance != null)
            //{
            //    _pluginInstance.Call("Close");
            //    _pluginInstance = null;
            //}
            if (trackers != null)
            {
                trackers.CloseApplication();
                trackers = null; 
            }
        }

        private void OnDestroy()
        {
            //if (_pluginInstance != null)
            //{
            //    _pluginInstance.Call("Close");
            //    _pluginInstance = null;
            //}
            if (trackers != null)
            {
                trackers.CloseApplication();
                trackers = null; 
            }
        }

        #endregion


        #region Calls To Native Android
        
        //public void SendByteDataToHTCDevice()
        //{
        //    if (_unityActivity != null)
        //    {
        //        ApplicationStarted();
        //        OpenChannelForScan();
        //    }
        //}

        public void ApplicationStarted()
        {
            //var indexes = new int[0]; //GetActiveIndexes();
            //byte[] flags = new byte[MAX_TRACKER_COUNT];
            //for (int i = 0; i < flags.Length; i++)
            //    flags[i] = 0x00;
            //foreach (var item in indexes)
            //    flags[item] = 0x01;
            //send_cmd(0x1e, null, true, false); //preambule
            //send_cmd(0xff, StructConverter.Pack($"<BB", 0x00, 0x00), true);
            //send_cmd(0xf0, StructConverter.Pack($"<BB", 0x12, 0x00), true);
            //send_cmd(0x1d, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", 0x02, 0x01, flags, 0x00), true);
        }
        public void OpenChannelForScan()
        {
            //var indexes = new int[0];  //GetActiveIndexes();
            //byte[] flags = new byte[MAX_TRACKER_COUNT];
            //for (int i = 0; i < flags.Length; i++)
            //    flags[i] = 0x1;
            //foreach (var item in indexes)
            //    flags[item] = 0x00;
            //send_cmd(0x1e, null, true, false);
            //send_cmd(0x1d, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", 0x00, 0x01, flags, 0x00), true);
            trackers.OpenChannelForScan();
        }
        public void CloseChannelForScan()
        {
            //var indexes = new int[0];
            //byte[] flags = new byte[MAX_TRACKER_COUNT];
            //for (int i = 0; i < flags.Length; i++)
            //    flags[i] = 0x1;
            //foreach (var item in indexes)
            //    flags[item] = 0x00;
            //send_cmd(0x1e, null, true, false);
            //send_cmd(0x1d, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", 0x02, 0x01, flags, 0x00), true);

            trackers.CloseChannelForScan();
        }

        //byte[] send_cmd(byte cmd_id, byte data, bool showLog, bool waitAnswer = false) => send_cmd(cmd_id, new byte[] { data }, showLog, waitAnswer);
        //byte[] send_cmd(byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false)
        //{
        //    if (data == null)
        //        data = new byte[0];
        //    int BUFFER_SIZE = 0x41;
        //    List<byte> output = new List<byte>(BUFFER_SIZE); // 65 byte for command
        //    output.AddRange(StructConverter.Pack("<BBB", (byte)0x0, cmd_id, (byte)(data.Length + 1)));
        //    output.AddRange(data);
        //    output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65
        //    byte[] result = new byte[0];
        //    try
        //    {
        //        sbyte[] sbytes = new sbyte[output.Count];
        //        Buffer.BlockCopy(output.ToArray(), 0, sbytes, 0, sbytes.Length);
        //        _pluginInstance.Call("Write", sbytes);//stream.SetFeature(output.ToArray());
        //        if (!waitAnswer)
        //        {
        //            if (showLog)
        //            {
        //                var str = Encoding.UTF8.GetString(result);
        //                Debug.LogWarning($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
        //            }
        //            return result;
        //        }
        //        //for (int i = 0; i < 10; i++)
        //        //{
        //        //    var resp = new byte[BUFFER_SIZE];
        //        //    stream.GetFeature(resp);
        //        //    var respData = ResponseStruct.Parse(resp, false);
        //        //    if (respData.err != 0)
        //        //    {
        //        //        Debug.LogError($"Got error response: {respData.err}");//Log.ErrorLine($"Got error response: {respData.err}");
        //        //        continue; //return new byte[0];
        //        //    }
        //        //    if (respData.cmd_id != cmd_id)
        //        //    {
        //        //        Debug.LogError($"Got error response (wrong commandID): {cmd_id}"); //Log.ErrorLine($"Got error response (wrong commandID): {cmd_id}");
        //        //        continue; //return new byte[0];
        //        //    }
        //        //    result = respData.ret;
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogException(ex); //Log.ErrorLine(ex.ToString());
        //    }

        //    if (showLog)
        //    {
        //        var str = Encoding.UTF8.GetString(result);
        //        Debug.LogWarning($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})"); // Log.WarningLine($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
        //    }
        //    return result;
        //}
        #endregion


        #region Calls From Native

        //public void GetDataFromNative(string data)
        //{
        //    if (_fromAndroidText == null) return;
        //    var bytes = System.Convert.FromBase64String(data);
        //    if (bytes.Length > 0 && bytes[0] == 0 && bytes[1] == 0) return;
        //    _fromAndroidText.text = $"[{DateTime.Now}] length:{bytes.Length}\n" +
        //        $"{BitConverter.ToString(bytes)}";
        //}

        #endregion
    }
}