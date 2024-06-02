using HID_ViveTest.PythonLike;
using System.Linq;

namespace VIVE_Trackers
{
    public struct ResponseStruct
    {
        public byte err;
        public byte cmd_id;
        public byte[] ret;

        public ResponseStruct(byte err, byte cmd_id, byte[] ret)
        {
            this.err = err;
            this.cmd_id = cmd_id;
            this.ret = ret;
        }
        public ResponseStruct(byte cmd_id, byte[] ret)
        {
            this.err = 0;
            this.cmd_id = cmd_id;
            this.ret = ret;
        }

        public static ResponseStruct Parse(byte[] data, bool incoming)
        {
            if (incoming)
                return parse_incoming(data);
            return parse_response(data);
        }

        private static ResponseStruct parse_response(byte[] data)
        {
            if (data == null || data.Length < 5)
            {
                return new ResponseStruct(0, 0, new byte[0]);
            }
            var range = data.Take(5).ToArray();
            var res = StructConverter.Unpack("<BBBH", range);
            int c = (byte)res[2] - 4;
            var ret = data.Skip(5).Take(c).ToArray();
            return new ResponseStruct((byte)res[0], (byte)res[1], ret);
        }
        private static ResponseStruct parse_incoming(byte[] data)
        {
            if (data == null || data.Length < 5)
            {
                return new ResponseStruct(0, new byte[0]);
            }
            // 1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 36 37 38 39
            // 1e-1d-00 00-02-17 16-01-00 03-00-00-01 03-00-00-01 03-00-00-01 03-00-00-01 03-00-00-01 0000000000000000000000000000000000000000000000000000000000000000000000
            // 1e-1d-00 00-02-17 16-01-00 03-00-00-04 03-00-00-04 03-00-00-04 03-00-00-04 03-00-00-04 0000000000000000000000000000000000000000000000000000000000000000000000
            // 1e-1d-00 00-02-17 16-01-00 03-00-00-01 05-00-00-0a 03-00-00-01 03-00-00-01 05-00-00-0a 0000000000000000000000000000000000000000000000000000000000000000000000
            // 28-27-00 23-31-3f-f6-3f-68 10-01-04-15-80-78-18-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-0000000000000000000000000000000000000000000000e701
            // 1D = 29 byte data
            //var range = data.Take(4).ToArray();
            //var res = StructConverter.Unpack("<BBH", data);
            int c = data[1] - 3;
            Log.WriteLine(data.Take(data[1]).ToArray().ArrayToString(true));
            var ret = data.Skip(3).Take(c).ToArray();
            return new ResponseStruct(data[0], ret);
        }
    }
}
