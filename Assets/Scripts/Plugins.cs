using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityService
{
    public class Plugins : MonoBehaviour
    {
        private AndroidJavaClass _unityClass;
        private AndroidJavaObject _unityActivity;
        private AndroidJavaObject _pluginInstance;

        private void Start()
        {
            InitAndroidPlugin("com.ext1se.service_utils.PluginInstance");
        }

        private void InitAndroidPlugin(string pluginName)
        {
            _unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = _unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            _pluginInstance = new AndroidJavaObject(pluginName);
            if (_pluginInstance == null)
            {
                Debugger.Log("Plugin Instance Error");
            }

            _pluginInstance.CallStatic("receiveUnityActivity", _unityActivity); 
            _pluginInstance.Call("initAndroidServices", "s");
        }

        public void ShowDevices()
        {
            if (_pluginInstance != null)
            {
                _pluginInstance.Call("PrepareDeviceList");
            }
        }
        
        public void Add()
        {
            if (_pluginInstance != null)
            {
                int result = _pluginInstance.Call<int>("Add", 5, 6);
                Debugger.Log($"Add result from Native Android: {result}");
            }
        }

        public void ShowToast()
        {
            if (_pluginInstance != null)
            {
                _pluginInstance.Call("Toast", "Hi from Unity!");
            }
        }
    }
}