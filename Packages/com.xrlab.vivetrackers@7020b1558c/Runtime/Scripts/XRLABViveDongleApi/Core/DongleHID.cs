//#define SHOW_DUMP

using HID_ViveTest.PythonLike;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SystemHalf;
using VIVE_Trackers.Constants;
using static VIVE_Trackers.Constants.ConstantsChorusdDongle;
using static VIVE_Trackers.TrackerDeviceInfo;

namespace VIVE_Trackers
{
    public abstract class DongleHID : IVIVEDongle
    {
        public const byte MAX_TRACKER_COUNT = 5;

       

        //public readonly TrackerDeviceInfo[] Devices = new TrackerDeviceInfo[MAX_TRACKER_COUNT];

        protected long last_host_map_ask_ms;
        int _current_host_indx = -1;
        protected int current_host_indx 
        { 
            get => _current_host_indx;
            set
            {
                if(_current_host_indx != value)
                {
                    if(_current_host_indx != -1 && value == -1)
                        Log.dongleAPILogger?.WarningLine("DAMMM!!! And the host never showed up, then we recreate host as new tracker later");
                    else if (_current_host_indx != -1 && value != -1)
                        Log.dongleAPILogger?.WarningLine("DAMMM OK!!! And the host never showed up, then we recreate host as new tracker as " + value);
                    _current_host_indx = value;
                }
            }
        }
        protected int tick_periodic = 0;
        protected PairState[] pair_state = new PairState[MAX_TRACKER_COUNT];
        protected WIFI_Info wifi_info;

        protected static Stopwatch sw;
        public static long TotalMilis => sw.ElapsedMilliseconds;
        public static bool OnlyListenerMode = false;

        public event TrackerStatusCallback OnTrackerStatus;
        public event TrackCallback OnTrack;
        public event DeviceCallback OnConnected;
        public event DeviceCallback OnDisconnected;
        public event DeviceCallback OnButtonClicked;
        public event DeviceCallback OnButtonDown;
        public event DongleCallback OnDongleStatus;
        public event DongleInfoCallback OnDongleInfo;
        public abstract bool IsInit { get; }
        protected Dictionary<string, System.Action<string>> registeredActions = new Dictionary<string, System.Action<string>>();
        protected Dictionary<string, System.Action<string>> delayedTickActions = new Dictionary<string, System.Action<string>>();
        public abstract void Init();

        void IVIVEDongle.DoLoop()
        {
            DoLoop();
        }
        protected abstract void DoLoop();

        protected virtual void ParseUSBData(byte[] resp)
        {
            if (resp[0] == 0)
                resp = resp.Skip(1).ToArray();

            var cmd = (DongleResponceCmd)resp[0];
            switch (cmd)
            {
                case DongleResponceCmd.DRESP_PAIR_EVENT:
                    {
                        var respData = ResponseStruct.Parse(resp, true);
                        var unk = respData.ret[0];
                        var is_unpair = respData.ret[1] == 1; // 0x1, id?
                        var paired_mac = respData.ret.Skip(3).ToArray();
                        var device_idx = MacToIdx(paired_mac);

                        if (Log.dongleAPILogger != null)
                        {
                            var paired_mac_str = MacToStr(paired_mac);
                            Log.dongleAPILogger.WriteLine((is_unpair ? "Unpaired " : "Paired ") + $"{paired_mac_str}, status:{unk:X}"); 
                        }

                        if (is_unpair)
                        {
                            handle_disconnected(device_idx);
                            return;
                        }

                        InitNewTracker(device_idx, paired_mac);
                    }
                    break;
                case DongleResponceCmd.DRESP_TRACKER_RF_STATUS:
                    {
                        var respData = ResponseStruct.Parse(resp, true);
                        ParseDongleStatus(respData.ret);
                    }
                    break;
                case DongleResponceCmd.DRESP_TRACKER_INCOMING:
                    {
                        ParseTrackerIncoming(resp);
                        break;
                    }
                default:
                    {
#if SHOW_DUMP
                        Log.WarningLine($"dump for unused command {cmd:X} (UNK!!!):");
                        HEXDump(resp);
#endif
                    }
                    break;
            }
        }

        private void InitNewTracker(byte device_idx, byte[] addr)
        {
            OnConnected?.Invoke(device_idx);
            var dev = Get(device_idx);
            if (dev == null)
                dev = SetNew(this, addr);
            else if (dev.IsInit)
            {
                current_host_indx = dev.IsHost ? dev.CurrentIndex : current_host_indx;
                if (dev.CurrentAddress == null)
                    dev.CurrentAddress = addr;
                return;
            }
            ACKInitTracker(device_idx, addr);

            //ACK_SetNewID(addr, device_idx);
            //WIFI_SetCountry(addr, wifi_info.country);
            // ждем команду AZZ в пакете DRESP_TRACKER_INCOMING (ParseTrackerIncoming)

            //// TODO: detect re-pairs and force re-init
            //if (current_host_indx == -1 /*|| dev.IsHost*/)
            //{
            //    current_host_indx = device_idx;
            //    dev.IsHost = true;
            //    Log.WriteLine($"Making {paired_mac_str} the SLAM HOST", Log.LogType.Green);
            //    ACK_SetTrackingMode(addr, ConstantsChorusdStatus.TRACKING_MODE_HOST_ON);
            //}
            //else 
            //{
            //    if (!dev.IsInit)
            //    {
            //        dev.IsHost = false;
            //        Log.WriteLine($"Making {paired_mac_str} the SLAM CLIENT", Log.LogType.Green);
            //        ACK_SetTrackingMode(addr, ConstantsChorusdStatus.TRACKING_MODE_CLIENT_ON);
            //    }
            //}
            //ACK_SetTrackingHost(addr, current_host_indx);
            //ACK_SetWIFIHost(addr, current_host_indx);

            //ACK_SetRoleID(addr, 1);
        }

