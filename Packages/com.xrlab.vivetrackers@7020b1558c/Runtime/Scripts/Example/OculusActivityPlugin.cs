using System.Collections.Generic;
using System.Collections;
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
public enum BodyRole
{
    Invalid,
    Head,
    RightHand,
    LeftHand,
    RightFoot,
    LeftFoot,
    RightKnee,
    LeftKnee,
    Hip,
    Chest,
}

public class OculusActivityPlugin : MonoBehaviour
{
    [SerializeField] Button startScanBtn;
    [SerializeField] Button stopScanBtn;
    [SerializeField] Button restartBtn;
    [SerializeField] Transform trackerPref;
    [SerializeField] RectTransform trackersParentUI;
    [SerializeField] TextMeshProUGUI statusText;

    Dictionary<int, Transform> trackers = new Dictionary<int, Transform>();
    IVIVEDongle dongleAPI;
    TrackerDeviceInfoView[] deviceViews;
    Transform currentController;

#if UNITY_EDITOR || UNITY_STANDALONE
    Thread dongleLoopThread;
#endif

    private IEnumerator Start()
    {
        UnityDispatcher.Create();
        
        Log.dongleAPILogger = new XRLabTrackersLogger();
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
        dongleAPI.OnDongleInfo += DongleAPI_OnDongleInfo;
        dongleAPI.Init();

        while(!dongleAPI.IsInit)
        {
            yield return new WaitForFixedUpdate();
            dongleAPI.Init();
        }
        Log.dongleAPILogger?.WriteLine("Dongle is INITIATED OK!!!"); ;

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
        restartBtn.onClick.AddListener(() => dongleAPI.Restart());

#if UNITY_EDITOR || UNITY_STANDALONE
        dongleLoopThread = new Thread(ThreadDongleLoop);
        dongleLoopThread.Start();
#endif
    }

    public Transform GetTracker(int index)
    {
        if (trackers.ContainsKey(index))
            return trackers[index];
        return null;
    }

    private void DongleAPI_OnDongleInfo(KeyValuePair<string, string>[] info)
    {
        Log.dongleAPILogger?.Write("Dongle info:\n");
        foreach (var item in info)
        {
            Log.dongleAPILogger?.Write($"{item.Key}:{item.Value}\n");
        }
        Log.dongleAPILogger?.WriteLine("");
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    void ThreadDongleLoop()
    {
        while (dongleAPI != null && dongleAPI.IsInit)
        {
            dongleAPI.DoLoop();
        }
        Log.dongleAPILogger?.WriteLine("Thread exited");
    } 
#endif

    private void Update()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        if (dongleAPI != null && dongleAPI.IsInit)
        {
            dongleAPI.DoLoop();
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
#if UNITY_EDITOR || UNITY_STANDALONE
            dongleLoopThread.Abort();
            dongleLoopThread = null; 
#endif
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
#if UNITY_EDITOR || UNITY_STANDALONE
            dongleLoopThread.Abort();
            dongleLoopThread = null; 
#endif
        }
    }

    private void Trackers_OnButtonClicked(int trackerIndx)
    {
        dongleAPI.ScanMap(trackerIndx);
    }

    private void Trackers_OnTrack(int trackerIndx, TrackData trackData, long time_delta)
    {
        //Log.WriteLine($"#{trackerIndx}, {trackData.pos_x}, {trackData.pos_y}, {trackData.pos_z}, {trackData.rot_x}, {trackData.rot_y}, {trackData.rot_z}, {trackData.rot_w}, {(int)trackData.status}");
#if UNITY_EDITOR || UNITY_STANDALONE
        UnityDispatcher.Invoke(() =>
        {
            InstantiateTracker(trackerIndx, false, TrackData.Status.None);
            trackers[trackerIndx].localPosition = new Vector3(trackData.pos_x, trackData.pos_y, -trackData.pos_z);
            trackers[trackerIndx].localRotation = new Quaternion(-trackData.rot_x, -trackData.rot_y, trackData.rot_z, trackData.rot_w);
        });
#else
        InstantiateTracker(trackerIndx, false, TrackData.Status.None);
        trackers[trackerIndx].localPosition = new Vector3(trackData.pos_x, trackData.pos_y, -trackData.pos_z);
        trackers[trackerIndx].localRotation = new Quaternion(-trackData.rot_x, -trackData.rot_y, trackData.rot_z, trackData.rot_w);
#endif
    }

    private void Trackers_OnDisconnected(int trackerIndx)
    {
        Log.dongleAPILogger?.WriteLine($"Device indx:{trackerIndx} disconnected");
#if UNITY_EDITOR || UNITY_STANDALONE
        UnityDispatcher.Invoke(() =>
        {
            if (trackers.ContainsKey(trackerIndx))
            {
                Destroy(trackers[trackerIndx].parent.gameObject);
                trackers.Remove(trackerIndx);
                dongleAPI.CloseChannelForScan();
            }
        });
#else
        if (trackers.ContainsKey(trackerIndx))
        {
            Destroy(trackers[trackerIndx].parent.gameObject);
            trackers.Remove(trackerIndx);
            dongleAPI.CloseChannelForScan();
        }
#endif
        //dongleAPI.CloseChannelForScan();

    }

    private void Trackers_OnConnected(int trackerIndx)
    {
        Log.dongleAPILogger?.WriteLine($"Device indx:{trackerIndx} connected");
#if UNITY_EDITOR || UNITY_STANDALONE
        UnityDispatcher.Invoke(() =>
        {
            InstantiateTracker(trackerIndx, false, 0);
        });
#else
        InstantiateTracker(trackerIndx, false, 0);
#endif
    }

    private void Trackers_OnTrackerStatus(TrackerDeviceInfo device)
    {
        //Log.WriteLine($"STATUS #{device.CurrentIndex}, Battery:{device.Battery}, Status:{device.status}, IsHost:{device.IsHost}");
#if UNITY_EDITOR || UNITY_STANDALONE
        UnityDispatcher.Invoke(() =>
        {
            InstantiateTracker(device.CurrentIndex, device.IsHost, device.status);
        });
#else
        InstantiateTracker(device.CurrentIndex, device.IsHost, device.status);
#endif
    }

    void InstantiateTracker(int trackerIndx, bool isHost, TrackData.Status status)
    {
        if (trackerIndx < 0) return;
#if UNITY_EDITOR || UNITY_STANDALONE
        UnityDispatcher.Invoke(() =>
        {
            if (!trackers.ContainsKey(trackerIndx))
            {
                var parent = new GameObject($"Tracker ({trackerIndx}) parent");
                parent.transform.position = Vector3.zero;
                var go = Instantiate(trackerPref, parent.transform);
                go.name = $"{trackerIndx}";
                trackers.Add(trackerIndx, go.transform);
            }
            else if (status != TrackData.Status.None)
                trackers[trackerIndx].parent.name = $"{trackerIndx} ({(isHost ? "HOST" : "CLIENT")}) {status}";
        });
#else
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
#endif
    }
}
