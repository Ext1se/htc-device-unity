using HID_ViveTest.PythonLike;
using System.Linq;

namespace VIVE_Trackers
{
    public struct TrackerIncomingData
    {
        public enum CommandType : ushort
        {
            ACK = 0x101,
            POSE = 0x110
        }
        public byte cmd_id;
        public ushort pkt_indx;
        public byte[] mac;
        public CommandType type;
        public byte[] data_raw;
        public int rawDataLength;

        /// <summary>
        /// parse first 12 bytes cmd_id(1byte), pkt_indx(2byte), mac(6byte), 
        /// type(2byte), data_raw(left data (last byte is length))
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static TrackerIncomingData Parse(byte[] data)
        {
            //example 28-0f-00 23-34-34-7d-68-93 01-01-04 4e415a5a (AZZ)
            int RANGE_SIZE = 0xC;
            var range = data.Take(RANGE_SIZE).ToArray(); //12 bytes
            var res = StructConverter.Unpack("<BH6BHB", range);
            var len = (byte)res[4];
            bool hasEnd = false;
            if (data[data.Length - 2] == 0xEF && data[data.Length - 1] == 0x01 &&
                data[RANGE_SIZE + len - 2] != 0xEF && data[RANGE_SIZE + len - 1] != 0x01)
            {
                len += 2;
                hasEnd = true;
            }
            var raw = data.Skip(RANGE_SIZE).ToArray();
            TrackerIncomingData incData = new TrackerIncomingData
            {
                cmd_id = (byte)res[0],
                pkt_indx = (ushort)res[1],
                mac = (byte[])res[2],
                type = (CommandType)res[3],
                rawDataLength = len,
                data_raw = raw
            };
            //if (hasEnd)
            //{
            //    incData.data_raw[len - 2] = 0xEF;
            //    incData.data_raw[len - 1] = 0x01;
            //}
            return incData;
        }
        public override string ToString()
        {
            return $"cmd_id: 0x{cmd_id:X2}, pkt_idx: 0x{pkt_indx:X2}, mac: {AndroidDongleHID.MacToStr(mac)}, type: {type}, data_len: {data_raw.Length}";
        }
    }
}
