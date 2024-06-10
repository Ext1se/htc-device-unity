
using TMPro;
using UnityEngine;
using VIVE_Trackers;

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
        IVIVEDongle trackers;

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
        }

        private void OnApplicationQuit()
        {
            if (trackers != null)
            {
                trackers.CloseApplication();
                trackers = null; 
            }
        }

        private void OnDestroy()
        {
            if (trackers != null)
            {
                trackers.CloseApplication();
                trackers = null; 
            }
        }

        #endregion


        #region Calls To Native Android
        
        public void OpenChannelForScan()
        {
            trackers.OpenChannelForScan();
        }
        public void CloseChannelForScan()
        {
            trackers.CloseChannelForScan();
        }
        #endregion
    }
}