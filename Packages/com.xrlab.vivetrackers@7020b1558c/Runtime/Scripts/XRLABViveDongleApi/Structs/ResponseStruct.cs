using HID_ViveTest.PythonLike;
using System.Linq;

namespace VIVE_Trackers
{
    public struct ResponseStruct
    {
        public byte err;
        public byte cmd_id;
        public byte[] ret;

        public bool IsEmpty => ret == null || ret.Length == 0;

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
            int c = data[1] - 3;
            //Log.WriteLine(data.Take(data[1]).ToArray().ArrayToString(true));
            var ret = data.Skip(3).Take(c).ToArray();
            return new ResponseStruct(data[0], ret);
        }
    }
}
