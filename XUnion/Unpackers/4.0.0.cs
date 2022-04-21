using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet.Emit;
using dnlib.DotNet;
using dnlib;
using System.IO;
using System.Reflection;
using System.Resources;
using System.IO.Compression;

namespace XUnion.Unpackers
{
    public class v4
    {
        public class Stage2
        {
            private XUnion.v4.SharedItems SharedItems;

            public Stage2(ref XUnion.v4.SharedItems sharedItems) { SharedItems = sharedItems; }

            public void  GetResDecryptMethod()  
            {
                for (int x = 0; x < SharedItems.Stage2asm.EntryPoint.DeclaringType.Methods.Count; x++)
                {
                    MethodDef methods = SharedItems.Stage2asm.EntryPoint.DeclaringType.Methods[x];
                    if (!methods.HasBody) { continue; }
                    Instruction[] insts = methods.Body.Instructions.ToArray();
                    for (int x_inst = 0; x_inst < insts.Length; x_inst++)
                    {
                        Instruction inst = insts[x_inst];
                        try
                        {
                            if (inst.OpCode == OpCodes.Call &&
                                inst.Operand is MemberRef &&
                                insts[x_inst - 4].OpCode == OpCodes.Call &&
                                insts[x_inst - 8].OpCode == OpCodes.Call &&
                                insts[x_inst - 4].Operand is MethodDef &&
                                insts[x_inst - 8].Operand is MethodDef &&
                                Utils.CompareNamespaceMemberRef(inst, typeof(Buffer), "BlockCopy", new Type[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }))
                            {
                                MethodDef intPart = ((MethodDef)insts[x_inst - 4].Operand);

                                if (intPart.Body.Instructions[0].IsLdarg() &&
                                    intPart.Body.Instructions[1].IsLdarg() &&
                                    intPart.Body.Instructions[2].IsLdcI4() &&
                                    intPart.Body.Instructions[3].OpCode == OpCodes.Xor &&
                                    intPart.Body.Instructions[4].OpCode == OpCodes.Xor &&
                                    intPart.Body.Instructions[5].OpCode == OpCodes.Ret)
                                {
                                    SharedItems.ResourceDecrypt = methods;
                                    return;
                                }
                            }
                        }
                        catch { }
                    }
                }
                throw new Exception("Could not find ResourceDecryption Function!");
            }
            public int[] GetBufferValues()      
            {
                int[] result = new int[4];
                Instruction[] insts = SharedItems.ResourceDecrypt.Body.Instructions.ToArray();

                for (int x_inst = 0; x_inst < insts.Length; x_inst++)
                {
                    Instruction inst = insts[x_inst];
                    if (inst.OpCode == OpCodes.Call &&
                        inst.Operand is MethodDef &&
                        inst.Operand == SharedItems.UnXorMethod_IntPart)
                    {
                        try
                        {
                            if (insts[x_inst - 4].OpCode == OpCodes.Callvirt &&
                            insts[x_inst + 2].OpCode == OpCodes.Callvirt &&
                            insts[x_inst - 4].Operand is MemberRef &&
                            insts[x_inst + 2].Operand is MemberRef &&
                            ((MemberRef)insts[x_inst - 2].Operand).DeclaringType.FullName == typeof(Assembly).FullName + "." + typeof(Assembly).GetMethod("GetManifestResourceName").Name &&
                            ((MemberRef)insts[x_inst - 4].Operand).DeclaringType.FullName == typeof(Assembly).FullName + "." + typeof(Assembly).GetMethod("GetManifestResourceStream", new Type[] { typeof(string) }).Name)
                            {
                                result[0] = SharedItems.XORMng.ToInt(insts[x_inst - 2].GetLdcI4Value(), insts[x_inst - 1].GetLdcI4Value()); // Index of resource name
                            }

                            if (insts[x_inst + 1].OpCode == OpCodes.Sub &&
                                insts[x_inst + 2].OpCode == OpCodes.Newarr &&
                                insts[x_inst + 2].Operand is TypeRef &&
                                ((TypeRef)insts[x_inst + 2].Operand).FullName == typeof(byte).FullName)
                            {
                                result[1] = SharedItems.XORMng.ToInt(insts[x_inst - 2].GetLdcI4Value(), insts[x_inst - 1].GetLdcI4Value()); // Size of output array
                            }

                            if (insts[x_inst - 6].OpCode == OpCodes.Sub &&
                                insts[x_inst - 5].OpCode == OpCodes.Newarr &&
                                insts[x_inst - 5].Operand is TypeRef &&
                                ((TypeRef)insts[x_inst - 5].Operand).FullName == typeof(byte).FullName)
                            {
                                result[2] = SharedItems.XORMng.ToInt(insts[x_inst - 2].GetLdcI4Value(), insts[x_inst - 1].GetLdcI4Value()); // Buffer.BlockCopy startIndex
                            }

                            if (insts[x_inst - 10].OpCode == OpCodes.Sub &&
                                insts[x_inst - 9].OpCode == OpCodes.Newarr &&
                                insts[x_inst - 9].Operand is TypeRef &&
                                ((TypeRef)insts[x_inst - 5].Operand).FullName == typeof(byte).FullName)
                            {
                                result[3] = SharedItems.XORMng.ToInt(insts[x_inst - 2].GetLdcI4Value(), insts[x_inst - 1].GetLdcI4Value()); // Buffer.BlockCopy count
                            }
                        }
                        catch { }
                    }
                }

                return result;
            }
            public void  IsCompressed()         
            {
                for (int x_methods = 0; x_methods < SharedItems.Stage2asm.EntryPoint.DeclaringType.Methods.Count; x_methods++)
                {
                    MethodDef methods = SharedItems.Stage2asm.EntryPoint.DeclaringType.Methods[x_methods];
                    if (!methods.HasBody) { continue; }
                    Instruction[] insts = methods.Body.Instructions.ToArray();
                    try
                    {
                        if (insts[0].OpCode == OpCodes.Newobj &&
                        insts[3].OpCode == OpCodes.Newobj &&
                        insts[5].OpCode == OpCodes.Newobj &&
                        insts[9].OpCode == OpCodes.Callvirt &&
                        insts[9].Operand is MemberRef &&
                        Utils.CompareNamespaceMemberRef(insts[9], typeof(Stream), "CopyTo", new Type[] { typeof(Stream) }))
                        {
                            SharedItems.isCompressed = true;
                            SharedItems.GZipStage2Method = methods;
                            return;
                        }
                    } catch { }
                }
            }

