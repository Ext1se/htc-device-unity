using Meta.WitAi.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VIVE_Trackers.Constants;
using static VIVE_Trackers.DongleHID;
using static VIVE_Trackers.TrackData;

namespace VIVE_Trackers
{
    public class TrackerDeviceInfo
    {
        static readonly TrackerDeviceInfo[] Devices = new TrackerDeviceInfo[MAX_TRACKER_COUNT];

        private long lastTimeUpdate;
        private int last_pose_btns;
        private int btns;

        private int stuck_on_static;
        private int stuck_on_exists;
        private int stuck_on_not_checked;
        private bool bump_map_once;

        private IVIVEDongle hid;
        private byte[] currentAddress = null;
        private string currendAddressStr = null;
        private int currentIndex = -1;
        public string calib_1 = "";
        public string calib_2 = "";

        public string SerialNumber { get; private set; }
        public string ShipSerialNumber { get; private set; }
        public string SKU_ID { get; private set; }
        public string PCB_ID { get; private set; }
        public string FirmwareVersion { get; private set; }
        public int CurrentIndex => currentIndex;
        public byte[] CurrentAddress
        {
            get => currentAddress;
            set
            {
                currentAddress = value;
                if (currentAddress == null)
                {
                    currendAddressStr = null;
                    currentIndex = -1;
                }
                else currentIndex = MacToIdx(currentAddress);
            }
        }
        public string CurrendAddressStr
        {
            get
            {
                if (currendAddressStr == null && CurrentAddress != null)
                    currendAddressStr = MacToStr(CurrentAddress);
                return currendAddressStr;
            }
        }

        public bool BumpMapOnce { get => bump_map_once; set => bump_map_once = value; }
        public long DeltaTime => TotalMilis - lastTimeUpdate;
        public bool IsBtnClicked => (btns & 0x100) > 0 && (last_pose_btns & 0x100) == 0x0;
        public bool IsBtnDown => (btns & 0x100) > 0;
        public bool IsClient => IsInit && !IsHost;
        public bool IsHost { get; set; }
        public bool IsConnectedToHost { get; set; }
        public bool HasHostMap { get; set; }
        public bool IsConnected => IsHost || IsConnectedToHost;

        public bool IsInit =>
            !string.IsNullOrEmpty(SerialNumber) && !string.IsNullOrEmpty(ShipSerialNumber) &&
            !string.IsNullOrEmpty(SKU_ID) && !string.IsNullOrEmpty(PCB_ID) &&
            !string.IsNullOrEmpty(FirmwareVersion);

        public int FrameIndex { get; internal set; }
        public byte Battery { get; internal set; }
        public Status status { get; internal set; }


        public TrackerDeviceInfo(IVIVEDongle hid)
        {
            this.hid = hid;
            lastTimeUpdate = TotalMilis;
            //IsClient = false;
        }
        public TrackerDeviceInfo(IVIVEDongle hid, byte[] addr)
        {
            this.hid = hid;
            lastTimeUpdate = TotalMilis;
            CurrentAddress = addr;
        }

