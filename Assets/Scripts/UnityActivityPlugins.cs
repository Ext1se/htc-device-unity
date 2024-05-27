using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityService
{
    public class UnityActivityPlugins : MonoBehaviour
    {
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
                byte[] bytes = new byte[]{};
                _unityActivity.Call("sendDataToDevice", bytes);
            }
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