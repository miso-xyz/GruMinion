using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XUnion
{
    public class XORManager
    {
        public XORManager(int skipVal, int arrVal, int intXor)
        {
            SkipValue = skipVal;
            ArrayValue = arrVal;
            IntXorValue = intXor;
        }

        public static int ToInt(int val1, int val2, int xorValue) { return val1 ^ (val2 ^ xorValue); }
        public int ToInt(int val1, int val2) { return ToInt(val1, val2, IntXorValue); }

        public string Compute(ushort[] src)
        {
            string result = "";
            foreach (ushort b in src.Skip(SkipValue)) { result += (char)(b ^ src[ArrayValue]); }
            return result;
        }

        public int SkipValue    { get; set; }
        public int ArrayValue   { get; set; }
        public int IntXorValue  { get; set; }
    }
}
