using HID_ViveTest.PythonLike;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UnityService
{
    public class UnityActivityPlugins : MonoBehaviour
    {
        const int MAX_TRACKER_COUNT = 5;
        [SerializeField] private TextMeshProUGUI _fromAndroidText;
        [SerializeField] private TextMeshProUGUI _fromIntentText;
        [SerializeField] private TMP_InputField _inputField;

        private AndroidJavaObject _unityActivity;
        private AndroidJavaObject _intent;

        private byte[] _currentData;
        private long _currentTime;

        #region MonoBehaviour

        private void Start()
        {
            InitAndroidPlugin("com.ext1se.unity_activity.PluginActivity");
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
            _unityActivity = new AndroidJavaObject(pluginName);
            if (_unityActivity == null)
            {
                Debugger.Log("Plugin Instance Error");
            }
            else
            {
                AndroidJavaObject currentActivity = _unityActivity.GetStatic<AndroidJavaObject>("currentUnityActivity");
                _intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            }
        }

        #endregion


        #region Calls To Native Android

        public void InitAndroidServices()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("initAndroidServices", "message");
            }
        }

        public void PrepareDevices()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("prepareDeviceList");
            }
        }

        public void ShowDevices()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("showListOfDevices");
            }
        }

        public void SelectHTCDevice()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("selectHtcDevice");
            }
        }

        //TODO: check data
        public void SendDataToHTCDevice()
        {
            if (_unityActivity != null)
            {
                string message = _inputField.text;
                //_unityActivity.Call("sendStringDataToDevice", message);
                _unityActivity.Call("sendDataToDevice", Encoding.ASCII.GetBytes(message));
            }
        }
        
        public void SendByteDataToHTCDevice()
        {
            if (_unityActivity != null)
            {
                //TODO: add bytes
                //byte[] bytes = new byte[]{};
                //_unityActivity.Call("sendDataToDevice", bytes);
                ApplicationStarted();
                OpenChannelForScan();
            }
        }

        public void ApplicationStarted()
        {
            var indexes = new int[0]; //GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x00;
            foreach (var item in indexes)
                flags[item] = 0x01;
            send_cmd(0x1e, null, true, false); //preambule
            send_cmd(0xff, StructConverter.Pack($"<BB", 0x00, 0x00), true);
            send_cmd(0xf0, StructConverter.Pack($"<BB", 0x12, 0x00), true);
            send_cmd(0x1d, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", 0x02, 0x01, flags, 0x00), true);
        }
        public void OpenChannelForScan()
        {
            var indexes = new int[0];  //GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            foreach (var item in indexes)
                flags[item] = 0x00;
            send_cmd(0x1e, null, true, false);
            send_cmd(0x1d, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", 0x00, 0x01, flags, 0x00), true);
        }

        byte[] send_cmd(byte cmd_id, byte data, bool showLog, bool waitAnswer = false) => send_cmd(cmd_id, new byte[] { data }, showLog, waitAnswer);
        byte[] send_cmd(byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false)
        {
            //if (OnlyListenerMode)
            //{
            //    Log.WarningLine("Send command is blocked");
            //    return new byte[0];
            //}
            if (data == null)
                data = new byte[0];
            int BUFFER_SIZE = 0x41;
            List<byte> output = new List<byte>(BUFFER_SIZE); // 65 byte for command
            output.AddRange(HID_ViveTest.PythonLike.StructConverter.Pack("<BBB", (byte)0x0, cmd_id, (byte)(data.Length + 1)));
            output.AddRange(data);
            output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65
            byte[] result = new byte[0];
            try
            {
                _unityActivity.Call("sendDataToDevice", output.ToArray());//stream.SetFeature(output.ToArray());
                if (!waitAnswer)
                {
                    if (showLog)
                    {
                        var str = Encoding.UTF8.GetString(result);
                        //Log.WarningLine($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
                        Debug.LogWarning($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
                    }
                    return result;
                }
                //for (int i = 0; i < 10; i++)
                //{
                //    var resp = new byte[BUFFER_SIZE];
                //    stream.GetFeature(resp);
                //    var respData = ResponseStruct.Parse(resp, false);
                //    if (respData.err != 0)
                //    {
                //        Debug.LogError($"Got error response: {respData.err}");//Log.ErrorLine($"Got error response: {respData.err}");
                //        continue; //return new byte[0];
                //    }
                //    if (respData.cmd_id != cmd_id)
                //    {
                //        Debug.LogError($"Got error response (wrong commandID): {cmd_id}"); //Log.ErrorLine($"Got error response (wrong commandID): {cmd_id}");
                //        continue; //return new byte[0];
                //    }
                //    result = respData.ret;
                //}
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); //Log.ErrorLine(ex.ToString());
            }

            if (showLog)
            {
                var str = Encoding.UTF8.GetString(result);
                Debug.LogWarning($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})"); // Log.WarningLine($"[SEND ANSWER] <cmd:0x{cmd_id:X}, data:{data.ArrayToString(true)}> {BitConverter.ToString(result)} ... ({str})");
            }
            return result;
        }

        public void Add()
        {
            if (_unityActivity != null)
            {
                int result = _unityActivity.Call<int>("add", 5, 6);
                Debugger.Log($"Add result from Native Android: {result}");
            }
        }

        //TODO: check enum
        /// <summary>
        /// typeFormat:
        /// 0 - binary
        /// 1 - int
        /// 2 - hex
        /// 3 - string 
        /// </summary>
        /// <param name="typeFormat"></param>
        public void SetReceiveFormat(int typeFormat)
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("setReceiveFormat", typeFormat);
            }
        }

        public void ShowMessage()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("showMessage", "Hi from Unity!");
            }
        }


        public void ShowMessageWithTag()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("showMessageWithTag", "Hi from Unity!");
            }
        }

        #endregion


        #region Calls From Native

        public void GetDataFromNative(string data)
        {
            if (_fromAndroidText == null) return;
            var bytes = System.Convert.FromBase64String(data);
            if (bytes.Length > 0 && bytes[0] == 0 && bytes[1] == 0) return;
            _fromAndroidText.text = $"[{DateTime.Now}] length:{bytes.Length}\n" +
                $"{BitConverter.ToString(bytes)}";
        }

        #endregion
    }
}