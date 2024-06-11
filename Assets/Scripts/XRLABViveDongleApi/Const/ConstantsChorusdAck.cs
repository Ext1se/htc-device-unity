
namespace VIVE_Trackers.Constants
{
    public static class ConstantsChorusdAck
    {
        public static string[] ACKsConnectedTracker =>
            new string[]
            {
                ACK_NEW_ID, // + index
                ACK_ATW,
                ACK_CAMERA_FPS, // + FPS (60 or 50)
                ACK_TIME_SET, // + UnixTimeStep
                ACK_WIFI_COUNTRY // + country (RU)
            };
        public static string[] ACKsConnectedTrackerAfterAZZ =>
            new string[]
            {
                ACK_ARPERSIST_VBP, // + index
                ACK_ARPENROLL_UID,
                ACK_FILE_WRITE, // + 1 if client or 2 if host
                ACK_TRACKING_MODE, // + 11 if client or 21 if host
            };

        public const string ACK_ARPERSIST_VBP = "ARppersist.vbp.";
        public const string ACK_ARPENROLL_UID = "Arpenroll_uid";
        //
        // ACK CMDs
        //
        // Wifi is limited to those starting with: P, FE?
        public const char ACK_CATEGORY_CALIB_1 = 'C';
        public const char ACK_CATEGORY_CALIB_2 = 'c';
        public const char ACK_CATEGORY_DEVICE_INFO = 'N';
        public const char ACK_CATEGORY_PLAYER = 'P'; // Lambda (SLAM) related cmds

        // ACK_CATEGORY_DEVICE_INFO
        /// <summary>
        /// 
        /// </summary>
        public const string ACK_ANA = "ANA"; // TODO, "OT1"? recv'd from tracker on connect (NANA?)
        /// <summary>
        /// SERIAL NUMBER
        /// </summary>
        public const string ACK_DEVICE_SN = "ADS"; // SERIAL NUMBER BITCH /////////recv'd from tracker on connect (NADS?)
        public const string ACK_SHIP_SN = "ASS"; // recv'd from tracker on connect (NASS?)
        public const string ACK_SKU_ID = "ASI"; // recv'd from tracker on connect (NASI?)
        public const string ACK_PCB_ID = "API"; // recv'd from tracker on connect (NAPI?)
        /// <summary>
        /// Firmware version
        /// </summary>
        public const string ACK_VERSION = "AV1"; // recv'd from tracker on connect (NAV?)
        /// <summary>
        /// Firmware version maybe also
        /// </summary>
        public const string ACK_VERSION_ALT = "Av1"; // not actually sent
        public const string ACK_AZZ = "AZZ"; // NAZZ? no data.
        public const string ACK_AGN = "AGN"; // NAGN? 0,1,0
        public const string ACK_ARI = "ARI"; // NARI?

        public const string ACK_LAMBDA_PROPERTY = "LP"; // identical to AGN? 0,1,0 -- trans_setup, normalmode, 3rdhost. Can also be sent to check status.
        public const string ACK_LAMBDA_STATUS = "LS";

        /// <summary>
        /// запуск беспроводной прошивки ПО трекеров или свистка?
        /// </summary>
        public const string ACK_START_FOTA = "AFM";
        public const string ACK_CAMERA_FPS = "ACF";
        public const string ACK_CAMERA_POLICY = "ACP";
        public const string ACK_TRACKING_MODE = "ATM";
        public const string ACK_TRACKING_HOST = "ATH";
        public const string ACK_WIFI_HOST = "AWH";
        /// <summary>
        /// SetUserTime, calls clock_settime in seconds
        /// </summary>
        public const string ACK_TIME_SET = "ATS"; // 
        public const string ACK_ROLE_ID = "ARI";
        /// <summary>
        /// complicated for some reason, takes a list of ints, fusionmode related
        /// </summary>
        public const string ACK_GET_INFO = "AGI"; // 
        /// <summary>
        /// может не сработать
        /// </summary>
        public const string ACK_START_MAP = "AHE"; // 
        public const string ACK_END_MAP = "ALE";
        /// <summary>
        /// sets DeviceID, WiFi related?
        /// </summary>
        public const string ACK_NEW_ID = "ANI"; // 
        /// <summary>
        /// enables acceleration data?
        /// </summary>
        public const string ACK_ATW = "ATW"; // 

        public const string ACK_POWER_OFF_CLEAR_PAIRING_LIST = "APC";
        public const string ACK_POWER_OFF = "APF";
        public const string ACK_STANDBY = "APS";
        public const string ACK_RESET = "APR";

        public const string ACK_WIFI_SSID_PASS = "WS";
        public const string ACK_WIFI_SSID_FULL = "Ws";
        public const string ACK_WIFI_IP = "WI";
        public const string ACK_WIFI_IP_2 = "Wi";
        public const string ACK_WIFI_CONNECT = "WC";
        public const string ACK_WIFI_COUNTRY = "Wc";
        public const string ACK_WIFI_FREQ = "Wf";
        public const string ACK_WIFI_PW = "Wp";
        public const string ACK_WIFI_SSID_APPEND = "Wt";
        public const string ACK_WIFI_ERROR = "WE";
        public const string ACK_WIFI_HOST_SSID = "WH";

        public const string ACK_FT_KEEPALIVE = "FK";
        public const string ACK_FILE_WRITE = "FW";// TODO
        public const string ACK_FILE_DOWNLOAD = "FD";

        // ACK_CATEGORY_PLAYER
        public const string ACK_LAMBDA_SET_STATUS = "P61:";
        public const string ACK_LAMBDA_ASK_STATUS = "P63:";
        public const string ACK_LAMBDA_COMMAND = "P64:";
        public const string ACK_LAMBDA_MESSAGE = "P82:"; // PR?

        public const ushort LAMBDA_PROP_DEVICE_CONNECTED = 58; // 0x3a
        public const ushort LAMBDA_PROP_GET_STATUS = 61;
        public const ushort LAMBDA_PROP_ASK_STATUS = 63;
        public const ushort LAMBDA_PROP_COMMAND = 64;
        public const ushort LAMBDA_PROP_MESSAGE = 82;
        public const ushort LAMBDA_PROP_SAVE_MAP = 80; // internal

        public const byte LAMBDA_CMD_ASK_ED = 0;
        public const byte LAMBDA_CMD_ASK_MAP = 1;
        public const byte LAMBDA_CMD_ASK_KEYFRAME_SYNC = 2;
        public const byte LAMBDA_CMD_RESET_MAP = 3;

        // P61:0,1

        public const string ACK_ERROR_CODE = "DEC"; // DEC?
        public const string ACK_NA = "NA"; // resends device info?
        public const string ACK_MAP_STATUS = "MS";

        public const ushort ERROR_NO_CAMERA = 1100;
        public const ushort ERROR_CAMERA_SSR_1 = 1121;
        public const ushort ERROR_CAMERA_SSR_2 = 1122;
        public const ushort ERROR_NO_IMU = 1200;
        public const ushort ERROR_NO_POSE = 1300;

        // ACK_LAMBDA_MESSAGE
        public const byte LAMBDA_MESSAGE_ERROR = 0;
        public const byte LAMBDA_MESSAGE_UPDATE_MAP_UUID = 1;
    }
}
