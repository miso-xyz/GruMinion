using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone;
using Gee.External.Capstone.X86;

namespace XUnionPE
{
    class Decompiler
    {
        public Decompiler(string appPath)
        {
            AddressBook = new Dictionary<string, uint>();

            path = Path.GetFullPath(appPath);
            RawApp = File.ReadAllBytes(path);

            Headers = new PEUtils.Headers(RawApp);
            PEUtils.Headers.SectionHeader mainSection = Headers.GetSectionHeader(Headers.OptionalHdr.EntrypointAdr, true);

            byte[] RawInsts = new byte[mainSection.VirtSize + mainSection.VirtAdr];
            Array.Copy(RawApp, mainSection.RawAdr, RawInsts, 0, mainSection.RawSize);

            disasm = CapstoneDisassembler.CreateX86Disassembler(GetBitness());
            disasm.EnableInstructionDetails = true;
            disasm.DisassembleSyntax = DisassembleSyntax.Intel;

            //X86Instruction[] insts = disasm.Disassemble(RawInsts);
        }

        public enum MachineType : ushort { Native = 0, x86 = 0x014c, Itanium = 0x0200, x64 = 0x8664 }

        private string path;
        private byte[] RawApp, outputRaw;
        private CapstoneX86Disassembler disasm;

        public PEUtils.Headers Headers;
        public Dictionary<string, uint> AddressBook { get; set; }

        public X86DisassembleMode GetBitness()
        {
            switch (BitConverter.ToUInt16(RawApp, 0x4A))
            {
                case 0x6486: return X86DisassembleMode.Bit64;
                case 0x4c01: return X86DisassembleMode.Bit32;
                default: throw new Exception("Unsupporteed Bitness (must be x86_64)");
            }
        }

        public uint Stage2Entrypoint = uint.MaxValue,
                    Stage1Entrypoint = uint.MaxValue,
                    Stage2Size = uint.MaxValue,
                    Stage2Key = uint.MaxValue,
                    Stage2PaddingMask = uint.MaxValue,
                    PaddingCount = uint.MaxValue;

