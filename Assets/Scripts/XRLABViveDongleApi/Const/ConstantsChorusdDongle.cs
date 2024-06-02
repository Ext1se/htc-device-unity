namespace VIVE_Trackers.Constants
{
    public class ConstantsChorusdDongle
    {
        //
        // DONGLE COMMANDS (DCMD = dongle command)
        //
        /// <summary>
        /// RF_REPORT_RF_VERSION, [0]->[0,0,0], [1]->[0,0], [2]->[2,0,0,0,...], [3]->[3], [4]->[4]45
        /// 
        /// </summary>
        public const byte DCMD_TX = 0x18;
        /// <summary>
        /// Enters DFU bootloader? accepts 2 bytes, first byte is seemingly not used, second must be 2 or 3?
        /// </summary>
        public const byte DCMD_RESET_DFU = 0x1C;
        /// <summary>
        /// RF_REPORT_CHANGE_BEHAVIOR. Crafts some pkt if given >2 bytes (sent to specific device). First byte must be <7, !=3, !=4, second byte must be 0 or 1
        /// </summary>
        public const byte DCMD_REQUEST_RF_CHANGE_BEHAVIOR = 0x1D; // -> 
        /// <summary>
        /// gets RF_REPORT_CHANGE_BEHAVIOR w/o changing anything, checks if length is 2?
        /// </summary>
        public const byte DCMD_1E = 0x1E;
        /// <summary>
        /// RF_REPORT_RF_MODE_OP? BRICKED MY DONGLE :( Accepts only 1 byte
        /// </summary>
        public const byte DCMD_21 = 0x21;
        /// <summary>
        /// Echoes back the USB buffer, 0x40 bytes
        /// </summary>
        public const byte DCMD_26 = 0x26;
        /// <summary>
        /// RF_REPORT_RF_IDS, "Proprietary" in string. Checks if data is [7] and size is < 4, but does nothing with it.
        /// </summary>
        public const byte DCMD_27 = 0x27;
        /// <summary>
        /// takes [6/7/8/9, ...]
        /// </summary>
        public const byte DCMD_28 = 0x28;

        /// <summary>
        /// flashing related! accepts 5 bytes: <BL, second arg must not be > 0x14000, first arg must not be 1 or 2
        /// </summary>
        public const byte DCMD_FLASH_WRITE_1 = 0x98;
        /// <summary>
        /// flashing related! does a crc32. data length must not exceed 0x3c
        /// </summary>
        public const byte DCMD_FLASH_WRITE_2 = 0x99;
        /// <summary>
        /// flashing related! accepts <BBBBBL, last u32 is the crc32 of the data. finalizes flashing?
        /// </summary>
        public const byte DCMD_FLASH_WRITE_3 = 0x9A;

        /// <summary>
        /// accepts 1 byte.
        /// </summary>
        public const byte DCMD_9E = 0x9E;
        /// <summary>
        /// always returns 0x10 00s if data[0] is not 0x2, otherwise, resets into cmd98..cmd9A data? maybe?
        /// </summary>
        public const byte DCMD_9F = 0x9F;
        /// <summary>
        /// Reboot
        /// </summary>
        public const byte DCMD_EB = 0xEB;

        /// <summary>
        /// writes to ID stuff, accepts a byte idx and data. data must not exceed 0x3d
        /// </summary>
        public const byte DCMD_WRITE_CR_ID = 0xEF; // ->
        /// <summary>
        /// a lot of ID stuff
        /// </summary>
        public const byte DCMD_GET_CR_ID = 0xF0; // -> 
        /// <summary>
        /// wrapper for cmd 0x1D?
        /// </summary>
        public const byte DCMD_F3 = 0xF3;
        /// <summary>
        /// accepts <BBBBBBB, a checksum, 5 bytes (presumably trackers), and a subcmd minimum
        /// </summary>
        public const byte DCMD_F4 = 0xF4;
        /// <summary>
        /// ROM version, does not check input
        /// </summary>
        public const byte DCMD_QUERY_ROM_VERSION = 0xFF;

        // Everything that's showed up in tracker-side binaries, but not in the dongle firmware
        //DCMD_1A = 0x1A // -> GetFusionMode, GetRoleId?
        //DCMD_9C = 0x9C // -> "RequestData"?
        //DCMD_9D = 0x9D // -> "RequestData"?
        //DCMD_A4 = 0xA4 // RequestCap1/RequestCap2?
        //DCMD_FA = 0xFA // -> used in DisableCharging?

        //
        // SUB COMMANDS
        //
        public const byte TX_SUBCMD_0 = 0x00; // returns const
        public const byte TX_SUBCMD_1 = 0x01; // returns const
        public const byte TX_SUBCMD_2 = 0x02; // returns const
        public const byte TX_ACK_TO_MAC = 0x03; // data len must be <=0x2C data[8] must be 10, data[9] must be < 0x10
        public const byte TX_ACK_TO_PARTIAL_MAC = 0x04;
        public const byte TX_SUBCMD_5 = 0x05; // Takes 5 bytes in, sends ACK P:%d to each tracker where %d is the value in the input

        public const byte DCMD_21_SUBCMD_0 = 0x00; // BRICKED MY DONGLE :(
        public const byte DCMD_21_SUBCMD_1 = 0x01; // would have unbricked my dongle if I could actually send USB commands
        public const byte DCMD_21_SUBCMD_2 = 0x02; // flashes some different byte?
        public const byte DCMD_21_SUBCMD_5 = 0x05; // flashes some different byte?
        public const byte DCMD_21_SUBCMD_6 = 0x06; // flashes some different byte?
        public const byte DCMD_21_SUBCMD_7 = 0x07; // restarts?
        public const byte DCMD_21_SUBCMD_8 = 0x08; // restarts?

        // DCMD_REQUEST_RF_CHANGE_BEHAVIOR
        /// <summary>
        /// pair
        /// </summary>
        public const byte RF_BEHAVIOR_PAIR_DEVICE = 0x00; // pair
        /// <summary>
        /// RxPowerSaving
        /// </summary>
        public const byte RF_BEHAVIOR_RX_POWER_SAVING = 0x01; // RxPowerSaving
        /// <summary>
        /// Restart RF
        /// </summary>
        public const byte RF_BEHAVIOR_RESTART_RF = 0x02; // RestartRf
        /// <summary>
        /// Factory reset
        /// </summary>
        public const byte RF_BEHAVIOR_FACTORY_RESET = 0x05; // Factory Reset
        /// <summary>
        /// clears pairing info maybe
        /// </summary>
        public const byte RF_BEHAVIOR_6 = 0x06; // ? clears pairing info maybe

        /// <summary>
        /// accepts 1 byte, might restart?
        /// </summary>
        public const byte DCMD_28_SUBCMD_6 = 0x06; // -> 
        /// <summary>
        /// returns some bytes from RAM, does the same weird check as 0x27
        /// </summary>
        public const byte DCMD_28_SUBCMD_7 = 0x07; // -> 
        /// <summary>
        /// takes 5 bytes, one per tracker presumably, as well as some extra bytes?
        /// </summary>
        public const byte DCMD_28_SUBCMD_8 = 0x08; // -> 
        /// <summary>
        /// takes 5 bytes, one per tracker presumably
        /// </summary>
        public const byte DCMD_28_SUBCMD_9 = 0x09; // -> 

        public const byte DCMD_F4_SUBCMD_0 = 0x00; // -> 00 00 00 00 00 00 2c hex_dump(send_rf_command(0xF4, [0,0,0,0,0,0,0])) -> 01 03 03 03 03 00 2c hex_dump(send_rf_command(0xF4, [1,  1,1,1,1,1,0])) tracker related
        public const byte DCMD_F4_SUBCMD_1 = 0x01; // also accepts <BL
        public const byte DCMD_F4_SUBCMD_2 = 0x02;
        public const byte DCMD_F4_SUBCMD_3 = 0x03;

        // DCMD_GET_CR_ID
        public const byte CR_ID_PCBID = 0x06;
        public const byte CR_ID_SKUID = 0x07;
        public const byte CR_ID_SN = 0x08;
        public const byte CR_ID_SHIP_SN = 0x09;
        public const byte CR_ID_CAP_FPC = 0x11;

        public struct CommandPair
        {
            public byte cmd;
            public byte[] data;
            public CommandPair(byte cmd, byte[] data)
            {
                this.cmd = cmd;
                this.data = data;
            }
        }
        // Safety-oriented lists
        //public enum ResetMode
        //{
        //    AutoConnect,
        //    ManualConnect
        //}
        public enum ApplicationStatus : byte
        {
            SCAN_MODE = 0x00,
            IDLE_MODE = 0x01,
            READY_MODE = 0x02,
            UNPAIR_MODE = 0x06
        }
        public static CommandPair[] DCMDS_THAT_RESTART => new CommandPair[] // возможно работает когда трекер привязан и синхронизирован
        {
            new CommandPair(DCMD_RESET_DFU, null),//new byte[]{0, 2}), //try 2 or 3
            new CommandPair(DCMD_21, null),
            new CommandPair(DCMD_9F, null),
            new CommandPair(DCMD_EB, null)//new byte[]{0x2})
        };
        public static readonly byte[] DCMDS_THAT_WRITE_FLASH = new byte[] { DCMD_21, DCMD_FLASH_WRITE_1, DCMD_FLASH_WRITE_2, DCMD_FLASH_WRITE_3, DCMD_WRITE_CR_ID };
        public static byte[] DCMD_FUZZ_BLACKLIST
        {
            get
            {
                var res = new byte[DCMDS_THAT_RESTART.Length + DCMDS_THAT_WRITE_FLASH.Length];
                DCMDS_THAT_RESTART.CopyTo(res, 0);
                DCMDS_THAT_WRITE_FLASH.CopyTo(res, DCMDS_THAT_RESTART.Length);
                return res;
            }
        }

        //
        // RESPONSE IDs
        //
        public enum DongleResponceCmd : byte
        {
            DRESP_PAIR_EVENT = 0x18,
            //DRESP_TRACKER_NEW_RF_STATUS = 0x1D,
            DRESP_TRACKER_RF_STATUS = 0x1E,
            /// <summary>
            /// события, когда трекеры присоеденены и карта построена
            /// </summary>
            DRESP_TRACKER_INCOMING = 0x28,
        }
    }
}
