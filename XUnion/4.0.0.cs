using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace XUnion
{
    public class v4
    {
		public class SharedItems                              
		{
            public SharedItems(string Path)
            {
                path = Path;
                RawFile = File.ReadAllBytes(path);
                asm = ModuleDefMD.Load(RawFile);
                app = Assembly.Load(RawFile);
            }

            public bool Stage2Only,
                        isCompressed;

            public string path;
            public byte[] RawFile,
						  Stage2RawFile,
                          OutputRawFile;

            public XORManager XORMng { get; set; }

            public ModuleDefMD asm,
							   Stage2asm;

            public Assembly app,
							Stage2app;

			public MethodDef UnXorMethod_Main,
							 UnXorMethod_IntPart,
							 UnXorMethod_SelectPart,
                             ResourceDecrypt,
                             GZipStage2Method;
		}

        public XUnion.Deobfuscators.v4    Stage1Deobfuscator  { get; set; }
        public XUnion.Deobfuscators.v4	  Stage2Deobfuscator  { get; set; }
        public XUnion.Unpackers.v4		  Unpacker			  { get; set; }
        public XUnion.Unpackers.v4.Stage2 Stage2			  { get; set; }

        public SharedItems Shared;

        public v4(ModuleDefMD asm, bool skipInit = false, bool isStage2ASM = false) { Shared = new SharedItems(asm.Location); if (!skipInit) { if (isStage2ASM) { Shared.Stage2Only = true; InitializeStage2(); } else { InitializeStage1(); } } }
        public v4(string path,     bool skipInit = false, bool isStage2ASM = false) { Shared = new SharedItems(path);         if (!skipInit) { if (isStage2ASM) { Shared.Stage2Only = true; InitializeStage2(); } else { InitializeStage1(); } } }

        public byte[] Unpack()			  { return Shared.OutputRawFile; }
        public void	  Unpack(string path) { File.WriteAllBytes(path, Shared.OutputRawFile); }

        private void IdentifyMethods     (ModuleDefMD asm) { IdentifyMethods(asm, ref Shared); }
        private void InitializeXORManager(ModuleDefMD asm) { Shared.XORMng = InitializeXORManager(asm, ref Shared); }

        private void InitializeStage1() 
        {
            IdentifyMethods(Shared.asm);				// Sets Shared.UnXorMethod_Main, Shared.UnXorMethod_IntPart, Shared.UnXorMethod_SelectPart
            InitializeXORManager(Shared.asm);			// Sets Shared.xor

            Unpacker = new XUnion.Unpackers.v4(ref Shared);

            Stage1Deobfuscator = new XUnion.Deobfuscators.v4(ref Shared, Shared.asm);
            //Stage1Deobfuscator.FixRenaming();

            Unpacker.GetStage2ASM();		            // Sets Shared.Stage2asm, Shared.Stage2app & Shared.Stage2RawFile
            InitializeStage2();
        }
        private void InitializeStage2() 
        {
            if (Shared.Stage2Only)
            {
                Shared.Stage2asm = Shared.asm;
                Shared.Stage2app = Assembly.Load(Shared.RawFile);
                IdentifyMethods(Shared.Stage2asm);
                InitializeXORManager(Shared.Stage2asm);
            }

            Stage2Deobfuscator = new XUnion.Deobfuscators.v4(ref Shared, Shared.Stage2asm);
            //Stage2Deobfuscator.FixRenaming();

            Stage2 = new Unpackers.v4.Stage2(ref Shared);

            Stage2.GetResDecryptMethod();	            // Sets Shared.ResourceDecrypt
            Stage2.IsCompressed();                      // Sets Shared.isCompressed and Shared.GZipStage2Method if found/detected
            Stage2.Fix();					            // Sets Shared.OutputRawFile
        }

        private static ModuleDefMD IdentifyMethods(ModuleDefMD asm, ref SharedItems Shared)         
        {
            ModuleDefMD resultAsm = asm;
            bool XorMethodFound = false;

            for (int x = 0; x < resultAsm.EntryPoint.DeclaringType.Methods.Count; x++)
            {
                if (XorMethodFound) { break; }

                MethodDef methods = resultAsm.EntryPoint.DeclaringType.Methods[x];
                if (!methods.HasBody) { continue; }

                Instruction[] insts = methods.Body.Instructions.ToArray();

                try
                {
                    // Detects if current method is Int32 UnXor Function (int[2] -> int)
                    if (insts[0].IsLdarg() &&
                        insts[1].IsLdarg() &&
                        insts[2].IsLdcI4() &&
                        insts[3].OpCode == OpCodes.Xor &&
                        insts[4].OpCode == OpCodes.Xor &&
                        insts[5].OpCode == OpCodes.Ret)
                    {
                        Shared.UnXorMethod_IntPart = methods; if (Shared.UnXorMethod_Main != null) { XorMethodFound = true; } continue;
                    }

                    // Detects if current method is GZipDecompression
                    if (insts[0].OpCode == OpCodes.Newobj &&
                        insts[3].OpCode == OpCodes.Newobj &&
                        insts[5].OpCode == OpCodes.Newobj &&
                        insts[9].OpCode == OpCodes.Callvirt &&
                        insts[9].Operand is MemberRef &&
                        Utils.CompareNamespaceMemberRef(insts[9], typeof(Stream), "CopyTo", new Type[] { typeof(Stream) }))
                    {
                        methods.Parameters[0].Name = "compressedData";
                        methods.Name = "DecompressProtectedApp";
                        continue;
                    }

                    // Detects if current method is UInt16 UnXor Function (uint16 -> string)
                    if (insts[10].Operand != null &&
                        insts[14].Operand != null &&
                        insts[7].IsLdcI4() &&
                        insts[8].IsLdcI4() &&
                        insts[10].Operand is MethodSpec &&
                        insts[14].Operand is MethodSpec &&
                        Utils.CompareNamespaceMethodSpec(insts[10], typeof(Enumerable), "Skip"))
                    {
                        Shared.UnXorMethod_Main = methods; if (Shared.UnXorMethod_IntPart != null) { XorMethodFound = true; } continue;
                    }
                }
                catch { }
            }

            // Getting the foreach Linq loop (hidden method) present in the UInt16 UnXor method
            for (int x = 0; x < resultAsm.EntryPoint.DeclaringType.NestedTypes.Count; x++)
            {
                TypeDef nestedType = resultAsm.EntryPoint.DeclaringType.NestedTypes[x];
                if (nestedType.IsDelegate || !nestedType.HasFields) { continue; }
                for (int x_ = 0; x_ < nestedType.Methods.Count; x_++)
                {
                    MethodDef nestedMethod = nestedType.Methods[x_];
                    if (!nestedMethod.HasBody || nestedMethod.IsConstructor) { continue; }
                    Instruction[] insts = nestedMethod.Body.Instructions.ToArray();
                    if (insts[3].IsLdcI4() &&
                        insts[4].IsLdcI4() &&
                        insts[5].OpCode == OpCodes.Call &&
                        insts[5].Operand is MethodDef &&
                        insts[6].OpCode == OpCodes.Ldelem_U2 &&
                        insts[7].OpCode == OpCodes.Xor &&
                        insts[8].OpCode == OpCodes.Conv_U2)
                    {
                        Shared.UnXorMethod_SelectPart = nestedMethod; return resultAsm;
                    }
                }
            }
            return null;
        }
        private static XORManager InitializeXORManager(ModuleDefMD asm, ref SharedItems Shared)     
        {
            int[] MainUnXorValues = new int[4]
                {
                    Shared.UnXorMethod_Main.Body.Instructions[7].GetLdcI4Value(),
                    Shared.UnXorMethod_Main.Body.Instructions[8].GetLdcI4Value(),
                    Shared.UnXorMethod_SelectPart.Body.Instructions[3].GetLdcI4Value(),
                    Shared.UnXorMethod_SelectPart.Body.Instructions[4].GetLdcI4Value()
                };

            int IntXorValue = Shared.UnXorMethod_IntPart.Body.Instructions[2].GetLdcI4Value();

            return new XORManager(XORManager.ToInt(MainUnXorValues[0], MainUnXorValues[1], IntXorValue),
                                  XORManager.ToInt(MainUnXorValues[2], MainUnXorValues[3], IntXorValue),
                                  IntXorValue);
        }

        public static bool IsStage2ASM(ModuleDefMD asm) { return asm.EntryPoint.DeclaringType.Fields.Count == 2 ? true : false; }
    }
}