            public void  Fix()                  
            {
                MSILArray resNameArr = null;

                Instruction[] insts = SharedItems.Stage2asm.EntryPoint.Body.Instructions.ToArray();
                for (int x = 0; x < insts.Length; x++)
                {
                    Instruction inst = insts[x];

                    if (inst.OpCode == OpCodes.Newarr) { int.Parse("0"); }

                    if (inst.OpCode == OpCodes.Newarr &&
                        inst.Operand is TypeRef &&
                        ((TypeRef)inst.Operand).FullName == typeof(ushort).FullName &&
                        insts[x - 5].OpCode == OpCodes.Call &&
                        insts[x - 5].Operand is MethodDef &&
                        insts[x - 1].IsLdcI4())
                    {
                        if (((MethodDef)insts[x - 5].Operand).Body.Instructions[3].OpCode == OpCodes.Xor &&
                            ((MethodDef)insts[x - 5].Operand).Body.Instructions[4].OpCode == OpCodes.Xor &&
                            ((MethodDef)insts[x - 5].Operand).Body.Instructions[5].OpCode == OpCodes.Ret)
                        {
                            //SharedItems.UnXorMethod_IntPart = (MethodDef)insts[x - 5].Operand;
                            resNameArr = MSILArray.ParseArray(insts, x - 1);
                        }
                    }
                }

                string resourceName = SharedItems.XORMng.Compute(Utils.ToUInt16(resNameArr.ToArray()));

                int[] bufVal = GetBufferValues();
                using (ResourceReader rr = new ResourceReader(SharedItems.Stage2app.GetManifestResourceStream(
                                                              SharedItems.Stage2app.GetManifestResourceNames()[bufVal[0]])))
                {
                    byte[] srcApp;
                    string resType;

                    rr.GetResourceData(resourceName, out resType, out srcApp);

                    if (SharedItems.isCompressed)
                    {
                        byte[] decompSrc = new byte[srcApp.Length];
                        bool offsetHeader = (srcApp[0] != 0x1F) && (srcApp[1] != 0x8B);
                        Buffer.BlockCopy(srcApp, offsetHeader ? 4 : 0, decompSrc, 0, srcApp.Length - 4);
                        MemoryStream tempMs = new MemoryStream();
                        GZipStream decomp = new GZipStream(new MemoryStream(decompSrc), CompressionMode.Decompress);
                        decomp.CopyTo(tempMs);
                        srcApp = tempMs.ToArray();
                        SharedItems.OutputRawFile = srcApp;
                    }
                    else
                    {
                        if (srcApp[0] != 0x4d &&       // PEunion sometimes just adds 4 junk bytes to the source file, causing the packed app to not
                            srcApp[1] != 0x5a)         // be usable, this fixes it.
                        {
                            SharedItems.OutputRawFile = new byte[(srcApp.Length - bufVal[1]) - 4];
                            Buffer.BlockCopy(srcApp, bufVal[2] + 4, SharedItems.OutputRawFile, 0, SharedItems.OutputRawFile.Length - 4);
                        }
                        else
                        {
                            SharedItems.OutputRawFile = new byte[srcApp.Length - bufVal[1]];
                            Buffer.BlockCopy(srcApp, bufVal[2], SharedItems.OutputRawFile, 0, SharedItems.OutputRawFile.Length);
                        }
                    }
                }
            }
        }

        private XUnion.v4.SharedItems SharedItems;

        public v4(ref XUnion.v4.SharedItems sharedItems) { SharedItems = sharedItems; }

