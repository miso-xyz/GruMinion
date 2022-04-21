using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace XUnion
{
    class Utils
    {
        public static ushort[] ToUInt16(object[] src)
        {
            List<ushort> result = new List<ushort>();
            for (int x = 0; x < src.Length; x++) { result.Add(ushort.Parse(src[x].ToString())); }
            return result.ToArray();
        }

        public static bool CompareNamespaceMemberRef (Instruction srcInst, Type matchType, string methodName, Type[] methodArgs = null)     
        {
            string instNamespace        = ((MemberRef)srcInst.Operand).DeclaringType.FullName + "." + ((MemberRef)srcInst.Operand).Name,
                   matchClassNamespace  = matchType.FullName,
                   matchMethodName      = "";
            if (methodArgs == null) { matchMethodName = matchType.GetMethod(methodName).Name; }
            else                    { matchMethodName = matchType.GetMethod(methodName, methodArgs).Name; }
            return instNamespace == matchClassNamespace + "." + matchMethodName;
        }
        public static bool CompareNamespaceMethodSpec(Instruction srcInst, Type matchType, string methodName, Type[] methodArgs = null)     
        {
            string instNamespace        = ((MethodSpec)srcInst.Operand).DeclaringType.FullName + "." + ((MethodSpec)srcInst.Operand).Name,
                   matchClassNamespace  = matchType.FullName,
                   matchMethodName      = "";
            if (methodArgs == null) { matchMethodName = matchType.GetMethod(methodName).Name; }
            else                    { matchMethodName = matchType.GetMethod(methodName, methodArgs).Name; }
            return instNamespace == matchClassNamespace + "." + matchMethodName;
        }
    }
}