        public void Unpack()
        {
            Console.WriteLine(" Checking if EXE is packed with PEUnion...");
            Console.WriteLine();
            if (!isPEUnionPacked())
            {
                Console.Write(" Invalid Executable! (Not packed using PEUnion)");
                Console.ReadKey();
                return;
            }
            Console.WriteLine(" PEUnion detected!");
            Console.WriteLine();
            X86Instruction[] insts = disasm.Disassemble(Utils.GetBuffer(RawApp, AddressBook["mainFunction"]));
            for (int x = 8; x < insts.Length; x++)
            {
                X86Instruction inst = insts[x];
                switch (inst.Id)
                {
                    case X86InstructionId.X86_INS_IMUL:
                        if (inst.Details.Operands[0].Type == X86OperandType.Register &&
                            inst.Details.Operands[0].Register.Id == X86RegisterId.X86_REG_EDX &&
                            inst.Details.Operands[1].Type == X86OperandType.Register &&
                            inst.Details.Operands[1].Register.Id == X86RegisterId.X86_REG_EDX &&
                            inst.Details.Operands[2].Type == X86OperandType.Immediate &&
                            inst.Details.Operands[2].Immediate == 7)
                        {
                            for (int y = x; y < insts.Length; y++)
                            {
                                if (PaddingCount != uint.MaxValue) { break; }
                                X86Instruction paddingCount = insts[y];
                                switch (paddingCount.Id)
                                {
                                    case X86InstructionId.X86_INS_TEST:
                                        if (paddingCount.Details.Operands[0].Type == X86OperandType.Register &&
                                            paddingCount.Details.Operands[0].Register.Id == X86RegisterId.X86_REG_EBX &&
                                            paddingCount.Details.Operands[1].Type == X86OperandType.Immediate)
                                        { PaddingCount = Convert.ToUInt32(paddingCount.Details.Operands[1].Immediate); }
                                        break;
                                }
                            }
                        }
                        break;
                    case X86InstructionId.X86_INS_CALL:
                        if (inst.Details.Operands[0].Type == X86OperandType.Memory &&
                            inst.Details.Operands[0].Memory.Base != null &&
                            inst.Details.Operands[0].Memory.Base.Id == X86RegisterId.X86_REG_EBP &&
                            inst.Operand.EndsWith("0xc]") &&
                            Utils.FromHexUInt32(insts[x - 1].Operand) == Headers.OptionalHdr.EntrypointAdr + Headers.OptionalHdr.ImageBase)
                        {
                            for (int y = x; y < insts.Length; y++)
                            {
                                if (Stage1Entrypoint != uint.MaxValue &&
                                    Stage2Entrypoint != uint.MaxValue &&
                                    Stage2Size != uint.MaxValue &&
                                    Stage2Key != uint.MaxValue &&
                                    Stage2PaddingMask != uint.MaxValue)
                                { break; }
                                inst = insts[y];
                                switch (inst.Id)
                                {
                                    case X86InstructionId.X86_INS_MOV:
                                        if (inst.Details.Operands.Length == 2 &&
                                            inst.Details.Operands[0].Type == X86OperandType.Register &&
                                            inst.Details.Operands[1].Type == X86OperandType.Immediate)
                                        {
                                            switch (inst.Details.Operands[0].Register.Id)
                                            {
                                                case X86RegisterId.X86_REG_EDI: Stage1Entrypoint = Headers.GetRawOffset((uint)inst.Details.Operands[1].Immediate, Headers.OptionalHdr.ImageBase); break;
                                                case X86RegisterId.X86_REG_ESI: Stage2Entrypoint = Headers.GetRawOffset((uint)inst.Details.Operands[1].Immediate, Headers.OptionalHdr.ImageBase); break;
                                                case X86RegisterId.X86_REG_ECX: Stage2Size = Convert.ToUInt32(inst.Details.Operands[1].Immediate); break;
                                                case X86RegisterId.X86_REG_EDX: Stage2Key = Convert.ToUInt32(inst.Details.Operands[1].Immediate); /*Utils.RightBitShift((int)inst.Details.Operands[1].Immediate, 5) * 7;*/ break;
                                                case X86RegisterId.X86_REG_EBX: Stage2PaddingMask = Convert.ToUInt32(inst.Details.Operands[1].Immediate); break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                }
            }
            List<byte> stage2Raw = new List<byte>();
            Console.WriteLine(" unXORing application...");
            using (BinaryReader br = new BinaryReader(new MemoryStream(RawApp)))
            {
                br.BaseStream.Position = Stage2Entrypoint;
                int curByte = -1;
                while ((curByte = (int)br.ReadByte()) != -1)
                {
                    if (stage2Raw.Count == Stage2Size) { break; }
                    stage2Raw.Add((byte)(curByte ^ BitConverter.GetBytes(Stage2Key)[0]));
                    Stage2Key = Utils.RightBitShift(Stage2Key, 5) * 7;
                    if ((Stage2PaddingMask & 1) == 1) { for (int y = 0; y < PaddingCount; y++) { curByte = br.ReadByte(); } }
                    Stage2PaddingMask = Utils.RightBitShift(Stage2PaddingMask, 1);
                }
            }
            outputRaw = new byte[stage2Raw.Count];
            Array.Copy(stage2Raw.ToArray(), 0x700, outputRaw, 0, stage2Raw.Count - 0x700); // removes junk
            File.WriteAllBytes("output_pe32_xunion.exe", outputRaw.ToArray());
            Console.WriteLine(" Extracted application saved as 'output_pe32_xunion.exe'!");
            Console.ReadKey();
        }

        public bool isPEUnionPacked()
        {
            X86Instruction[] insts = disasm.Disassemble(Utils.GetBuffer(RawApp, Headers.GetRawOffset(Headers.OptionalHdr.EntrypointAdr)));
            if (insts[0].Id == X86InstructionId.X86_INS_CALL &&
                insts[1].Id == X86InstructionId.X86_INS_PUSH &&
                insts[2].Id == X86InstructionId.X86_INS_CALL &&
                insts[3].Id == X86InstructionId.X86_INS_RET)
            {
                PEUtils.Headers.ImportDll kernel32ExitPrc = Headers.GetImport(Headers.GetRawOffset((uint)PEUtils.InstructionUtils.GetCallAdr(insts[2], Headers.OptionalHdr.ImageBase)));
                if (kernel32ExitPrc != null &&
                    kernel32ExitPrc.Name == "kernel32.dll" &&
                    kernel32ExitPrc.Functions[0].Name == "ExitProcess")
                {
                    AddressBook.Add("mainFunction", Headers.GetRawOffset(Utils.FromHexUInt32(insts[0].Operand)));
                    return true;
                }
            }
            return false;
        }
    }
}