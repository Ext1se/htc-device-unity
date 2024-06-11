using System;
using System.Collections.Generic;

namespace VIVE_Trackers
{
    public delegate void TrackCallback(int trackerIndx, TrackData trackData, long time_delta);
    public delegate void TrackerStatusCallback(TrackerDeviceInfo device);
    public delegate void DeviceCallback(int trackerIndx);
    public delegate void DongleCallback(PairState[] states);
    public delegate void DongleInfoCallback(KeyValuePair<string, string>[] info);

    public interface IVIVEDongle : IDisposable
    {
        event DongleCallback OnDongleStatus;
        event TrackerStatusCallback OnTrackerStatus;
        event TrackCallback OnTrack;
        event DeviceCallback OnConnected;
        event DeviceCallback OnDisconnected;
        event DeviceCallback OnButtonClicked;
        event DeviceCallback OnButtonDown;
        event DongleInfoCallback OnDongleInfo;

        bool IsInit { get; }

        void Init();
        void OpenChannelForScan();
        void CloseChannelForScan();
        void Restart();
        void CloseApplication();
        void UnpairAll();
        void Unpair(int indx);
        void PowerOffAll();
        void PowerOffAllAndClearPairingList();
        void StandByAll();
        void DoLoop();
        //void Reset(ResetMode mode);
        void GetDongleInfo();
        /// <summary>
        /// Request tracker info
        /// </summary>
        /// <param name="indx"></param>
        void GetTrackerStatus(int indx);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="value"></param>
        /// <returns>Return true if device is founded</returns>
        bool SetRoleID(string serialNumber, int value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns>Return roleid of device if founded, otherwise -1</returns>
        int GetRoleID(string serialNumber);
        /// <summary>
        /// Restart ascan map of tracker
        /// </summary>
        /// <param name="currentDeviceIndex"></param>
        void ScanMap(int currentDeviceIndex);
        void EndScanMap(int currentDeviceIndex);

        void ExperimentalFileDownload(int indx);
    }
}