        public bool Fill(string data)
        {
            lastTimeUpdate = TotalMilis;
            var ack = data.Substring(0, 3);
            var value = data.Substring(3);
            switch (ack)
            {
                case ConstantsChorusdAck.ACK_DEVICE_SN:
                    SerialNumber = value;
                    return true;
                case ConstantsChorusdAck.ACK_SHIP_SN:
                    ShipSerialNumber = value;
                    return true;
                case ConstantsChorusdAck.ACK_SKU_ID:
                    SKU_ID = value;
                    return true;
                case ConstantsChorusdAck.ACK_PCB_ID:
                    PCB_ID = value;
                    return true;
                case ConstantsChorusdAck.ACK_VERSION:
                case ConstantsChorusdAck.ACK_VERSION_ALT:
                    FirmwareVersion = value;
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"DEVICE\n[\tSN:{SerialNumber}\n\tSHIP SN:{ShipSerialNumber}\n\tSKU:{SKU_ID}\n\tPCB:{PCB_ID}\n\tFirmware:{FirmwareVersion}\n]";
        }

        public void SaveToFile()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var filename = Path.Combine(UnityEngine.Application.persistentDataPath, "trackers.json");
#else
            var filename = "trackers.json";
#endif
            JArray array;
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                Log.WriteLine(json);
                array = JArray.Parse(json);

                var device = array.SelectToken($"[?(@SN == '{SerialNumber}')]");
                if (device != null)
                {
                    if (CurrentIndex != 255)
                        device["CurrentIndex"] = (JValue)JToken.FromObject(CurrentIndex);
                    else ((JObject)device).Remove("CurrentIndex");
                    device["IsHost"] = (JValue)JToken.FromObject(IsHost);
                    device["HasHostMap"] = (JValue)JToken.FromObject(HasHostMap);
                    device["IsConnectedToHost"] = (JValue)JToken.FromObject(IsConnectedToHost);
                    File.WriteAllText(filename, array.ToString());
                    return;
                }
            }
            else array = new JArray();
            if (!IsInit) return;
            JObject obj = new JObject()
            {
                { "SN", (JValue)JToken.FromObject(SerialNumber) },
                { "SHIPSN", (JValue)JToken.FromObject(ShipSerialNumber) },
                { "SKU", (JValue)JToken.FromObject(SKU_ID) },
                { "PCB", (JValue) JToken.FromObject(PCB_ID) },
                { "Firmware", (JValue) JToken.FromObject(FirmwareVersion) },
                { "IsHost", (JValue) JToken.FromObject(IsHost) },
                { "HasHostMap", (JValue) JToken.FromObject(HasHostMap) },
                { "IsConnectedToHost", (JValue) JToken.FromObject(IsConnectedToHost) },
                { "CurrentIndex", (JValue)JToken.FromObject(CurrentIndex) }
            };
            array.Add(obj);
            File.WriteAllText(filename, array.ToString());
        }

        public static void InitTrackers(IVIVEDongle hid)
        {
            var devices = Get(hid);
            for (int i = 0; i < MAX_TRACKER_COUNT && i < devices.Length; i++)
            {
                Devices[i] = devices[i];
            }
        }

