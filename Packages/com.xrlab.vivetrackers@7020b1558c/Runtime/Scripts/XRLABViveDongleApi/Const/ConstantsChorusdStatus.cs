namespace VIVE_Trackers.Constants
{
    public enum MapStatus
    {
        MAP_NOT_CHECKED = 0,
        MAP_EXIST,
        MAP_NOTEXIST,
        MAP_REBUILT,
        MAP_SAVE_OK,
        MAP_SAVE_FAIL,
        MAP_REUSE_OK,
        MAP_REUSE_FAIL_FEATURE_DIFF,
        MAP_REUSE_FAIL_FEATURE_LESS,
        MAP_REBUILD_WAIT_FOR_STATIC,
        MAP_REBUILD_CREATE_MAP
    }
    internal class ConstantsChorusdStatus
    {
        // Work state enum, set by HORUS_CMD_POWER
        public const byte WS_STANDBY = 0x0;
        public const byte WS_CONNECTING = 0x1;
        public const byte WS_REPAIRING = 0x2;
        public const byte WS_CONNECTED = 0x3;
        public const byte WS_TRACKING = 0x4;
        public const byte WS_RECOVERY = 0x5;
        public const byte WS_REBOOT = 0x6;
        public const byte WS_SHUTDOWN = 0x7;
        public const byte WS_OCVR = 0x8;
        public const byte WS_PCVR = 0x9;
        public const byte WS_EYVR = 0xa;
        public const byte WS_RESTART = 0xb;

        // Error returns
        public const byte ERR_BUSY = 0x2;
        public const byte ERR_03 = 0x3;
        public const byte ERR_UNSUPPORTED = 0xEE;

        // mask
        public const ushort PAIRSTATE_1 = 0x0001;
        public const ushort PAIRSTATE_2 = 0x0002;
        public const ushort PAIRSTATE_4 = 0x0004;
        public const ushort PAIR_STATE_PAIRED = 0x0008;
        public const ushort PAIRSTATE_10 = 0x0010;
        public const int PAIR_STATE_UNPAIRED = 0x01;

        // SetStatus/GetStatus
        public const byte HDCC_BATTERY = 0x0;
        public const byte HDCC_IS_CHARGING = 0x1;
        public const byte HDCC_POGO_PINS = 0x3;
        public const byte HDCC_DEVICE_ID = 0x4;
        public const byte HDCC_TRACKING_HOST = 0x5;
        public const byte HDCC_WIFI_HOST = 0x6;
        public const byte HDCC_7 = 0x7; // LED?
        public const byte HDCC_FT_OVER_WIFI = 0x8;
        public const byte HDCC_ROLE_ID = 0xA;
        public const byte HDCC_WIFI_CONNECTED = 0xC;
        public const byte HDCC_HID_CONNECTED = 0xD;
        public const byte HDCC_E = 0xE; // related to ROLE_ID? Sent on pairing.
        public const byte HDCC_WIFI_ONLY_MODE = 0xF;
        public const byte HDCC_10 = 0x10; // pose related

        public const short TRACKING_MODE_NONE = -1; // checks persist.lambda.3rdhost

        public const short TRACKING_MODE_HOST_ON = 21;
        public const short TRACKING_MODE_HOST_OFF = 20;
        /// <summary>
        /// Client mode
        /// </summary>
        public const short TRACKING_MODE_1 = 1;
        /// <summary>
        /// Host mode
        /// </summary>
        public const short TRACKING_MODE_2 = 2;
        public const short TRACKING_MODE_CLIENT_ON = 11;
        public const short TRACKING_MODE_CLIENT_OFF = 10;
        //public const short TRACKING_MODE_21 = 21; // body tracking? persist.lambda.3rdhost

        public const short TRACKING_MODE_51 = 51; // SetUVCStatus?

        // GET_STATUS
        public const byte KEY_TRANSMISSION_READY = 0;
        public const byte KEY_RECEIVED_FIRST_FILE = 1;
        public const byte KEY_RECEIVED_HOST_ED = 2;
        public const byte KEY_RECEIVED_HOST_MAP = 3;
        public const byte KEY_CURRENT_MAP_ID = 4;
        public const byte KEY_MAP_STATE = 5;
        public const byte KEY_CURRENT_TRACKING_STATE = 6;

        private static readonly string[] _slam_key_strs = new string[] { "TRANSMISSION_READY", "RECEIVED_FIRST_FILE", "RECEIVED_HOST_ED", "RECEIVED_HOST_MAP", "CURRENT_MAP_ID", "MAP_STATE", "CURRENT_TRACKING_STATE" };
        public static string SlamKeyToStr(int idx) => _slam_key_strs[idx];

        // Commands
        public const byte ASK_ED = 0;
        public const byte ASK_MAP = 1;
        public const byte KF_SYNC = 2;
        public const byte RESET_MAP = 3;


        private static readonly string[] _map_status_strs = new string[] { "MAP_NOT_CHECKED", "MAP_EXIST", "MAP_NOTEXIST", "MAP_REBUILT", "MAP_SAVE_OK", "MAP_SAVE_FAIL", "MAP_REUSE_OK", "MAP_REUSE_FAIL_FEATURE_DIFF", "MAP_REUSE_FAIL_FEATURE_LESS", "MAP_REBUILD_WAIT_FOR_STATIC", "MAP_REBUILD_CREATE_MAP" };
        public static string MapStatusToStr(int idx) => _map_status_strs[idx];

        // Map status
        public const byte MAP_NOT_CHECKED = 0;
        public const byte MAP_EXIST = 1;
        public const byte MAP_NOTEXIST = 2;
        public const byte MAP_REBUILT = 3;
        public const byte MAP_SAVE_OK = 4;
        public const byte MAP_SAVE_FAIL = 5;
        public const byte MAP_REUSE_OK = 6;
        public const byte MAP_REUSE_FAIL_FEATURE_DIFF = 7;
        public const byte MAP_REUSE_FAIL_FEATURE_LESS = 8;
        public const byte MAP_REBUILD_WAIT_FOR_STATIC = 9;
        public const byte MAP_REBUILD_CREATE_MAP = 10;

        // Pose state
        private static readonly string[] _pose_status_strs = new string[] { "NO_IMAGES_YET", "NOT_INITIALIZED", "OK", "SETUP_REQUIRED", "RECENTLY_LOST", "SYSTEM_NOT_READY" };
        public static string PoseStatusToStr(int idx) => idx < _pose_status_strs.Length ? _pose_status_strs[idx] : "UNK-" + idx;

        public const short POSE_SYSTEM_NOT_READY = -1;
        public const short POSE_NO_IMAGES_YET = 0;
        public const short POSE_NOT_INITIALIZED = 1;
        public const short POSE_OK = 2;
        public const short POSE_LOST = 3;
        public const short POSE_RECENTLY_LOST = 4;

        // imu state
        public const byte POSESTATE_OK = 0;
        public const byte POSESTATE_LOST = 1;
        public const byte POSESTATE_UNINITIALIZED = 2;
        public const byte POSESTATE_RECOVER = 3;
        public const byte POSESTATE_FOV_BOUNDARY = 4;
        public const byte POSESTATE_FOV_OCCLUSION = 5;
        public const byte POSESTATE_DEAD_ZONE = 6;
        public const byte POSESTATE_NOMEASUREMENT = 7;
        public const byte POSESTATE_NONCONVERGE = 8;
        public const byte POSESTATE_IK = 9;
        public const byte POSESTATE_INTEGRATOR = 10;
        public const byte POSESTATE_NEW_MAP = 11;

        public const byte SYSTEM_STATE_NONE = 0;
        public const byte SYSTEM_STATE_INIT = 1;
        public const byte SYSTEM_STATE_SAVE_ROOM_SETUP = 2;
        public const byte SYSTEM_STATE_ACTIVE = 3;
    }
}
