using System.Text;

namespace HID_ViveTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    // https://stackoverflow.com/a/28418846/26923
    // This is a crude implementation of a format string based struct converter for C#.
    // This is probably not the best implementation, the fastest implementation, the most bug-proof implementation, or even the most functional implementation.
    // It's provided as-is for free. Enjoy.

    namespace PythonLike
    {
        public class StructConverter
        {
            // We use this function to provide an easier way to type-agnostically call the GetBytes method of the BitConverter class.
            // This means we can have much cleaner code below.
            private static byte[] TypeAgnosticGetBytes(object o, Encoding enc = null)
            {
                switch (o)
                {
                    case char c:
                        return BitConverter.GetBytes(c);
                    case string s:
                        if (enc == null)
                            enc = System.Text.Encoding.UTF8;
                        return enc.GetBytes(s);
                    case int i:
                        return BitConverter.GetBytes(i);
                    case uint u:
                        return BitConverter.GetBytes(u);
                    case long l:
                        return BitConverter.GetBytes(l);
                    case ulong @ulong:
                        return BitConverter.GetBytes(@ulong);
                    case short s:
                        return BitConverter.GetBytes(s);
                    case ushort @ushort:
                        return BitConverter.GetBytes(@ushort);
                    case byte _:
                    case sbyte _:
                        return new[] { (byte)o };
                    default:
                        throw new ArgumentException("Unsupported object type found");
                }
            }

            private static string GetFormatSpecifierFor(object o)
            {
                switch (o)
                {
                    case char _:
                        return "c";
                    case string _:
                        return "s";
                    case int _:
                        return "i";
                    case uint _:
                        return "I";
                    case long _:
                        return "q";
                    case ulong _:
                        return "Q";
                    case short _:
                        return "h";
                    case ushort _:
                        return "H";
                    case byte _:
                        return "B";
                    case sbyte _:
                        return "b";
                    default:
                        throw new ArgumentException("Unsupported object type found");
                }
            }

            /// <summary>
            /// Convert a byte array into an array of objects based on Python's "struct.unpack" protocol.
            /// </summary>
            /// <param name="fmt">A "struct.pack"-compatible format string</param>
            /// <param name="bytes">An array of bytes to convert to objects</param>
            /// <returns>Array of objects.</returns>
            /// <remarks>You are responsible for casting the objects in the array back to their proper types.</remarks>
            public static object[] Unpack(string fmt, byte[] bytes)
            {
                //Debug.WriteLine("Format string is length {0}, {1} bytes provided.", fmt.Length, bytes.Length);

                // First we parse the format string to make sure it's proper.
                if (fmt.Length < 1) throw new ArgumentException("Format string cannot be empty.");

                bool endianFlip = false;
                if (fmt.Substring(0, 1) == "<")
                {
                    //Debug.WriteLine("  Endian marker found: little endian");
                    // Little endian.
                    // Do we need to flip endianness?
                    if (BitConverter.IsLittleEndian == false) endianFlip = true;
                    fmt = fmt.Substring(1);
                }
                else if (fmt.Substring(0, 1) == ">")
                {
                    //Debug.WriteLine("  Endian marker found: big endian");
                    // Big endian.
                    // Do we need to flip endianness?
                    if (BitConverter.IsLittleEndian) endianFlip = true;
                    fmt = fmt.Substring(1);
                }

                // Now, we find out how long the byte array needs to be
                int totalByteLength = 0;
                string digit = "";
                foreach (char c in fmt)
                {
                    //Debug.WriteLine("  Format character found: {0}", c);
                    int charCount = 1;
                    if (char.IsDigit(c))
                    {
                        digit += c;
                        continue;
                    }
                    else if (digit != "")
                    {
                        charCount = int.Parse(digit);
                        digit = "";
                    }
                    switch (c)
                    {
                        case 'c':
                        case 's':
                            totalByteLength += charCount;
                            break;
                        case 'q':
                        case 'Q':
                            totalByteLength += 8 * charCount;
                            break;
                        case 'f':
                            totalByteLength += 4 * charCount;
                            break;
                        case 'd':
                            totalByteLength += 8 * charCount;
                            break;
                        case 'i':
                        case 'I':
                        case 'l':
                        case 'L':
                            totalByteLength += 4 * charCount;
                            break;
                        case 'h':
                        case 'H':
                            totalByteLength += 2 * charCount;
                            break;
                        case 'b':
                        case 'B':
                        case 'x':
                            totalByteLength += 1 * charCount;
                            break;
                        default:
                            throw new ArgumentException("Invalid character found in format string.");
                    }
                }

                //Debug.WriteLine("Endianness will {0}be flipped.", (object)(endianFlip ? "" : "NOT "));
                //Debug.WriteLine("The byte array is expected to be {0} bytes long.", totalByteLength);

                // Test the byte array length to see if it contains as many bytes as is needed for the string.
                if (bytes.Length < totalByteLength) throw new ArgumentException("The number of bytes provided does not match the total length of the format string.");

                // Ok, we can go ahead and start parsing bytes!
                int byteArrayPosition = 0;
                var outputList = new List<object>();

                //Debug.WriteLine("Processing byte array...");
                digit = "";

                foreach (char c in fmt)
                {
                    byte[] buf;
                    int charCount = 1;
                    if(char.IsDigit(c))
                    {
                        digit += c;
                        continue;
                    }
                    else if(digit != "")
                    {
                        charCount = int.Parse(digit);
                        digit = "";
                    }
                    switch (c)
                    {
                        case 's':
                            //outputList.Add(BitConverter.ToString(bytes, byteArrayPosition, charCount));
                            //char[] arr = new char[charCount];
                            //for (int i = 0; i < charCount; i++)
                            //    arr[i] = (char)bytes[byteArrayPosition + i];
                            //outputList.Add(arr);
                            string str = Encoding.UTF8.GetString(bytes, byteArrayPosition, charCount);
                            outputList.Add(str);
                            byteArrayPosition += charCount;
                            //Debug.WriteLine("  Added string.");
                            break;
                        case 'c':
                            char[] arr = new char[charCount];
                            for (int i = 0; i < charCount; i++)
                            {
                                char ch = (char)bytes[byteArrayPosition];
                                arr[i] = ch;
                                byteArrayPosition += 1;
                            }
                            if(charCount == 1)
                                outputList.Add(arr[0]);
                            else
                                outputList.Add(arr);
                            //Debug.WriteLine("  Added string.");
                            break;
                        case 'q':
                            byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 8,
                                BitConverter.ToInt64);
                            //Debug.WriteLine("  Added signed 64-bit integer.");
                            break;
                        case 'Q':
                            byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 8,
                                BitConverter.ToUInt64);
                            //Debug.WriteLine("  Added unsigned 64-bit integer.");
                            break;
                        case 'l':
                            byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 4,
                                BitConverter.ToInt32);
                            //Debug.WriteLine("  Added signed 32-bit integer.");
                            break;
                        case 'L':
                            byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 4,
                                BitConverter.ToUInt32);
                            //Debug.WriteLine("  Added unsignedsigned 32-bit integer.");
                            break;
                        case 'f':
                            byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 4,
                                BitConverter.ToSingle);
                            //Debug.WriteLine("  Added signed 16-bit integer.");
                            break;
                        case 'd':

                            byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 8,
                                BitConverter.ToDouble);
                            //Debug.WriteLine("  Added signed 16-bit integer.");
                            break;
                        case 'h':
                            {
                                byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 2,
                                    BitConverter.ToInt16);
                            }
                            //Debug.WriteLine("  Added signed 16-bit integer.");
                            break;
                        case 'H':
                            {

                                byteArrayPosition = AddElement(bytes, endianFlip, byteArrayPosition, outputList, charCount, 2,
                                    BitConverter.ToUInt16);
                            }
                            break;
                        case 'b':
                            var _buf = new sbyte[charCount];
                            Array.Copy(bytes, byteArrayPosition, _buf, 0, charCount);
                            if (charCount == 1)
                                outputList.Add(_buf[0]);
                            else
                                outputList.Add(_buf);
                            byteArrayPosition += charCount;
                            //Debug.WriteLine("  Added signed byte");
                            break;
                        case 'B':
                            buf = new byte[charCount];
                            Array.Copy(bytes, byteArrayPosition, buf, 0, charCount);
                            if(charCount == 1)
                                outputList.Add(buf[0]);
                            else
                                outputList.Add(buf);
                            byteArrayPosition += charCount;
                            break;
                        case 'x':
                            byteArrayPosition += charCount;
                            //Debug.WriteLine("  Ignoring a byte");
                            break;
                        default:
                            throw new ArgumentException("You should not be here.");
                    }
                }
                return outputList.ToArray();
            }
            private static int AddElement<T>(byte[] bytes, bool endianFlip, int byteArrayPosition, List<object> outputList, int charCount, int typeSize, Func<byte[], int, T> converter)
            {
                int count = typeSize * charCount;
                var deezBytes = endianFlip ? bytes.Reverse().Skip(byteArrayPosition).Take(count).ToArray() :
                    bytes.Skip(byteArrayPosition).Take(count).ToArray();
                if (charCount > 1)
                {
                    T[] us_arr = new T[charCount];
                    for (int i = 0; i < charCount; i++)
                    {
                        us_arr[i] = converter.Invoke(deezBytes, typeSize * i);
                    }
                    outputList.Add(us_arr);
                }
                else
                {
                    outputList.Add(converter.Invoke(deezBytes, 0));
                }
                return byteArrayPosition + typeSize * charCount;
            }

            public static byte[] Pack(string fmt, params object[] items)
            {
                List<object> item_list = new List<object>();
                string digit = "";
                bool littleEndian = true;
                int n = 0;
                string newfmt = "";
                for (int i = 0; i < fmt.Length; i++)
                {
                    char c = fmt[i];
                    //Debug.WriteLine("  Format character found: {0}", c);
                    int charCount = 1;
                    if (char.IsDigit(c))
                    {
                        digit += c;
                        continue;
                    }
                    else if (digit != "")
                    {
                        charCount = int.Parse(digit);
                        digit = "";
                    }
                    newfmt += new string(c, charCount);
                    switch (c)
                    {
                        case 'c':
                            if (charCount > 1)
                            {
                                var arr = (char[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add((char)items[n++]);
                            break;
                        case 's':
                            if (charCount > 1)
                            {
                                var arr = (string[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]); 
                            }
                            else
                                item_list.Add((string)items[n++]);
                            break;
                        case 'q':
                            if (charCount > 1)
                            {
                                var arr = (long[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToInt64(items[n++]));
                            break;
                        case 'Q':
                            if (charCount > 1)
                            {
                                var arr = (ulong[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToUInt64(items[n++]));
                            break;
                        case 'i':
                            if (charCount > 1)
                            {
                                var arr = (int[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToInt32(items[n++]));
                            break;
                        case 'I':
                            if (charCount > 1)
                            {
                                var arr = (uint[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToUInt32(items[n++]));
                            break;
                        case 'h':
                            if (charCount > 1)
                            {
                                var arr = (short[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToInt16(items[n++]));
                            break;
                        case 'H':
                            if (charCount > 1)
                            {
                                var arr = (ushort[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToUInt16(items[n++]));
                            break;
                        case 'b':
                        case 'B':
                        case 'x':
                            if (charCount > 1)
                            {
                                var arr = (byte[])items[n++];
                                for (int cc = 0; cc < charCount; cc++)
                                    item_list.Add(arr[cc]);
                            }
                            else
                                item_list.Add(Convert.ToByte(items[n++]));
                            break;
                        case '<':
                            littleEndian = true;
                            break;
                        case '>':
                            littleEndian = false;
                            break;
                        default:
                            throw new ArgumentException("Invalid character found in format string.");
                    }
                }

                var res = Pack(item_list.ToArray(), littleEndian, out string neededFormatStringToRecover);
                if (!neededFormatStringToRecover.Equals(newfmt, StringComparison.InvariantCultureIgnoreCase))
                {

                    Console.Error.WriteLine("Wrong pack format");
                }
                return res;
            }
            /// <summary>
            /// Convert an array of objects to a byte array, along with a string that can be used with Unpack.
            /// </summary>
            /// <param name="items">An object array of items to convert</param>
            /// <param name="littleEndian">Set to False if you want to use big endian output.</param>
            /// <param name="neededFormatStringToRecover">Variable to place an 'Unpack'-compatible format string into.</param>
            /// <returns>A Byte array containing the objects provided in binary format.</returns>
            public static byte[] Pack(object[] items, bool littleEndian, out string neededFormatStringToRecover)
            {

                // make a byte list to hold the bytes of output
                var outputBytes = new List<byte>();

                // should we be flipping bits for proper endianness?
                bool endianFlip = (littleEndian != BitConverter.IsLittleEndian);

                // start working on the output string
                string outString = (littleEndian == false ? ">" : "<");

                // convert each item in the objects to the representative bytes
                foreach (object o in items)
                {
                    byte[] theseBytes = TypeAgnosticGetBytes(o);
                    if (endianFlip) theseBytes = theseBytes.Reverse().ToArray();
                    outString += GetFormatSpecifierFor(o);
                    outputBytes.AddRange(theseBytes);
                }

                neededFormatStringToRecover = outString;

                return outputBytes.ToArray();

            }

            public static byte[] Pack(object[] items)
            {
                return Pack(items, true, out _);
            }
        }
    }
}
