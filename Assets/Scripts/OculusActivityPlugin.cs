using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VIVE_Trackers;

public enum CalibrationStatus
{
    Hand_in_front,
    Hand_up,
    Hand_down,
    Tracker_on_the_floor
}

public class OculusActivityPlugin : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] Transform leftAnchor;
    [SerializeField] Transform rightAnchor;
    [SerializeField] Button startScanBtn;
    [SerializeField] Button stopScanBtn;
    [SerializeField] Transform trackerPref;
    [SerializeField] RectTransform trackersParentUI;
    [SerializeField] TextMeshProUGUI statusText;

    Dictionary<int, Transform> trackers = new Dictionary<int, Transform>();
    IVIVEDongle dongleAPI;
    TrackerDeviceInfoView[] deviceViews;
    Dictionary<OVRInput.Controller, Transform> attachedTrackers = new Dictionary<OVRInput.Controller, Transform>();
    Transform currentController;
#if UNITY_EDITOR || UNITY_STANDALONE
    Thread dongleLoopThread;
#endif

    void Start()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        dongleAPI = new AndroidDongleHID(); 
#else
        dongleAPI = new WindowsDongleHID();
#endif
        dongleAPI.OnTrackerStatus += Trackers_OnTrackerStatus;
        dongleAPI.OnConnected += Trackers_OnConnected;
        dongleAPI.OnDisconnected += Trackers_OnDisconnected;
        dongleAPI.OnTrack += Trackers_OnTrack;
        dongleAPI.OnButtonClicked += Trackers_OnButtonClicked;
        dongleAPI.Init();

        if (trackersParentUI != null)
        {
            deviceViews = trackersParentUI.GetComponentsInChildren<TrackerDeviceInfoView>();
            foreach (var view in deviceViews)
            {
                view.DongleAPI = dongleAPI;
            }
        }

        startScanBtn.onClick.AddListener(() => dongleAPI.OpenChannelForScan());
        stopScanBtn.onClick.AddListener(() => dongleAPI.CloseChannelForScan());

#if UNITY_EDITOR || UNITY_STANDALONE
        dongleLoopThread = new Thread(ThreadDongleLoop);
        dongleLoopThread.Start();
#endif
    }

    void ThreadDongleLoop()
    {
        while (dongleAPI != null && dongleAPI.IsInit)
        {
            dongleAPI.DoLoop();
        }
        Log.WriteLine("Thread exited");
    }

    private void Update()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        if (dongleAPI.IsInit)
        {
            dongleAPI.DoLoop();
            if (OVRInput.Get(OVRInput.RawButton.X)) // calibrate left tracker
            {
                //var pos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
                currentController = leftAnchor;
            }
            if (OVRInput.Get(OVRInput.RawButton.A)) // calibrate right tracker
            {
                //var pos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                currentController = rightAnchor;
            }
        } 
#endif
    }

    private void OnApplicationQuit()
    {
        if (dongleAPI != null)
        {
            dongleAPI.CloseApplication();
            dongleAPI.Dispose();
            dongleAPI = null;
            Thread.Sleep(1000);
            dongleLoopThread.Abort();
            dongleLoopThread = null;
        }
    }

    private void OnDestroy()
    {
        if (dongleAPI != null)
        {
            dongleAPI.CloseApplication();
            dongleAPI.Dispose();
            dongleAPI = null;
            Thread.Sleep(1000);
            dongleLoopThread.Abort();
            dongleLoopThread = null;
        }
    }

    private void Trackers_OnButtonClicked(int trackerIndx)
    {
        if (currentController != null)
        {
            var offset = trackers[trackerIndx].position - currentController.position;
            trackers[trackerIndx].parent.position = -offset; 
        }
    }

    private void Trackers_OnTrack(int trackerIndx, TrackData trackData, long time_delta)
    {
        Log.WriteLine($"{trackerIndx}, {trackData.pos_x}, {trackData.pos_y}, {trackData.pos_z}, {trackData.rot_x}, {trackData.rot_y}, {trackData.rot_z}, {trackData.rot_w}, {(int)trackData.status}");

        InstantiateTracker(trackerIndx, false, TrackData.Status.None);
        trackers[trackerIndx].localPosition = new Vector3(trackData.pos_x, trackData.pos_y, -trackData.pos_z);
        trackers[trackerIndx].localRotation = new Quaternion(-trackData.rot_x, -trackData.rot_y, trackData.rot_z, trackData.rot_w);
    }

    private void Trackers_OnDisconnected(int trackerIndx)
    {
        Log.WriteLine($"Device indx:{trackerIndx} disconnected");
        if (trackers.ContainsKey(trackerIndx))
        {
            Destroy(trackers[trackerIndx].gameObject);
            trackers.Remove(trackerIndx);
        }
    }

    private void Trackers_OnConnected(int trackerIndx)
    {
        Log.WriteLine($"Device indx:{trackerIndx} connected");
        InstantiateTracker(trackerIndx, false, 0);
    }

    private void Trackers_OnTrackerStatus(TrackerDeviceInfo device)
    {
        Log.WriteLine($"{device.CurrentIndex}, {device.Battery}, {(int)device.status}, {device.IsHost}");
        InstantiateTracker(device.CurrentIndex, device.IsHost, device.status);
    }

    void InstantiateTracker(int trackerIndx, bool isHost, TrackData.Status status)
    {
        if (!trackers.ContainsKey(trackerIndx))
        {
            var parent = new GameObject($"Tracker ({trackerIndx}) parent");
            parent.transform.position = Vector3.zero;
            var go = Instantiate(trackerPref, parent.transform);
            go.name = $"{trackerIndx}";
            trackers.Add(trackerIndx, go.transform);
        }
        else if(status != TrackData.Status.None)
            trackers[trackerIndx].name = $"{trackerIndx} ({(isHost ? "HOST" : "CLIENT")}) {status}";
    }
}
