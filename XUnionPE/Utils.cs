using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace XUnionPE
{
    class Utils
    {
        public static short FromHexInt16(string hexInt) { return short.Parse(hexInt.Replace("0x", null), NumberStyles.HexNumber); }
        public static ushort FromHexUInt16(string hexInt) { return ushort.Parse(hexInt.Replace("0x", null), NumberStyles.HexNumber); }
        public static int FromHexInt32(string hexInt) { return int.Parse(hexInt.Replace("0x", null), NumberStyles.HexNumber); }
        public static uint FromHexUInt32(string hexInt) { return uint.Parse(hexInt.Replace("0x", null), NumberStyles.HexNumber); }
        public static long FromHexInt64(string hexInt) { return long.Parse(hexInt.Replace("0x", null), NumberStyles.HexNumber); }
        public static ulong FromHexUInt64(string hexInt) { return ulong.Parse(hexInt.Replace("0x", null), NumberStyles.HexNumber); }

        public static uint RightBitShift(uint value, int bits)
        {
            bits &= 31;
            return value >> bits | value << 32 - bits;
        }

        public static uint LeftBitShift(uint value, int bits)
        {
            bits &= 31;
            return value << bits | value >> 32 - bits;
        }

        public static byte[] GetBuffer(byte[] srcData, long keyAdr) { return GetBuffer(srcData, keyAdr, srcData.Length - keyAdr); }
        public static byte[] GetBuffer(byte[] srcData, long keyAdr, long keySize)
        {
            List<byte> result = new List<byte>();
            for (int x = 0; x < keySize; x++) { result.Add(srcData[Convert.ToInt32(keyAdr) + x]); }
            return result.ToArray();
        }
    }
}
