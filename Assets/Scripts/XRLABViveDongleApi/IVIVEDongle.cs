using System;

namespace VIVE_Trackers
{
    public delegate void TrackCallback(int trackerIndx, TrackData trackData, long time_delta);
    public delegate void TrackerStatusCallback(TrackerDeviceInfo device);
    public delegate void DeviceCallback(int trackerIndx);
    public delegate void DongleCallback(PairState[] states);

    public interface IVIVEDongle : IDisposable
    {
        event DongleCallback OnDongleStatus;
        event TrackerStatusCallback OnTrackerStatus;
        event TrackCallback OnTrack;
        event DeviceCallback OnConnected;
        event DeviceCallback OnDisconnected;
        event DeviceCallback OnButtonClicked;
        event DeviceCallback OnButtonDown;

        bool IsInit { get; }

        void Init();
        void OpenChannelForScan();
        void CloseChannelForScan();
        void CloseApplication();
        void UnpairAll();
        void Unpair(int indx);
        void PowerOffAll();
        void PowerOffAllAndClearPairingList();
        void StandByAll();
        void DoLoop();
        //void Reset(ResetMode mode);
        void Info();
        void TrackerInfo(int indx);

        void EndScanMap(byte currentDeviceIndex);
    }
}
