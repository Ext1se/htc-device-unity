//#define SHOW_DUMP

using HID_ViveTest.PythonLike;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        protected int current_host_indx = -1;
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
                        var paired_mac_str = MacToStr(paired_mac);
                        Log.WriteLine((is_unpair ? "Unpaired " : "Paired ") + $"{paired_mac_str}, unk:{unk:X}");

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
            Log.WriteLine("PARSE_TRACKER_STATUS");
            var res = StructConverter.Unpack("<6BLLLLL", data);
            var status = (byte[])res[0];
            pair_state = new PairState[MAX_TRACKER_COUNT] { (PairState)(uint)res[1], (PairState)(uint)res[2], (PairState)(uint)res[3], (PairState)(uint)res[4], (PairState)(uint)res[5] };
            UpdateStatus(pair_state);
            
            var pairStr = Array.ConvertAll(pair_state, ps => Enum.IsDefined(typeof(PairState), ps) ? ps.ToString() : $"{(uint)ps:X}");
            Log.WriteLine($"cmd:{status.ArrayToString(true)}, {pairStr[0]}, {pairStr[1]}, {pairStr[2]}, {pairStr[3]}, {pairStr[4]}");

            // Fallback for disconnects
            if (current_host_indx >= 0)
                if (((ushort)pair_state[current_host_indx] & ConstantsChorusdStatus.PAIR_STATE_PAIRED) == 0)
                    handle_disconnected(current_host_indx);


            OnDongleStatus?.Invoke(pair_state);
        }

        protected virtual void ParseTrackerIncoming(byte[] data)
        {
            //Log.WriteLine(BitConverter.ToString(data), Log.LogType.Magenta);
            var incoming = TrackerIncomingData.Parse(data);
            if (incoming.type == TrackerIncomingData.CommandType.ACK)
            {
                ParseIncomingACK(incoming.mac, incoming.data_raw);
                Tick();
                return;
            }
            else if (incoming.type == TrackerIncomingData.CommandType.POSE)
            {
                parse_pose_data(incoming.mac, incoming.data_raw);
                Tick();
                return;
            }
            try
            {
                var data_id = incoming.data_raw[0];
                var data_real = incoming.data_raw.Skip(1).ToArray();
                Log.WriteLine($"   [PARSE_TRACKER_INCOMING (NOT IMPLEMENTED!!!!)] data_id:{data_id:X}");
                HEXDump(data_real);
            }
            catch (Exception ex)
            {
                Log.ErrorLine(ex);
            }
            Tick();
        }

        protected virtual void Tick()
        {
            tick_periodic += 1;
            if (tick_periodic > 1000)
            {
                Log.WriteLine("Tick!");
                for (byte i = 0; i < MAX_TRACKER_COUNT; i++)
                {
                    var _dev = Get(i);
                    if (_dev == null || _dev.CurrentIndex == -1) continue;
                    OnTrackerStatus?.Invoke(_dev);
                    if (_dev.CurrentAddress == null) continue;
                    ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_TRANSMISSION_READY);
                    //ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_CURRENT_MAP_ID);
                    //ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_MAP_STATE);
                    ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_CURRENT_TRACKING_STATE);
                    if (_dev.IsHost)
                    {
                        ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_RECEIVED_HOST_ED);
                        ACK_LambdaAskStatus(_dev.CurrentAddress, ConstantsChorusdStatus.KEY_RECEIVED_HOST_MAP);
                    }
                }
                tick_periodic = 0;
            }
        }

        protected void parse_pose_data(byte[] deviceAddr, byte[] data)
        {
            byte device_idx = MacToIdx(deviceAddr);

            var dev = Get(device_idx);
            if (dev == null)
            {
                InitNewTracker(device_idx, deviceAddr);
                dev = Get(device_idx);
            }
            if (dev.CurrentAddress == null)
                dev.CurrentAddress = deviceAddr;
            dev.FrameIndex += 1;

            if (data.Length == 2)
            {
                //var _res = StructConverter.Unpack("<BB", data);
                Log.WriteLine($"({MacToStr(deviceAddr)}) indx:{data[0]:d3}, state:{data[1]:X2}", Log.LogType.Blue);
                // при нечетном indx приходит инфа о зарядке?
                dev.Update(data[1]);
                if (dev.IsBtnClicked)
                    OnButtonClicked?.Invoke(device_idx);
                if (dev.IsBtnDown)
                    OnButtonDown?.Invoke(device_idx);
                OnTrackerStatus?.Invoke(dev);
                return;
            }
            if (data.Length == 4)
            {
                Log.WarningLine(data.ArrayToString(true));
                dev.Update(data[1]);
                if (dev.IsBtnClicked)
                    OnButtonClicked?.Invoke(device_idx);
                if (dev.IsBtnDown)
                    OnButtonDown?.Invoke(device_idx);
                OnTrackerStatus?.Invoke(dev);
                return;
            }
            if (data.Length != 0x25 && data.Length != 0x27)
            {
                dev.Update(data[1]);
                if (dev.IsBtnClicked)
                    OnButtonClicked?.Invoke(device_idx);
                if (dev.IsBtnDown)
                    OnButtonDown?.Invoke(device_idx);
                OnTrackerStatus?.Invoke(dev);
                Log.WarningLine("Неизвестные данные при связанном устройстве. Length:" + data.Length);
                Log.WarningLine(data.ArrayToString(true));
                //HEXDump(data);
                //Log.WriteLine($"({MacToStr(deviceAddr)}) indx:{data[0]:d3}, state:{data[1]:X2}, unk1:{data[2]:X2}, unk2:{data[3]:X2}", Log.LogType.Blue);
                return;
            }
            
            var res = StructConverter.Unpack("<BB3f4h3h4hB", data.Take(0x25).ToArray());

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
                rot_vel_w = (float)Half.ToHalf(vel_arr[3]),

                tracking_status = (byte)res[6]
            };
            // tracking_status = 2 => pose + rot
            // tracking_status = 3 => rot only
            // tracking_status = 4 => pose frozen (lost tracking), rots

            long delta_ms = dev.DeltaTime;
            dev.Update(trackData.btns, trackData.tracking_status);

            OnTrack?.Invoke(device_idx, trackData, delta_ms);

            if (dev.IsBtnClicked)
                OnButtonClicked?.Invoke(device_idx);
            if (dev.IsBtnDown)
                OnButtonDown?.Invoke(device_idx);
        }

        protected void ParseIncomingACK(byte[] device_addr, byte[] raw_data)
        {
            var data = raw_data.DecodeToUTF8();
            if (raw_data.Length == 0)
            {
                Log.Warning(".");
                return;
            }


            var first = data[0];
            var data_real = data.Substring(1);
            byte deviceIndx = MacToIdx(device_addr);
            string macAddress = MacToStr(device_addr);
            if (current_host_indx == -1)
                current_host_indx = deviceIndx;
            Log.WriteLine($"[PARSE ACK] #{deviceIndx}({macAddress}) len:{data.Length} datastring:({data})");
            TrackerDeviceInfo dev = Get(deviceIndx); 
            if (dev == null)
                dev = SetNew(this, device_addr);
            dev.IsHost = current_host_indx == deviceIndx;
            if (first == ConstantsChorusdAck.ACK_CATEGORY_CALIB_1)
            {
                dev.calib_1 += data_real;
                if (raw_data[raw_data.Length - 2] == 0xEF && raw_data[raw_data.Length - 1] == 0x01)
                    Log.WriteLine($"   Got CALIB_1 ({macAddress}): {dev.calib_1}");
            }
            else if (first == ConstantsChorusdAck.ACK_CATEGORY_CALIB_2)
            {
                dev.calib_2 += data_real; 
                Log.WriteLine($"   Got CALIB_1 ({macAddress}): {dev.calib_1}");
                Log.WriteLine($"   Got CALIB_2 ({macAddress}): {dev.calib_2}");
            }
            else if (first == ConstantsChorusdAck.ACK_CATEGORY_DEVICE_INFO)
            {
                var data_real_ss = data_real.Substring(0, 3);
                Log.WriteLine($"   Got device info ACK ({macAddress}): {data_real_ss}");

                //dev = Get(deviceIndx);
                // Handle post-deviceinfo commands
                switch (data_real_ss)
                {
                    case ConstantsChorusdAck.ACK_AZZ:
                        {
                            dev.IsHost = current_host_indx == deviceIndx;
                            var mode = (current_host_indx == deviceIndx ? 2 : 1);
                            var track_mode = (current_host_indx == deviceIndx ? 21 : 20);
                            SendAckTo(device_addr, ConstantsChorusdAck.ACK_ARPERSIST_VBP);
                            SendAckTo(device_addr, ConstantsChorusdAck.ACK_ARPENROLL_UID);
                            SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_FILE_WRITE}{mode}");
                            SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_TRACKING_MODE}{track_mode}");
                            if (!dev.IsHost)
                            {
                                SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_LAMBDA_COMMAND}{ConstantsChorusdStatus.RESET_MAP}");
                                //SendAckTo(device_addr, $"P94:{ConstantsChorusdStatus.RESET_MAP}");
                            }
                            //if (dev.IsHost)
                            //{
                            //    //comms.lambda_end_map(device_addr)
                            //}
                            //else
                            //{
                            //    SendAckTo(deviceIndx, $"{ConstantsChorusdAck.ACK_LAMBDA_COMMAND}{ConstantsChorusdStatus.RESET_MAP}");
                            //}
                            //SendAckTo(deviceIndx, ConstantsChorusdAck.ACK_FILE_WRITE + "1");
                        }
                        break;
                    case ConstantsChorusdAck.ACK_AGN:

                        break;
                    case ConstantsChorusdAck.ACK_ARI:
                        Log.WarningLine("GET ROLE ID: " + data_real);
                        if (dev != null)
                            dev.RoleID = data_real.After(ConstantsChorusdAck.ACK_ARI).ToInt();
                        break;
                    default:
                        if (dev.Fill(data_real))
                        {
                            if (dev.IsInit)
                            {
                                Log.WarningLine(dev.ToString());
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
                            Log.WriteLine("Ask for map again");
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

                        handle_map_state(deviceIndx, state);
                    }
                    else if (key_id == ConstantsChorusdStatus.KEY_CURRENT_TRACKING_STATE)
                        addendum = $"({ConstantsChorusdStatus.PoseStatusToStr(state)})";
                    else if (key_id == ConstantsChorusdStatus.KEY_CURRENT_MAP_ID)
                    {
                        if (dev.IsHost)
                        {
                            Log.WarningLine("KEY_CURRENT_MAP_ID is " + state + ". COOL!!!!");
                            ACK_LambdaAskStatus(device_addr, ConstantsChorusdStatus.KEY_MAP_STATE);
                        }
                        else
                            Log.ErrorLine("KEY_CURRENT_MAP_ID is " + state + ". But is it client info????");
                    }

                    Log.WriteLine($"   Status returned for SLAM key {ConstantsChorusdStatus.SlamKeyToStr(key_id)} ({macAddress}): {state} {addendum}");
                }
                else
                {
                    Log.WarningLine($"   Got PLAYER UNDEFIINED ACK ({macAddress}): {data_real}");
                }
            }
            else
            {
                if (data.Length < 2)
                {
                    Log.WriteLine("SMALL DATA LENGTH: " + data.Length);
                    return;
                }
                var second = data.Substring(0, 2);
                data_real = data.Substring(2);
                if (second == ConstantsChorusdAck.ACK_LAMBDA_PROPERTY)
                {
                    Log.WriteLine($"   Got LP ACK ({macAddress}): {data.Substring(2)}");
                }
                else if (second == ConstantsChorusdAck.ACK_LAMBDA_STATUS)
                {
                    var int_args = Array.ConvertAll(data_real.Split(","), a => a.ToInt()); // should be 3
                    Log.WriteLine($"   Got LAMBDA_STATUS ACK ({macAddress}): {int_args[0]},{int_args[1]},{int_args[2]}", Log.LogType.Error);
                    SendAckTo(device_addr, ConstantsChorusdAck.ACK_FILE_WRITE + "1");
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
                else if (second == ConstantsChorusdAck.ACK_WIFI_HOST_SSID)
                {
                    var parts = data_real.Split(","); //ssid, passwd, freq
                    Log.WriteLine($"   Got WIFI_HOST_SSID ACK ({macAddress}): ssid={parts[0]}, pass={parts[1]}, freq={parts[2]}");
                    wifi_info = new WIFI_Info { host_ssid = parts[0], host_passwd = parts[1], host_freq = parts[2].ToInt() };
                    wifi_info.Save();
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_SSID_PASS)
                {
                    Log.WriteLine($"   Got WIFI_SSID ACK ({macAddress})");

                    var mode = (current_host_indx == deviceIndx ? 2 : 1);
                    if (string.IsNullOrEmpty(wifi_info.host_ssid)) wifi_info = WIFI_Info.Load();
                    WIFI_SetCountry(device_addr, wifi_info.country);
                    System.Threading.Thread.Sleep(2);
                    WIFI_SetFreq(device_addr, (ushort)wifi_info.host_freq);
                    System.Threading.Thread.Sleep(2);
                    WIFI_SetSSID(device_addr, wifi_info.host_ssid);
                    System.Threading.Thread.Sleep(2);
                    WIFI_SetPassword(device_addr, wifi_info.host_passwd);
                    System.Threading.Thread.Sleep(2);
                    SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_FILE_WRITE}{mode}");
                }
                else if (second == ConstantsChorusdAck.ACK_WIFI_CONNECT)
                {
                    var ret = data_real.ToInt();
                    Log.BlueLine($"   Got WIFI_CONNECT ACK ({macAddress}): {ret}");
                    dev.IsConnectedToHost = (ret > 0);
                }
                else if (second == ConstantsChorusdAck.ACK_MAP_STATUS)
                {
                    var status = Array.ConvertAll(data_real.Split(","), a => a.ToInt());

                    Log.WriteLine($"   Got MAP_STATUS ({macAddress}): {status.ArrayToString()} ({ConstantsChorusdStatus.MapStatusToStr(status[1])})");
                    handle_map_state(deviceIndx, status[1]);

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
                    if (data.Length < 3) return;
                    var third = data.Substring(0, 3);
                    data_real = data.Substring(3);
                    if (third == ConstantsChorusdAck.ACK_POWER_OFF)
                    {
                        Log.WriteLine($".  Got POWER_OFF. ({macAddress})");
                        SendAckTo(device_addr, $"{ConstantsChorusdAck.ACK_STANDBY}");
                        dev.status = TrackData.Status.PowerOff;
                        OnTrackerStatus?.Invoke(dev);
                        handle_disconnected(deviceIndx);
                        CloseChannelForScan();
                    }
                    else if (third == ConstantsChorusdAck.ACK_RESET)
                    {
                        Log.WriteLine($".  Got RESET ({macAddress}).");
                        dev.status = TrackData.Status.Reset;
                        OnTrackerStatus?.Invoke(dev);
                        handle_disconnected(deviceIndx);
                        CloseChannelForScan();
                    }
                    else
                    {
                        if (data.Length < 4) return;
                        var fourth = data.Substring(0, 4);
                        data_real = data.Substring(4);
                        if (fourth == ConstantsChorusdAck.ACK_ERROR_CODE)
                        {
                            Log.ErrorLine($"   Got ERROR ({macAddress}): {data_real}");
                        }
                        else
                        {
                            Log.WriteLine($"   Got undefined ACK ({macAddress}): dataStr:({data}) raw:({BitConverter.ToString(raw_data)})");
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
            Console.WriteLine();
        }
        /// <summary>
        /// Обработка состояния карты
        /// </summary>
        /// <param name="device_idx"></param>
        /// <param name="state"></param>
        protected void handle_map_state(byte device_idx, int state)
        {
            var dev = Get(device_idx);
            dev.UpdateMapState(device_idx, state);
        }

        protected void SendAckTo(byte[] mac, string ack)
        {
            var result = send_cmd_raw(GetAckRequest(DCMD_TX, mac, 0, 1, ack.EncodeFromUTF8()), false, true);
            var answ = result.Length == 1 && result[0] == 3 ? "OK" : BitConverter.ToString(result);

            Log.Write($"[SEND ACK ");
            Log.Write($"({mac.ArrayToString(true)}) ", Log.LogType.Green);
            Log.Write($"({ack}) ", Log.LogType.Blue);
            Log.WriteLine($"ANSWER] {answ}");
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
        protected void SendAckTo(byte idx, string ack)
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

            var result = send_cmd(DCMD_TX, data.ToArray(), false);
            var answ = result.Length == 1 && result[0] == 4 ? "OK" : BitConverter.ToString(result);
            Log.Write($"[SEND ACK ");
            Log.Write($"#{idx} ", Log.LogType.Green); 
            Log.Write($"({ack}) ", Log.LogType.Blue);
            Log.WriteLine($"ANSWER] {answ}");
        }

        protected byte[] send_cmd(byte cmd_id, byte data, bool showLog, bool waitAnswer = false) => send_cmd(cmd_id, new byte[] { data }, showLog, waitAnswer);
        protected abstract byte[] send_cmd(byte cmd_id, byte[] data, bool showLog, bool waitAnswer = false);
        protected abstract byte[] send_cmd_raw(byte[] data, bool showLog, bool waitAnswer = false);

        protected virtual void SaveTrackerInfo(TrackerDeviceInfo info)
        { }

        /// <summary>
        /// разблокирует ранее привязанные устройства по их индексу
        /// </summary>
        public void ApplicationStarted()
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x00;
            foreach (var item in indexes)
                flags[item] = 0x01;
            send_cmd(DCMD_1E, null, true, false); //preambule
            send_cmd(DCMD_QUERY_ROM_VERSION, StructConverter.Pack($"<BB", 0x00, 0x00), true);
            send_cmd(DCMD_GET_CR_ID, StructConverter.Pack($"<BB", 0x12, 0x00), true);
            send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.READY_MODE, 0x01, flags, 0x00), true);
        }

        public void OpenChannelForScan()
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            foreach (var item in indexes)
                flags[item] = 0x00;
            send_cmd(DCMD_1E, null, true, false);
            send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.SCAN_MODE, 0x01, flags, 0x00), true);
        }
        public void CloseChannelForScan()
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            foreach (var item in indexes)
                flags[item] = 0x00;
            send_cmd(DCMD_1E, null, true, false);
            send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.READY_MODE, 0x01, flags, 0x00), true);
        }
        /// <summary>
        /// должно вызываться при выключении программы, при этом статус привязаного устройства меняется на 3000005
        /// </summary>
        public virtual void CloseApplication()
        {
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            send_cmd(DCMD_1E, null, true, false);
            send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.IDLE_MODE, 0x01, flags, 0x00), true);
        }

        public void UnpairAll()
        {
            //var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = 0x1;
            send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.UNPAIR_MODE, 0x01, flags, 0x00), true);
            DestroyDevices();
            SaveTrackerInfo(null);
        }
        public void Unpair(int indx)
        {
            var indexes = GetActiveIndexes();
            byte[] flags = new byte[MAX_TRACKER_COUNT];
            flags[indx] = 0x1;
            send_cmd(DCMD_REQUEST_RF_CHANGE_BEHAVIOR, StructConverter.Pack($"<BB{MAX_TRACKER_COUNT}BB", ApplicationStatus.UNPAIR_MODE, 0x01, flags, 0x00), true);
            
            // clear file
            SaveTrackerInfo(null);
            DestroyDevice(indx);
            // create new notes in file
            SaveExistedTrackersInfo();
        }

        private void ShowInfo()
        {
            //Console.WriteLine($"PCBID: {get_PCBID()}"); //equestPCBID
            //Console.WriteLine($"SKUID: {get_SKUID()}"); //RequestSKUID
            //Console.WriteLine($"SN: {get_SN()}"); // RequestSN
            //Console.WriteLine($"ShipSN: {get_ShipSN()}"); // RequestShipSN
            //Console.WriteLine($"CapFPC: {get_CapFPC()}"); // RequestCapFPC
            //Console.WriteLine($"ROMVersion: {get_ROMVersion()}"); // QueryROMVersion

            OnDongleInfo?.Invoke(new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("PCBID", get_PCBID()),
                    new KeyValuePair<string, string>("SKUID", get_SKUID()),
                    new KeyValuePair<string, string>("SN", get_SN()),
                    new KeyValuePair<string, string>("ShipSN", get_ShipSN()),
                    new KeyValuePair<string, string>("CapFPC", get_CapFPC()),
                    new KeyValuePair<string, string>("ROMVersion", get_ROMVersion())
                });
        }

        public void PowerOffAll()
        {
            SendAckToAll(ConstantsChorusdAck.ACK_POWER_OFF);
        }
        public void StandByAll()
        {
            SendAckToAll(ConstantsChorusdAck.ACK_STANDBY);
        }
        public void PowerOffAllAndClearPairingList()
        {
            SendAckToAll(ConstantsChorusdAck.ACK_POWER_OFF_CLEAR_PAIRING_LIST);
            SendAckToAll(ConstantsChorusdAck.ACK_RESET);
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
        protected string get_PCBID() => send_cmd(DCMD_GET_CR_ID, CR_ID_PCBID, false).DecodeToUTF8();
        protected string get_SKUID() => send_cmd(DCMD_GET_CR_ID, CR_ID_SKUID, false).DecodeToUTF8();
        protected string get_SN() => send_cmd(DCMD_GET_CR_ID, CR_ID_SN, false).DecodeToUTF8();
        protected string get_ShipSN() => send_cmd(DCMD_GET_CR_ID, CR_ID_SHIP_SN, false).DecodeToUTF8();
        protected string get_CapFPC() => send_cmd(DCMD_GET_CR_ID, CR_ID_CAP_FPC, false).DecodeToUTF8();
        protected string get_ROMVersion() => send_cmd(DCMD_QUERY_ROM_VERSION, 0x00, false).DecodeToUTF8();

        void IVIVEDongle.ScanMap(int indx)
        {
            var dev = Get(indx);
            //LambdaEndMap(dev.CurrentAddress);
            if (dev.IsHost)
                SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_START_MAP);
            else Log.ErrorLine($"[ASK_FOR_MAP] Device:SN-{dev.SerialNumber} INDX-{dev.CurrentIndex} is not host");
        }
        void IVIVEDongle.EndScanMap(int indx)
        {
            var dev = Get(indx);
            //LambdaEndMap(dev.CurrentAddress);
            if (dev.IsHost)
                SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_END_MAP);
            else Log.ErrorLine($"[ASK_FOR_END_MAP] Device:SN-{dev.SerialNumber} INDX-{dev.CurrentIndex} is not host");
        }
        void IVIVEDongle.ExperimentalFileDownload(int indx)
        {
            var dev = Get(indx);
            //LambdaEndMap(dev.CurrentAddress);
            if (dev.IsHost)
                SendAckTo(dev.CurrentAddress, ConstantsChorusdAck.ACK_FILE_DOWNLOAD);
        }
        //private void LambdaEndMap(byte[] addr)
        //{
        //    //Log.WriteLine("END MAP", Log.LogType.Green);
        //    SendAckTo(addr, ConstantsChorusdAck.ACK_END_MAP);
        //}

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
        void WIFI_Connect(byte[] addr)
        {
            SendAckTo(addr, ConstantsChorusdAck.ACK_WIFI_CONNECT);
        }
        void WIFI_SetSSID(byte[] addr, string ssid)
        {
            Log.WriteLine(ssid.Substring(0, 13));
            WIFI_SetSSIDFull(addr, ssid.Substring(0, 13));

            for (int i = 13; i < ssid.Length; i += 13)
            {
                var len = ssid.Length - i;
                if (len == 0) break;
                WIFI_SetSSIDAppend(addr, ssid.Substring(i, len));
            }
        }

        /// <summary>
        /// Ws
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="ssid"></param>
        void WIFI_SetSSIDFull(byte[] addr, string ssid) => SendAckTo(addr, ConstantsChorusdAck.ACK_WIFI_SSID_FULL + ssid);
        /// <summary>
        /// Wt
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="ssid"></param>
        void WIFI_SetSSIDAppend(byte[] addr, string ssid) => SendAckTo(addr, ConstantsChorusdAck.ACK_WIFI_SSID_APPEND + ssid);
        /// <summary>
        /// Wc
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="country"></param>
        void WIFI_SetCountry(byte[] addr, string country) => SendAckTo(addr, ConstantsChorusdAck.ACK_WIFI_COUNTRY + country);
        /// <summary>
        /// Wp
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="pass"></param>
        void WIFI_SetPassword(byte[] addr, string pass) => SendAckTo(addr, ConstantsChorusdAck.ACK_WIFI_PW + pass);
        /// <summary>
        /// Wf
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="freq"></param>
        void WIFI_SetFreq(byte[] addr, ushort freq) => SendAckTo(addr, $"{ConstantsChorusdAck.ACK_WIFI_FREQ}{freq}");

        /// <summary>
        /// ARI
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="rid"></param>
        void ACK_SetRoleID(byte[] addr, ushort rid) =>
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_ROLE_ID}{rid}");
        /// <summary>
        /// ATM
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="mode"></param>
        void ACK_SetTrackingMode(byte[] addr, int mode) =>
            SendAckTo(addr, $"{ConstantsChorusdAck.ACK_TRACKING_MODE}{mode}");
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
        void ACK_LambdaAskStatus(int idx, byte key_id) => SendAckTo((byte)idx, $"{ConstantsChorusdAck.ACK_LAMBDA_ASK_STATUS}{key_id}");
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

        static void HEXDump(byte[] b, int startIndex = 0, int count = 0, string prefix = "", bool hexView = true)
        {
#if SHOW_DUMP
            Log.WriteLine("--------------------------DUMP--------------------------------", Log.LogType.Blue);
            //Debug.WriteLine("--------------------------DUMP--------------------------------");
            string str = prefix;
            if (count <= 0)
                count = b.Length;
            for (int i = startIndex; i < count; i++)
            {
                if (i % 8 == 0)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        //Debug.WriteLine(str);

                        if (str != prefix && !string.IsNullOrEmpty(str) && i > 0)
                        {
                            str += "    ";
                            for (int j = i - 8; j < i; j++)
                            {
                                var ch = (char)b[j];
                                if (!char.IsSymbol(ch))
                                    ch = '.';
                                if (ch == '\n' || ch == '\r')
                                    ch = '.';
                                if (b[j] == 0 || b[j] == 1)
                                    ch = '.';
                                str += ch + " ";
                            }
                        }
                        Log.WriteLine(str, Log.LogType.Blue);
                    }
                    str = prefix + (hexView ? b[i].ToString("X2") : b[i].ToString().PadLeft(3)) + "  ";
                }
                else str += (hexView ? b[i].ToString("X2") : b[i].ToString().PadLeft(3)) + "  ";
            }
            if (str != prefix && !string.IsNullOrEmpty(str))
            {
                str += "    ";
                for (int j = count - 8; j < count; j++)
                {
                    var ch = (char)b[j];
                    if (!char.IsSymbol(ch))
                        ch = '.';
                    if (ch == '\n' || ch == '\r')
                        ch = '.';
                    if (b[j] == 0 || b[j] == 1)
                        ch = '.';
                    str += ch + " ";
                }
            }
            Log.WriteLine(str, Log.LogType.Blue);
            Log.WriteLine("--------------------------------------------------------------", Log.LogType.Blue); 