        public long[] GetDecryptionValues()
        {
            long[] result = new long[6];

            Instruction[] insts = SharedItems.asm.EntryPoint.Body.Instructions.ToArray();
            for (int x_inst = 0; x_inst < insts.Length; x_inst++)
            {
                Instruction inst = insts[x_inst];

                try
                {
                    if (inst.OpCode == OpCodes.Call &&
                        inst.Operand is MethodDef &&
                        insts[x_inst + 2].OpCode == OpCodes.Call &&
                        insts[x_inst + 2].Operand is MemberRef &&
                        (MethodDef)inst.Operand == SharedItems.UnXorMethod_IntPart &&
                        ((MemberRef)insts[x_inst + 2].Operand).DeclaringType.FullName == typeof(Assembly).FullName)
                    {
                        result[0] = insts[x_inst - 6].GetLdcI4Value();                      // Bitshift
                        result[1] = insts[x_inst - 4].GetLdcI4Value();                      // Bitshift
                        result[4] = SharedItems.XORMng.ToInt(insts[x_inst - 2].GetLdcI4Value(),
                                              insts[x_inst - 1].GetLdcI4Value()); continue; // Offsets bitshift
                    }

                    if (inst.OpCode == OpCodes.Call &&
                        insts[x_inst - 4].OpCode == OpCodes.Newarr &&
                        insts[x_inst + 4].OpCode == OpCodes.Call &&
                        inst.Operand is MethodDef &&
                        insts[x_inst + 4].Operand is MethodDef &&
                        insts[x_inst - 4].Operand is TypeRef &&
                        (MethodDef)inst.Operand == SharedItems.UnXorMethod_IntPart &&
                        (MethodDef)insts[x_inst + 4].Operand == SharedItems.UnXorMethod_IntPart &&
                        ((ITypeDefOrRef)insts[x_inst - 4].Operand).FullName == typeof(byte).FullName)
                    {
                        result[2] = SharedItems.XORMng.ToInt(insts[x_inst - 2].GetLdcI4Value(),
                                              insts[x_inst - 1].GetLdcI4Value());
                        result[3] = SharedItems.XORMng.ToInt(insts[x_inst + 2].GetLdcI4Value(),
                                              insts[x_inst + 3].GetLdcI4Value());
                        result[5] = insts[x_inst - 7].GetLdcI4Value();                      // Packed Assembly Size
                    }
                }
                catch { }
            }
            return result;
        }
        public void   GetStage2ASM()       
        {
            Instruction[] insts = SharedItems.asm.EntryPoint.Body.Instructions.ToArray();

            MSILTryCatch hdex = MSILTryCatch.Parse(insts, SharedItems.asm.EntryPoint.Body.ExceptionHandlers[2]);

            int resNamArrStartIndex = -1;

            for (int x = 0; x < hdex.TryInstructions.Length; x++)
            {
                Instruction inst = hdex.TryInstructions[x];
                try
                {
                    if (inst.IsStloc() &&
                    hdex.TryInstructions[x - 1].OpCode == OpCodes.Newarr &&
                    hdex.TryInstructions[x - 1].Operand is TypeRef &&
                    ((TypeRef)hdex.TryInstructions[x - 1].Operand).FullName == typeof(ushort).FullName &&
                    hdex.TryInstructions[x - 5].IsLdloc()) { resNamArrStartIndex = x - 2; break; }
                }
                catch { }
            }

            ushort[] nameArr = Utils.ToUInt16(MSILArray.ParseArray(hdex.TryInstructions).ToArray()),
                  resourceNameArr = Utils.ToUInt16(MSILArray.ParseArray(hdex.TryInstructions, resNamArrStartIndex).ToArray());

            string name = SharedItems.XORMng.Compute(nameArr),
                   resourceName = SharedItems.XORMng.Compute(resourceNameArr);

            long[] dVal = GetDecryptionValues();

            MemoryStream ms = new MemoryStream();
            SharedItems.app.GetManifestResourceStream(name).CopyTo(ms);

            using (ResourceReader rr = new ResourceReader(SharedItems.app.GetManifestResourceStream(name)))
            {
                byte[] encryptedApp;
                byte[] decryptedApp = new byte[dVal[5]];

                string resType;

                rr.GetResourceData(resourceName, out resType, out encryptedApp);

                for (long x = dVal[2]; x < dVal[5]; x++)
                {
                    decryptedApp[x] = (byte)((uint)encryptedApp[dVal[3]++] ^ (uint)dVal[0]);
                    if ((dVal[1] & 1U) == 1U) { dVal[3] += dVal[4]; }
                    dVal[0] = ((uint)dVal[0] >> 5 | (uint)dVal[0] << 27) * 7U;
                    dVal[1] = ((uint)dVal[1] >> 1 | (uint)dVal[1] << 31);
                }

                SharedItems.Stage2RawFile = decryptedApp;
                SharedItems.Stage2app = Assembly.Load(decryptedApp);
                SharedItems.Stage2asm = ModuleDefMD.Load(decryptedApp);
            }
        }
    }
}