        public static void SaveExistedTrackersInfo()
        {
            for (int i = 0; i < Devices.Length; i++)
            {
                if (Devices[i] != null)
                    Devices[i].SaveToFile();
            }
        }
        public static TrackerDeviceInfo Get(int indx)
        {
            return Array.Find(Devices, dev => dev != null && dev.CurrentIndex == indx);
        }
        public static TrackerDeviceInfo Get(byte[] addr)
        {
            var indx = MacToIdx(addr);
            return Get(indx);
        }
        public static TrackerDeviceInfo[] Get(IVIVEDongle hid)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var filename = Path.Combine(UnityEngine.Application.persistentDataPath, "trackers.json");
#else
            var filename = "trackers.json";
#endif
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                Log.WriteLine(json);
                var array = JArray.Parse(json);
                var devices = new List<TrackerDeviceInfo>();
                foreach (var dev in array)
                {
                    var tracker = new TrackerDeviceInfo(hid)
                    {
                        currentIndex = dev["CurrentIndex"].Value<int>(),
                        SerialNumber = dev["SN"].Value<string>(),
                        ShipSerialNumber = dev["SHIPSN"].Value<string>(),
                        SKU_ID = dev["SKU"].Value<string>(),
                        PCB_ID = dev["PCB"].Value<string>(),
                        FirmwareVersion = dev["Firmware"].Value<string>(),
                        IsHost = dev["IsHost"].Value<bool>(),
                        HasHostMap = dev["HasHostMap"].Value<bool>(),
                        IsConnectedToHost = dev["IsConnectedToHost"].Value<bool>(),
                    };
                    devices.Add(tracker);
                }
                return devices.ToArray();
            }
            return new TrackerDeviceInfo[0];
        }

        public static TrackerDeviceInfo GetHost()
        {
            return Array.Find(Devices, dev => dev != null && dev.CurrentIndex != -1 && dev.IsHost);
        }

        public static int[] GetActiveIndexes()
        {
            return Devices.Select(d => d != null ? d.CurrentIndex : -1).Where(i => i != -1).ToArray();
        }

        public static void UpdateStatus(PairState[] pair_state)
        {
            for (int i = 0; i < MAX_TRACKER_COUNT; i++)
            {
                var dev = Get(i);
                if (dev != null)
                {
                    if (pair_state[i] == PairState.UnpairedNoInfo || pair_state[i] == PairState.ReadyForScan)
                    {
                        dev.CurrentAddress = null;
                        Devices[i] = null;
                    }
                }
            }
        }

        public static TrackerDeviceInfo SetNew(IVIVEDongle hid, byte[] addr)
        {
            TrackerDeviceInfo dev = null;
            for (int i = 0; i < Devices.Length; i++)
            {
                if (Devices[i] == null || Devices[i].CurrentIndex == -1)
                {
                    dev = new TrackerDeviceInfo(hid, addr);
                    Devices[i] = dev;
                    break;
                }
            }
            return dev;
        }

        public static void DestroyDevice(int idx)
        {
            var arr_idx = Array.FindIndex(Devices, dev => dev != null && dev.CurrentIndex == idx);
            Devices[arr_idx] = null;
        }
        public static void DestroyDevices()
        {
            for (int i = 0; i < Devices.Length; i++)
                Devices[i] = null;
        }

        internal void Disconnect()
        {
            CurrentAddress = null;
            IsHost = false;
            IsConnectedToHost = false;
            HasHostMap = false;
        }

        internal void Update(byte btns, byte? status = null)
        {
            lastTimeUpdate = TotalMilis;
            if (status.HasValue)
                this.status = (Status)status.Value;

            if (btns >= 0x80)
            {
                if ((btns & 0x80) > 0)
                {
                    last_pose_btns = this.btns;
                    this.btns = ((btns & 0x7F) << 8) | this.btns & 0xFF;
                }
                else
                {
                    last_pose_btns = this.btns;
                    this.btns = btns | this.btns & 0xFF00;
                }
            }
            else
            {
                Battery = btns;
            }
        }

        internal void UpdateMapState(byte currentDeviceIndex, int state)
        {
            //tracker_map_state = state;
            if (stuck_on_static > 7)
            {
                Log.WarningLine(".......ok we're stuck, end the map again");
                bump_map_once = true;
                stuck_on_static = 0;
            }
            if (stuck_on_exists > 3)
                stuck_on_exists = 0;

            if (stuck_on_not_checked > 3)
                stuck_on_not_checked = 0;



            if (state == ConstantsChorusdStatus.MAP_REBUILD_WAIT_FOR_STATIC)
            {
                //comms.send_ack_to(Ackable.MacToIdx(device_addr), ACK_LAMBDA_COMMAND + f"{RESET_MAP}")
                stuck_on_static += 1;
                if (bump_map_once)
                {
                    if (IsClient)
                        Log.WriteLine("End the map?");
                    //comms.lambda_end_map(comms.current_host_id)
                    //comms.lambda_end_map(device_addr)
                    //comms.send_ack_to(comms.current_host_id, ACK_LAMBDA_COMMAND + f"{ASK_MAP}")
                    //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{RESET_MAP}")
                    bump_map_once = false;
                }
                //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_SET_STATUS + f"{KEY_MAP_STATE},{MAP_REBUILT}")
                //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_SET_STATUS + f"{KEY_CURRENT_TRACKING_STATE},{MAP_REBUILD_CREATE_MAP}")
            }
            else
            {
                bump_map_once = true;
                stuck_on_static = 0;
            }

            if (state == ConstantsChorusdStatus.MAP_EXIST)
            {
                if (stuck_on_exists == 0) // and comms.is_host(device_addr))
                {
                    Log.WarningLine("ok we're stuck on EXISTS, end the map again");
                }
                stuck_on_exists += 1;
            }
            else
                stuck_on_exists = 0;

            if (state == ConstantsChorusdStatus.MAP_NOT_CHECKED)
            {
                if (stuck_on_not_checked == 0 && IsClient && HasHostMap)
                {
                    Log.WarningLine("ok we're stuck on NOT CHECKED, end the map again");
                    hid.EndScanMap(currentDeviceIndex);
                }
                stuck_on_not_checked += 1;
            }
            else
                stuck_on_not_checked = 0;
        }
    }
}