#endif
        }

        public abstract void Dispose();
        #endregion

        #region Unused
        //protected static int do_u8_checksum(IEnumerable<byte> data)
        //{
        //    var _out = 0;
        //    foreach (var item in data)
        //    {
        //        _out ^= item;
        //    }
        //    return _out;
        //}
        //protected static byte[] send_raw(HidStream stream, byte[] data, bool pad = true)
        //{
        //    if (data == null)
        //        data = new byte[0];
        //    int BUFFER_SIZE = 0x41;
        //    List<byte> output = new List<byte>(BUFFER_SIZE); // 65 byte for command
        //    output.AddRange(data);
        //    if (pad)
        //        output.AddRange(new byte[Math.Max(0, BUFFER_SIZE - output.Count)]); // заполняем остальнное нулями до размера 65

        //    //print(f"Sending raw:")
        //    //hex_dump(out)

        //    try
        //    {
        //        stream.SetFeature(output.ToArray());
        //        var resp = new byte[BUFFER_SIZE];
        //        stream.GetFeature(resp);
        //        //hex_dump(resp)
        //        return resp;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine(ex.ToString());
        //        return new byte[0];
        //    }
        //}
        //protected static byte[] send_F4_to_all(HidStream stream, byte subcmd, byte[] data)
        //{
        //    return send_F4(stream, new byte[] { 1, 1, 1, 1, 1 }, subcmd, data);
        //}
        //protected static byte[] send_F4(HidStream stream, byte[] trackers, byte subcmd, byte[] data)
        //{
        //    if (data == null)
        //        data = new byte[0];
        //    if (trackers.Length != 5)
        //        return new byte[0];

        //    int BUFFER_SIZE = 0x40;
        //    List<byte> checksummed_data = new List<byte>(BUFFER_SIZE); // 65 byte for command
        //    checksummed_data.AddRange(trackers);
        //    checksummed_data.Add(subcmd);
        //    checksummed_data.AddRange(data);

        //    var out_data = do_u8_checksum(checksummed_data);// + checksummed_data;
        //    checksummed_data.Insert(0, (byte)out_data);
        //    var arr = checksummed_data.ToArray();
        //    HEXDump(arr);
        //    return send_cmd(stream, ConstantsChorusdDongle.DCMD_F4, arr);
        //}
        #endregion
    }
}
