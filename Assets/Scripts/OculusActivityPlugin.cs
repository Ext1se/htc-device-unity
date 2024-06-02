using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using VIVE_Trackers;

public class OculusActivityPlugin : MonoBehaviour
{
    [SerializeField] Button startScanBtn;
    [SerializeField] Button stopScanBtn;
    [SerializeField] Transform trackerPref;
    [SerializeField] RectTransform trackersParentUI;

    Dictionary<int, Transform> trackers = new Dictionary<int, Transform>();
    IAckable dongleAPI;
    TrackerDeviceInfoView[] deviceViews;

    void Start()
    {
        dongleAPI = new AndroidDongleHID();
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
    }

    private void Update()
    {
        if (dongleAPI.IsInit)
            dongleAPI.DoLoop();
    }

    private void OnApplicationQuit()
    {
        if (trackers != null)
        {
            dongleAPI.CloseApplication();
            dongleAPI = null;
        }
    }

    private void OnDestroy()
    {
        if (trackers != null)
        {
            dongleAPI.CloseApplication();
            dongleAPI = null;
        }
    }

    private void Trackers_OnButtonClicked(int trackerIndx)
    {

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
            var go = Instantiate(trackerPref);
            go.name = $"{trackerIndx}";
            trackers.Add(trackerIndx, go.transform);
        }
        else if(status != TrackData.Status.None)
            trackers[trackerIndx].name = $"{trackerIndx} ({(isHost ? "HOST" : "CLIENT")}) {status}";
    }
}
