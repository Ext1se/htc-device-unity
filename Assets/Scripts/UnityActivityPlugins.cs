using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityService
{
    public class UnityActivityPlugins : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _fromAndroidText;
        [SerializeField] private TextMeshProUGUI _fromIntentText;

        private AndroidJavaObject _unityActivity;
        private AndroidJavaObject _intent;

        private void Start()
        {
            InitAndroidPlugin("com.ext1se.unity_activity.PluginActivity");
        }

        private void Update()
        {
            if (_unityActivity == null || _intent == null)
            {
                _fromIntentText.text = $"intent = null";
                return;
            }

            //string data = _intent.Call<string>("getStringExtra", new object[] { "usb_data" });
            //_fromIntentText.text = $"{DateTime.Now}: {data}";

            byte[] data = _intent.Call<byte[]>("getByteArrayExtra", new object[] { "usb_raw_data" });
            if (data != null)
            {
                _fromIntentText.text = $"{DateTime.Now}: {data.Length}; {data} ";
            }
        }

        private void InitAndroidPlugin(string pluginName)
        {

            //AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            //AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            //AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            
            _unityActivity = new AndroidJavaObject(pluginName);
            
            //AndroidJavaObject currentActivity = _unityActivity.GetStatic<AndroidJavaObject>("currentActivity");
            //AndroidJavaObject currentActivity = _unityActivity.GetStatic<AndroidJavaObject>("UnityPlayer.currentActivity");
            //AndroidJavaObject currentActivity = _unityActivity.Get<AndroidJavaObject>("currentActivity");
            AndroidJavaObject currentActivity = _unityActivity.GetStatic<AndroidJavaObject>("currentActivity1");
            Debugger.Log("CurrentActivity =" + currentActivity);

            _intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            Debugger.Log("_intent =" + _intent);

            //_intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            if (_unityActivity == null)
            {
                Debugger.Log("Plugin Instance Error");
            }

            //_unityActivity.CallStatic("receiveUnityActivity", _unityActivity); 
            //_unityActivity.Call("initAndroidServices", "s");
        }

        public void InitAndroidServices()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("initAndroidServices", "message");
            }
        }

        public void ShowDevices()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("PrepareDeviceList");
            }
        }

        public void Add()
        {
            if (_unityActivity != null)
            {
                int result = _unityActivity.Call<int>("Add", 5, 6);
                Debugger.Log($"Add result from Native Android: {result}");
            }
        }

        public void ShowMessage()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("ShowMessage", "Hi from Unity!");
            }
        }


        public void ShowMessageWithTag()
        {
            if (_unityActivity != null)
            {
                _unityActivity.Call("ShowMessageWithTag", "Hi from Unity!");
            }
        }

        //

        public void GetDataFromNative(string data)
        {
            if (_fromAndroidText == null)
            {
                return;
            }

            _fromAndroidText.text = $"{DateTime.Now}: {data.Length}: {data}";
        }
    }
}