        protected virtual void ACKInitTracker(byte device_idx, byte[] addr)
        {
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_NEW_ID}{device_idx}");
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_ATW}");
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_CAMERA_FPS}60");
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_TIME_SET}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_WIFI_COUNTRY}{wifi_info.country}");
        }

        protected virtual void ParseDongleStatus(byte[] data)
        {
            //Log.dongleAPILogger?.WriteLine("PARSE_TRACKER_STATUS");
            var res = StructConverter.Unpack("<6BLLLLL", data);
            var status = (byte[])res[0];
            pair_state = new PairState[MAX_TRACKER_COUNT] { (PairState)(uint)res[1], (PairState)(uint)res[2], (PairState)(uint)res[3], (PairState)(uint)res[4], (PairState)(uint)res[5] };
            UpdateStatus(pair_state);

            if (Log.dongleAPILogger != null)
            {
                var pairStr = Array.ConvertAll(pair_state, ps => Enum.IsDefined(typeof(PairState), ps) ? ps.ToString() : $"{(uint)ps:X}");
                Log.dongleAPILogger?.WriteLine($"cmd:{status.ArrayToString(true)}, {pairStr[0]}, {pairStr[1]}, {pairStr[2]}, {pairStr[3]}, {pairStr[4]}"); 
            }

            // Fallback for disconnects if tracker isUnpaired full
            if (current_host_indx >= 0)
            {
                //if (((ushort)pair_state[current_host_indx] & ConstantsChorusdStatus.PAIR_STATE_PAIRED) == 0)
                if (((ushort)pair_state[current_host_indx] & ConstantsChorusdStatus.PAIR_STATE_UNPAIRED) == ConstantsChorusdStatus.PAIR_STATE_UNPAIRED)
                {
                    var dev = Get(current_host_indx);
                    if (dev != null && dev.IsInit)
                        handle_disconnected(current_host_indx);
                }
            }


            OnDongleStatus?.Invoke(pair_state);
        }

        protected virtual void ParseTrackerIncoming(byte[] data)
        {
            //Log.WriteLine(BitConverter.ToString(data), Log.LogType.Magenta);
            var incoming = TrackerIncomingData.Parse(data);
            if (incoming.type == TrackerIncomingData.CommandType.ACK)
            {
                ParseIncomingACK(incoming);
                Tick();
                return;
            }
            else if (incoming.type == TrackerIncomingData.CommandType.POSE)
            {
                parse_pose_data(incoming);
                Tick();
                return;
            }
            try
            {
                var data_id = incoming.data_raw[0];
                var data_real = incoming.data_raw.Skip(1).ToArray();
                Log.dongleAPILogger?.WriteLine($"   [PARSE_TRACKER_INCOMING (NOT IMPLEMENTED!!!!)] data_id:{data_id:X}");
            }
            catch (Exception ex)
            {
                Log.dongleAPILogger?.ErrorLine(ex);
            }
            Tick();
        }

        protected virtual void Tick()
        {
            tick_periodic += 1;
            if (tick_periodic > 1000)
            {
                //Log.dongleAPILogger?.WriteLine("Tick!");
                for (byte i = 0; i < MAX_TRACKER_COUNT; i++)
                {
                    var _dev = Get(i);
                    if (_dev == null || _dev.CurrentIndex == -1) continue;
                    OnTrackerStatus?.Invoke(_dev);
                    if (_dev.CurrentAddress == null) continue;

                    //ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_TRANSMISSION_READY);
                    //ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_CURRENT_MAP_ID);
                    //ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_MAP_STATE);
                    //ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_CURRENT_TRACKING_STATE);
                    //if (_dev.IsHost)
                    //{
                    //    ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_RECEIVED_HOST_ED);
                    //    ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_RECEIVED_HOST_MAP);
                    //}
                }

                foreach (var item in delayedTickActions)
                {
                    item.Value?.Invoke(item.Key);
                }
                delayedTickActions.Clear();
                tick_periodic = 0;
            }
        }

        protected void parse_pose_data(TrackerIncomingData incoming)
        {
            byte device_idx = MacToIdx(incoming.mac);

            var dev = Get(device_idx);
            if (dev == null)
            {
                InitNewTracker(device_idx, incoming.mac);
                dev = Get(device_idx);
            }
            if (dev.CurrentAddress == null)
                dev.CurrentAddress = incoming.mac;
            dev.FrameIndex += 1;

            if (incoming.rawDataLength == 2)
            {
                dev.Update(incoming.data_raw[1]);
                if (dev.IsBtnClicked)
                    OnButtonClicked?.Invoke(device_idx);
                if (dev.IsBtnDown)
                    OnButtonDown?.Invoke(device_idx);
                OnTrackerStatus?.Invoke(dev);
                return;
            }
            if (incoming.rawDataLength == 4)
            {
                dev.Update(incoming.data_raw[1]);
                if (dev.IsBtnClicked)
                    OnButtonClicked?.Invoke(device_idx);
                if (dev.IsBtnDown)
                    OnButtonDown?.Invoke(device_idx);
                OnTrackerStatus?.Invoke(dev);
                return;
            }
            if (incoming.rawDataLength != 0x25 && incoming.rawDataLength != 0x27)
            {
                dev.Update(incoming.data_raw[1]);
                if (dev.IsBtnClicked)
                    OnButtonClicked?.Invoke(device_idx);
                if (dev.IsBtnDown)
                    OnButtonDown?.Invoke(device_idx);
                OnTrackerStatus?.Invoke(dev);
                return;
            }

            var res = StructConverter.Unpack("<BB3f4h3h3hHBH", incoming.data_raw);

            float[] posArr = (float[])res[2];
            ushort[] rot_arr = (ushort[])res[3];
            ushort[] acc_arr = (ushort[])res[4];
            ushort[] vel_arr = (ushort[])res[5];
            TrackData trackData = new TrackData
            {
                frame_idx = (byte)res[0],
                btns = (byte)res[1],

                pos_x = posArr[0],
                pos_y = posArr[1],
                pos_z = posArr[2],

                rot_x = (float)Half.ToHalf(rot_arr[0]),
                rot_y = (float)Half.ToHalf(rot_arr[1]),
                rot_z = (float)Half.ToHalf(rot_arr[2]),
                rot_w = (float)Half.ToHalf(rot_arr[3]),

                acc_x = (float)Half.ToHalf(acc_arr[0]),
                acc_y = (float)Half.ToHalf(acc_arr[1]),
                acc_z = (float)Half.ToHalf(acc_arr[2]),

                rot_vel_x = (float)Half.ToHalf(vel_arr[0]),
                rot_vel_y = (float)Half.ToHalf(vel_arr[1]),
                rot_vel_z = (float)Half.ToHalf(vel_arr[2]),

                timeMS = (ushort)res[6],
                deltaTimeMS = ((ushort)res[8]) / 1000000f,
                mapPointQuality = incoming.data_raw[^2],
                mapPointQualityMode = incoming.data_raw[^1],
                status = (TrackData.Status)(byte)res[7]
            };

            long delta_ms = dev.DeltaTime;
            dev.Update(trackData.btns, trackData.status);

            OnTrack?.Invoke(device_idx, trackData, delta_ms);

            if (dev.IsBtnClicked)
                OnButtonClicked?.Invoke(device_idx);
            if (dev.IsBtnDown)
                OnButtonDown?.Invoke(device_idx);
        }

        protected void ParseIncomingACK(TrackerIncomingData incoming)
        {
            if (incoming.rawDataLength == 0)
            {
                Log.dongleAPILogger?.Warning(".");
                return;
            }
            byte[] device_addr = incoming.mac;
            byte[] raw_data = incoming.data_raw.Take(incoming.rawDataLength).ToArray();
            var data = raw_data.DecodeToUTF8();


            var first = data[0];
            var data_real = data.Substring(1);
            byte deviceIndx = MacToIdx(device_addr);
            string macAddress = MacToStr(device_addr);
            if (current_host_indx == -1)
                current_host_indx = deviceIndx;
            Log.dongleAPILogger?.Write($"[PARSE ACK] #{deviceIndx}({macAddress}) len:{data.Length} datastring:");
            Log.dongleAPILogger?.Warning($"{data}");
            Log.dongleAPILogger?.WriteLine("");
            TrackerDeviceInfo dev = Get(deviceIndx); 
            if (dev == null)
                dev = SetNew(this, device_addr);
            dev.IsHost = current_host_indx == deviceIndx;
            if (first == ConstantsChorusdAck.ACK_CATEGORY_CALIB_1)
            {
                dev.calib_1 += data_real;
                if (raw_data[raw_data.Length - 2] == 0xEF && raw_data[raw_data.Length - 1] == 0x01)
                    Log.dongleAPILogger?.WriteLine($"   Got CALIB_1 ({macAddress}): {dev.calib_1}");
            }
            else if (first == ConstantsChorusdAck.ACK_CATEGORY_CALIB_2)
            {
                dev.calib_2 += data_real; 
                Log.dongleAPILogger?.WriteLine($"   Got CALIB_1 ({macAddress}): {dev.calib_1}");
                Log.dongleAPILogger?.WriteLine($"   Got CALIB_2 ({macAddress}): {dev.calib_2}");
            }
            else if (first == ConstantsChorusdAck.ACK_CATEGORY_DEVICE_INFO)
            {
                var data_real_ss = data_real.Substring(0, 3);
                Log.dongleAPILogger?.WriteLine($"   Got device info ACK ({macAddress}): {data_real_ss}");

                switch (data_real_ss)
                {
                    case ConstantsChorusdAck.ACK_AGN:

                        break;
                    case ConstantsChorusdAck.ACK_ARI:
                        Log.dongleAPILogger?.WarningLine("GET ROLE ID: " + data_real);
                        if (dev != null)
                            dev.RoleID = data_real.After(ConstantsChorusdAck.ACK_ARI).ToInt();
                        break;
                    case ConstantsChorusdAck.ACK_AZZ:
                        {
                            AnswerTo_ACK_AZZ(dev);
                        }
                        break;
                    default:
                        if (dev.Fill(data_real))
                        {
                            if (dev.IsInit)
                            {
                                Log.dongleAPILogger?.WarningLine(dev.ToString());
                                SaveTrackerInfo(dev);
                            }
                        }
                        break;
                }
            }
            else if (first == ConstantsChorusdAck.ACK_CATEGORY_PLAYER || first == 'p')
            {
                var parts = data_real.Split(":");
                var idx = parts[0].ToInt();
                var args = parts[1];

                if (idx == ConstantsChorusdAck.LAMBDA_PROP_GET_STATUS)
                {
                    var int_args = Array.ConvertAll(args.Split(","), a => a.ToInt());
                    var key_id = int_args[0];
                    var state = int_args[1];
                    var addendum = "";

                    if (key_id == ConstantsChorusdStatus.KEY_RECEIVED_HOST_ED)
                    {
                        //if state == 0:
                        //    comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{ASK_ED}")
                        //pass
                    }
                    else if (key_id == ConstantsChorusdStatus.KEY_RECEIVED_HOST_MAP)
                    {
                        if (state == 0 && (TotalMilis - last_host_map_ask_ms) > 10000 && dev.IsConnected)
                        {
                            Log.dongleAPILogger?.WriteLine("Ask for map again");
                            last_host_map_ask_ms = TotalMilis;
                            //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{RESET_MAP}")
                            //comms.send_ack_to(comms.current_host_id, ACK_LAMBDA_COMMAND + f"{ASK_MAP}") # doesn't work?
                            //SendAckTo(deviceIndx, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdStatus.ASK_MAP}"); // doesn't do anything?
                            //SendAckTo(device_addr, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdStatus.ASK_MAP}");
                            var hostDev = GetHost();
                            SendAckTo(hostDev.CurrentAddress, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdStatus.ASK_MAP}");
                            // TODO: when do we ask for maps?
                            dev.BumpMapOnce = true;
                            dev.HasHostMap = false;
                        }
                        else
                        {
                            if (state > 0)
                                dev.HasHostMap = true;
                            if (dev.BumpMapOnce)
                            {
                                //comms.send_ack_to(mac_to_idx(device_addr), ACK_END_MAP)
                                //comms.send_ack_to(mac_to_idx(device_addr), ACK_TRACKING_MODE + "-1")
                                //comms.send_ack_to(mac_to_idx(device_addr), ACK_TRACKING_MODE + "1")
                                dev.BumpMapOnce = false;
                            }
                            //comms.send_ack_to(mac_to_idx(paired_mac), ACK_END_MAP)
                            //comms.send_ack_to_all(ACK_FW + "1")
                        }
                    }
                    else if (key_id == ConstantsChorusdStatus.KEY_TRANSMISSION_READY)
                    {
                        // comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{RESET_MAP}")
                        // comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{ASK_ED}")
                        //  a = 'a';
                        //SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_LAMBDA_SET_STATUS}{key_id}");
                        ACK_LambdaAskStatus(device_addr, ConstantsChorusdStatus.KEY_CURRENT_MAP_ID);
                    }
                    else if (key_id == ConstantsChorusdStatus.KEY_MAP_STATE)
                    {
                        addendum = $"({ConstantsChorusdStatus.MapStatusToStr(state)})";

                        dev.UpdateMapState(state);
                    }
                    else if (key_id == ConstantsChorusdStatus.KEY_CURRENT_TRACKING_STATE)
                        addendum = $"({ConstantsChorusdStatus.PoseStatusToStr(state)})";
                    else if (key_id == ConstantsChorusdStatus.KEY_CURRENT_MAP_ID)
                    {
                        if (dev.IsHost)
                        {
                            Log.dongleAPILogger?.WarningLine("KEY_CURRENT_MAP_ID is " + state + ". COOL!!!!");
                            ACK_LambdaAskStatus(device_addr, ConstantsChorusdStatus.KEY_MAP_STATE);
                        }
                        else
                            Log.dongleAPILogger?.ErrorLine("KEY_CURRENT_MAP_ID is " + state + ". But is it client info????");
                    }

                    Log.dongleAPILogger?.WriteLine($"   Status returned for SLAM key {ConstantsChorusdStatus.SlamKeyToStr(key_id)} ({macAddress}): {state} {addendum}");
                }
                else
                {
                    Log.dongleAPILogger?.WarningLine($"   Got PLAYER UNDEFIINED ACK ({macAddress}): {data_real}");
                }
            }
            else
            {
                if (data.Length < 2)
                {
                    Log.dongleAPILogger?.WriteLine("SMALL DATA LENGTH: " + data.Length);
                    return;
                }
                var second = data.Substring(0, 2);
                data_real = data.Substring(2);
                if (second == ConstantsChorusdAck.ACK_LAMBDA_PROPERTY)
                {
                    Log.dongleAPILogger?.WriteLine($"   Got LP ACK ({macAddress}): {data.Substring(2)}");
                }
                else if (second == ConstantsChorusdAck.ACK_LAMBDA_STATUS)
                {
                    var int_args = Array.ConvertAll(data_real.Split(","), a => a.ToInt()); // should be 3
                    Log.dongleAPILogger?.ErrorLine($"   Got LAMBDA_STATUS ACK ({macAddress}): {int_args[0]},{int_args[1]},{int_args[2]}");
                    var mode = (current_host_indx == deviceIndx ? 2 : 1);
                    SendAckTo(device_addr, ConstantsChorusdAck.ACK_FILE_WRITE + mode);
                    if (int_args[1] != 2)
                    {
                        //print("ask for host map.")
                        //SendAckTo(device_addr, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdAck.LAMBDA_CMD_ASK_ED}");
                        //SendAckTo(device_addr, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdAck.LAMBDA_CMD_ASK_MAP}");
                        //SendAckTo(device_addr, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdAck.LAMBDA_CMD_ASK_KEYFRAME_SYNC}");
                        //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{ASK_ED}")
                        //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{ASK_MAP}")
                        //comms.send_ack_to(mac_to_idx(device_addr), ACK_LAMBDA_COMMAND + f"{KF_SYNC}")
                        //pass
                    }
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_COUNTRY)
                {
                    Log.dongleAPILogger?.WriteLine($"   Got WIFI_HOST_SSID ACK ({data_real})");
                    if (dev.IsHost)
                    {
                        wifi_info.country = data_real;
                        wifi_info.Save();
                    }
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_HOST_SSID)
                {
                    var parts = data_real.Split(","); //ssid, passwd, freq
                    Log.dongleAPILogger?.WriteLine($"   Got WIFI_HOST_SSID ACK ({macAddress}): ssid={parts[0]}, pass={parts[1]}, freq={parts[2]}");
                    if (dev.IsHost)
                    {
                        wifi_info.host_ssid = parts[0];
                        wifi_info.host_passwd = parts[1];
                        wifi_info.host_freq = parts[2].ToInt();
                        wifi_info.Save();
                    }
                    else
                    {
                        Log.dongleAPILogger?.WarningLine($"Skip save WIFI data");
                        if(wifi_info.IsValid)
                        {
                            SendWIFIToTracker(device_addr);
                        }
                    }
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_SETUP)
                {
                    if (!dev.IsConnectedToHost && !dev.IsHost && wifi_info.IsValid)
                    {
                        Log.dongleAPILogger?.WriteLine($"   Got WIFI_SSID ACK ({macAddress})");
                        Log.dongleAPILogger?.WarningLine($"=====Sync wifi with other trackers");
                        SendWIFIConnection(dev); 
                    }
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_CONNECT)
                {
                    var ret = data_real.ToInt();
                    Log.dongleAPILogger?.Warning($"   Got WIFI_CONNECT ACK ({macAddress}): ");
                    if (ret == 0)
                        Log.dongleAPILogger?.BlueLine(ret);
                    else
                        Log.dongleAPILogger?.GreenLine(ret);
                    dev.IsConnectedToHost = (ret > 0);
                    if (!dev.IsConnectedToHost && !dev.IsHost && wifi_info.IsValid)
                    {
                        SendWIFIConnection(dev);
                    }
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_ERROR)
                {
                    //if(!wifi_info.IsValid)
                    //    wifi_info = WIFI_Info.Load();
                    if (!dev.IsHost && wifi_info.IsValid)
                    {
                        SendWIFIToTracker(device_addr);
                    }
                }
                else if (second == ConstantsChorusdAck.ACK_MAP_STATUS)
                {
                    var status = Array.ConvertAll(data_real.Split(","), a => a.ToInt());
                    dev.MapStatus = (MapStatus)status[1];
                    Log.dongleAPILogger?.WriteLine($"   Got MAP_STATUS ({macAddress}): {status.ArrayToString()} ({ConstantsChorusdStatus.MapStatusToStr(status[1])})");
                   
                    dev.UpdateMapState(status[1]);

                    //comms.send_ack_to_all(ACK_END_MAP)

                    // Initial map status:
                    // Got MAP_STATUS: -1,10
                    // Got MAP_STATUS: 0,10
                    // Got MAP_STATUS: 0,3

                    // Losing tracking?
                    // Got LP ACK: 1,0,1
                    // Got MAP_STATUS: -1,0
                    // Got MAP_STATUS: -1,1

                    // Got MAP_STATUS: 0,6 = mapped and tracking
                }
                else
                {
                    if (data.Length < 3)
                    {
                        Log.dongleAPILogger?.WarningLine("UNUSED ACK: " + data);
                        return;
                    }
                    var third = data.Substring(0, 3);
                    data_real = data.Substring(3);
                    if (third == ConstantsChorusdAck.ACK_POWER_OFF)
                    {
                        Log.dongleAPILogger?.WriteLine($".  Got POWER_OFF. ({macAddress})");
                        SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_STANDBY}");
                        dev.status = TrackData.Status.PowerOff;
                        OnTrackerStatus?.Invoke(dev);
                        handle_disconnected(deviceIndx);
                        CloseChannelForScan();
                    }
                    else if (third == ConstantsChorusdAck.ACK_RESET)
                    {
                        Log.dongleAPILogger?.WriteLine($".  Got RESET ({macAddress}).");
                        dev.status = TrackData.Status.Reset;
                        OnTrackerStatus?.Invoke(dev);
                        handle_disconnected(deviceIndx);
                        CloseChannelForScan();
                    }
                    else if (third == ConstantsChorusdAck.ACK_TRACKING_MODE)
                    {
                        if(registeredActions.ContainsKey(ConstantsChorusdAck.ACK_TRACKING_MODE))
                            registeredActions[ConstantsChorusdAck.ACK_TRACKING_MODE]?.Invoke(data);
                    }
                    else
                    {
                        if (data.Length < 4) return;
                        var fourth = data.Substring(0, 4);
                        data_real = data.Substring(4);
                        if (fourth == ConstantsChorusdAck.ACK_ERROR_CODE)
                        {
                            Log.dongleAPILogger?.ErrorLine($"   Got ERROR ({macAddress}): {data_real}");
                        }
                        else
                        {
                            Log.dongleAPILogger?.WriteLine($"   Got undefined ACK ({macAddress}): dataStr:({data}) raw:({BitConverter.ToString(raw_data)})");
                            if (data.Contains("p94:"))
                            {
                                //SendAckTo(device_addr, $"P94:0:2:{ConstantsChorusdStatus.RESET_MAP}"); 
                                var hostDev = GetHost();
                                SendAckTo(hostDev.CurrentAddress, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdStatus.ASK_MAP}");
                                //System.Threading.Thread.Sleep(5);
                                //SendAckTo(deviceIndx, $"P94:0:2:{ConstantsChorusdStatus.RESET_MAP}");
                                //SendAckTo(deviceIndx, ConstantsChorusdAck.ACK_LAMBDA_COMMAND + $"{ConstantsChorusdStatus.ASK_MAP}");
                            }
                        }
                    }
                }
            }
        }

        private void SendWIFIToTracker(byte[] device_addr)
        {
            Log.dongleAPILogger?.WarningLine("Send WIFI to client tracker");
            SendAckTo(device_addr, ConstantsChorusdAck.ACK_WIFI_COUNTRY + wifi_info.country);
            SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_WIFI_FREQ}{wifi_info.host_freq}");
            SendAckTo(device_addr, ConstantsChorusdAck.ACK_WIFI_SSID_FULL + wifi_info.host_ssid.Substring(0, 13));
            for (int i = 13; i < wifi_info.host_ssid.Length; i += 13)
            {
                var len = wifi_info.host_ssid.Length - i;
                if (len == 0) break;
                SendAckTo(device_addr, ConstantsChorusdAck.ACK_WIFI_SSID_APPEND + wifi_info.host_ssid.Substring(i, len));
            }
            SendAckTo(device_addr, ConstantsChorusdAck.ACK_WIFI_PW + wifi_info.host_passwd);
            SendAckTo(device_addr, ConstantsChorusdAck.ACK_FILE_WRITE + 1);
        }

        /// <summary>
        /// Ответ на ACK_AZZ
        /// </summary>
        /// <param name="dev"></param>
        private void AnswerTo_ACK_AZZ(TrackerDeviceInfo dev)
        {
            dev.IsHost = current_host_indx == dev.CurrentIndex;
            var mode = (current_host_indx == dev.CurrentIndex ? 2 : 1);
            var track_mode = (current_host_indx == dev.CurrentIndex ? 21 : 11);//20);
            SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_ARPERSIST_VBP);
            SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_ARPENROLL_UID);
            SendAckTo(dev.CurrentAddress, $"{ConstantsChorusdAck.ACK_FILE_WRITE}{mode}");
            SendAckTo(dev.CurrentAddress, $"{ConstantsChorusdAck.ACK_TRACKING_MODE}{track_mode}");
            if (!dev.IsHost)
            {
                SendAckTo(dev.CurrentAddress, $"{ConstantsChorusdAck.ACK_LAMBDA_COMMAND}{ConstantsChorusdStatus.RESET_MAP}");
            }
        }

        private void SendWIFIConnection(TrackerDeviceInfo dev)
        {
            var device_addr = dev.CurrentAddress;
            var deviceIndx = dev.CurrentIndex;
            var mode = (current_host_indx == deviceIndx ? 2 : 1);

            //WIFI_SetCountry(device_addr, wifi_info.country);
            //WIFI_SetFreq(device_addr, (ushort)wifi_info.host_freq);
            // WIFI_SetSSID(device_addr, wifi_info.host_ssid);
            //WIFI_SetPassword(device_addr, wifi_info.host_passwd);
            //WIFI_ACK_Sync(device_addr, current_host_indx);
            //WIFI_ACK_ConnectionStatus(device_addr);
            //SendAckTo(device_addr, ConstantsChorusdAck.ACK_FILE_WRITE + mode);
            SendWIFIToTracker(device_addr);
            //SendAckTo(device_addr, ConstantsChorusdAck.ACK_WIFI_SETUP);
            if (mode == 1 && !dev.IsConnectedToHost) // is client
            {
                //SendAckTo(device_addr, ConstantsChorusdAck.ACK_WIFI_CONNECT + 1);
                delayedTickActions[ConstantsChorusdAck.ACK_WIFI_SETUP] = (ack) => SendAckTo(device_addr, ack);
            }
            else if (delayedTickActions.ContainsKey(ConstantsChorusdAck.ACK_WIFI_SETUP))
                delayedTickActions.Remove(ConstantsChorusdAck.ACK_WIFI_SETUP);
        }

        void IVIVEDongle.SendAck(int trackerIndex, string ack_command)
        {
            var dev = Get(trackerIndex);
            SendAckTo(dev.CurrentAddress, ack_command);
        }

        protected async void SendAckTo(byte[] mac, string ack)
        {
            var result = await send_cmd_raw(GetAckRequest(DCMD_TX, mac, 0, 1, ack.EncodeFromUTF8()), false, true);
            var answ = result.Length == 1 && result[0] == 3 ? "OK" : BitConverter.ToString(result);

            Log.dongleAPILogger?.Write($"[SEND ACK ");
            Log.dongleAPILogger?.Green($"({mac.ArrayToString(true)}) ");
            Log.dongleAPILogger?.Warning($"({ack}) ");
            Log.dongleAPILogger?.WriteLine($"ANSWER] {answ}");
        }
        protected byte[] GetAckRequest(byte cmd, byte[] mac, byte flag1, byte flag2, byte[] ack)
        {
            byte[] result = new byte[mac.Length + ack.Length + 6]; //6- cmd + fullrequestlen + DCMD_TX + flag1 + flag2 + ackLenInfo
            result[0] = cmd;
            result[1] = (byte)result.Length;
            result[2] = TX_ACK_TO_MAC;
            Array.Copy(mac, 0, result, 3, mac.Length);
            result[9] = flag1;
            result[10] = flag2;
            result[11] = (byte)ack.Length;
            Array.Copy(ack, 0, result, 12, ack.Length);
            return result;
        }
        protected async void SendAckTo(byte idx, string ack)
        {
            if (idx < 0) return;

            var mac = new byte[] { 0x23, 0x30, 0x42, 0xB7, 0x82, 0xD3 }; //0x23, 0x30, 0x42, 0xB7, 0x82, 0xD3
            mac[1] |= idx;
            ///Debug.WriteLine("+++++ " + idx + " ===== " + mac[1]);
            // TX_ACK_TO_MAC checks all MAC bytes,
            // TX_ACK_TO_PARTIAL_MAC checks the first 2
            var preamble = StructConverter.Pack("<BBBBBBBBB", TX_ACK_TO_PARTIAL_MAC, mac[0], mac[1], mac[2], mac[3], mac[4], mac[5], 0x0, 0x1);
            List<byte> data = new List<byte>();
            data.AddRange(preamble);
            data.AddRange(StructConverter.Pack("<B", (byte)ack.Length));
            data.AddRange(ack.EncodeFromUTF8());

            var result = await send_cmd(DCMD_TX, data.ToArray(), false);
            var answ = result.Length == 1 && result[0] == 4 ? "OK" : BitConverter.ToString(result);
            Log.dongleAPILogger?.Write($"[SEND ACK ");
            Log.dongleAPILogger?.Green($"#{idx} "); 
            Log.dongleAPILogger?.Blue($"({ack}) ");
            Log.dongleAPILogger?.WriteLine($"ANSWER] {answ}");
        }

        protected async Task<byte[]> send_cmd(byte cmd_id, byte data, bool showLog, bool waitAnswer = false) => await send_cmd(cmd_id, new byte[] { data }, showLog, waitAnswer);
        protected abstract Task<byte[]> send_cmd(byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false);
        protected abstract Task<byte[]> send_cmd_raw(byte[] data, bool showLog, bool waitAnswer = false);

        protected virtual void SaveTrackerInfo(TrackerDeviceInfo info)
        { }

        /// <summary>
        /// разблокирует ранее привязанные устройства по их индексу
        /// </summary>
        public async void ApplicationStarted()
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x00;
            foreach (var item in indexes)
                flags[item] = 0x01;
            await send_cmd(DCMD_1E, null, true, true); //preambule
            await send_cmd(DCMD_QUERY_ROM_VERSION, StructConverter.Pack($"<BB", 0x00, 0x00), true);
            await send_cmd(DCMD_GET_CR_ID, StructConverter.Pack($"<BB", 0x12, 0x00), true);
            await send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.READY_MODE, 0x01, flags, 0x00), true);
        }
        public async void OpenChannelForScan()
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            foreach (var item in indexes)
                flags[item] = 0x00;
            await send_cmd(DCMD_1E, null, true, true);
            await send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.SCAN_MODE, 0x01, flags, 0x00), true);
        }
        public async void CloseChannelForScan()
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            foreach (var item in indexes)
                flags[item] = 0x00;
            await send_cmd(DCMD_1E, null, true, true);
            await send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.READY_MODE, 0x01, flags, 0x00), true);
        }
        /// <summary>
        /// должно вызываться при выключении программы, при этом статус привязаного устройства меняется на 3000005
        /// </summary>
        public async virtual void CloseApplication()
        {
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            await send_cmd(DCMD_1E, null, true, true);
            await send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.IDLE_MODE, 0x01, flags, 0x00), true);
        }
        public async void UnpairAll()
        {
            //var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            await send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.UNPAIR_MODE, 0x01, flags, 0x00), true);
            DestroyDevices();
            SaveTrackerInfo(null);
        }
        public async void Unpair(int indx)
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            flags[indx] = 0x1;
            await send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.UNPAIR_MODE, 0x01, flags, 0x00), true);
            
            // clear file
            SaveTrackerInfo(null);
            DestroyDevice(indx);
            // create new notes in file
            SaveExistedTrackersInfo();
        }

        protected virtual void ShowInfo()
        {
            //var pcb = await get_PCBID();
            //var sku = await get_SKUID();
            //var sn = await get_SN();
            //var ssn = await get_ShipSN();
            //var fpc = await get_CapFPC();
            //var rom = await get_ROMVersion();
            //RaiseDongleInfoEvent(pcb, sku, sn, ssn, fpc, rom);
        }

        protected void RaiseDongleInfoEvent(KeyValuePair<string, string>[] info)
        {
            OnDongleInfo?.Invoke(info);
        }

        void IVIVEDongle.PowerOffAll()
        {
            SendAckToAll(ConstantsChorusdAck.ACK_POWER_OFF);
        }
        void IVIVEDongle.PowerOff(int indx)
        {
            var dev = Get(indx);
            SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_POWER_OFF);
        }
        void IVIVEDongle.StandByAll()
        {
            SendAckToAll(ConstantsChorusdAck.ACK_STANDBY);
        }
        void IVIVEDongle.PowerOffAllAndClearPairingList()
        {
            SendAckToAll(ConstantsChorusdAck.ACK_POWER_OFF_CLEAR_PAIRING_LIST);
            SendAckToAll(ConstantsChorusdAck.ACK_RESET);
        }

        void IVIVEDongle.ReMap(int currentDeviceIndex)
        {
            ReMap(currentDeviceIndex);
        }
        protected virtual void ReMap(int currentDeviceIndex)
        {
            var dev = Get(currentDeviceIndex);
            if (dev != null)
            {
                registeredActions[ConstantsChorusdAck.ACK_TRACKING_MODE] = (ack) => 
                {
                    registeredActions.Remove(ConstantsChorusdAck.ACK_TRACKING_MODE);
                    SendAckTo(dev.CurrentAddress, ack);
                    SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_END_MAP);
                };
                SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_TRACKING_MODE);
            }
        }

        public void GetDongleInfo()
        {
            ShowInfo();
        }
        public void GetTrackerStatus(int indx)
        {
            var dev = Get(indx);
            if(dev != null)
            {
                OnTrackerStatus?.Invoke(dev);
            }
        }

        public void Restart()
        {
            //StandByAll();
            SendAckToAll(ConstantsChorusdAck.ACK_RESET);
        }

        void handle_disconnected(int idx)
        {
            if (idx < 0)
                return;
            var dev = Get(idx);
            if (dev != null)
            {
                dev.Disconnect();
            }

            if (idx == current_host_indx)
                current_host_indx = -1;

            // recreate host tracker


            if (OnDisconnected != null)
                OnDisconnected(idx);
        }

        #region Helper
        void IVIVEDongle.EndScanMap(int indx)
        {
            var dev = Get(indx);
            if (dev.IsHost)
                SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_END_MAP);
            else Log.dongleAPILogger?.ErrorLine($"[ASK_FOR_END_MAP] Device:SN-{dev.SerialNumber} INDX-{dev.CurrentIndex} is not host");
        }
        void IVIVEDongle.ExperimentalFileDownload(int indx)
        {
            var dev = Get(indx);
            if (dev.IsHost)
                SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_FILE_DOWNLOAD);
        }
        void IVIVEDongle.Blink(int indx)
        {
            var dev = Get(indx);
            SendAckTo(dev.CurrentAddress, $"{ConstantsChorusdAck.ACK_BLINK}1,1,1,1");
        }
        void IVIVEDongle.RequestDeviceInfoFromTracker(int indx)
        {
            var dev = Get(indx);
            SendAckTo(dev.CurrentAddress, $"{ConstantsChorusdAck.ACK_RESEND_DEVICE_INFO}");
        }

        public bool SetRoleID(string serialNumber, int value)
        {
            var dev = Get(serialNumber);
            if(dev != null)
            {
                dev.RoleID = value;
                dev.SaveToFile();
                ACK_SetRoleID(dev.CurrentAddress, (ushort)value);
                return true;
            }
            return false;
        }
        public int GetRoleID(string serialNumber)
        {
            var dev = Get(serialNumber);
            if (dev != null)
            {
                return dev.RoleID;
            }
            return -1;
        }
        void SendAckToAll(string ack, bool force = false)
        {
            for (byte i = 0; i < MAX_TRACKER_COUNT; i++)
            {
                if(force)
                    SendAckTo(i, ack);
                else
                {
                    var dev = Get(i);
                    if (dev != null)
                    {
                        if (dev.CurrentAddress != null)
                            SendAckTo(dev.CurrentAddress, ack.Replace("{0}", i.ToString()));
                        else
                            SendAckTo(i, ack);
                    } 
                }
            }
        }
        /// <summary>
        /// ARI
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="rid"></param>
        void ACK_SetRoleID(byte[] addr, ushort rid) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_ROLE_ID}{rid}");
        /// <summary>
        /// ATM
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="mode"></param>
        void ACK_SetTrackingMode(byte[] addr, int mode) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_TRACKING_MODE}{mode}");
        /// <summary>
        /// ATH
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="host"></param>
        void ACK_SetTrackingHost(byte[] addr, int host) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_TRACKING_HOST}{host}");
        /// <summary>
        /// AWH
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="host"></param>
        void ACK_SetWIFIHost(byte[] addr, int host) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_WIFI_HOST}{host}");
        /// <summary>
        /// ANI
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="id"></param>
        void ACK_SetNewID(byte[] addr, int id) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_NEW_ID}{id}");
        /// <summary>
        /// P63:
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="key_id"></param>
        
        void ACK_LambdaAskStatus(byte[] addr, byte key_id) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_LAMBDA_ASK_STATUS}{key_id}");
        /// <summary>
        /// LP
        /// </summary>
        /// <param name="idx"></param>
        void ACK_LambdaProperty(byte idx) => SendAckTo(idx, ConstantsChorusdAck.ACK_LAMBDA_PROPERTY);


        public static byte MacToIdx(byte[] mac_addr)
        {
            var obj = mac_addr[1];
            return (byte)(obj & 0xF);
        }
        public static string MacToStr(byte[] b) => $"{b[0]:X2}:{b[1]:X2}:{b[2]:X2}:{b[3]:X2}:{b[4]:X2}:{b[5]:X2}";

        public abstract void Dispose();
        #endregion
    }
}
