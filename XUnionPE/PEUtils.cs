using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gee.External.Capstone.X86;

namespace XUnionPE
{
    class PEUtils
    {
        public class Headers
        {
            public Headers(byte[] data)
            {
                DOSHdr = new DOSHeader();
                FileHdr = new FileHeader();
                OptionalHdr = new OptionalHeader();

                BinaryReader br = new BinaryReader(new MemoryStream(data));

                #region DOS Header
                DOSHdr.Magic = new string(br.ReadChars(2));
                DOSHdr.LastPageByteCount = br.ReadUInt16();
                DOSHdr.PageCount = br.ReadUInt16();
                DOSHdr.RelocationCount = br.ReadUInt16();
                DOSHdr.SizeOfHeaderParagraph = br.ReadUInt16();
                DOSHdr.MinExtraParagraph = br.ReadUInt16();
                DOSHdr.MaxExtraParagraph = br.ReadUInt16();
                DOSHdr.SSValue = br.ReadUInt16();
                DOSHdr.SPValue = br.ReadUInt16();
                DOSHdr.SPValue = br.ReadUInt16();
                DOSHdr.Checksum = br.ReadUInt16();
                DOSHdr.IPValue = br.ReadUInt16();
                DOSHdr.CSValue = br.ReadUInt16();
                DOSHdr.RelocationTableAdr = br.ReadUInt16();
                DOSHdr.OverlayNum = br.ReadUInt16();

                ushort[] reservedArr = new ushort[4];
                for (int x = 0; x < 4; x++) { reservedArr[x] = br.ReadUInt16(); }
                DOSHdr.ReservedWords1 = reservedArr;

                DOSHdr.OEMID = br.ReadUInt16();
                DOSHdr.OEMInfo = br.ReadUInt16();

                reservedArr = new ushort[10];
                for (int x = 0; x < 10; x++) { reservedArr[x] = br.ReadUInt16(); }
                DOSHdr.ReservedWords2 = reservedArr;

                DOSHdr.NewExeHeaderAdr = br.ReadUInt32();
                #endregion
                #region File Header
                // 0x40 -> 0x7F is DOS Stub, 0x80 is Start of File Header
                br.BaseStream.Position = 0x80;

                br.ReadUInt32(); // Skips Magic Signature
                FileHdr.Machine = br.ReadUInt16();
                FileHdr.SectionCount = br.ReadUInt16();
                FileHdr.Timestamp = new DateTime(br.ReadUInt32());
                FileHdr.SymbolTableAdr = br.ReadUInt32();
                FileHdr.SymbolCount = br.ReadUInt32();
                FileHdr.OptionalHeaderSize = br.ReadUInt16();
                FileHdr.Characteristics = br.ReadUInt16();
                #endregion
                #region Optional Header
                OptionalHdr.Magic = br.ReadUInt16();
                OptionalHdr.MajorLinkerVersion = br.ReadByte();
                OptionalHdr.MinorLinkerVersion = br.ReadByte();
                OptionalHdr.SizeOfCode = br.ReadUInt32();
                OptionalHdr.SizeOfInitializedData = br.ReadUInt32();
                OptionalHdr.SizeOfUnInitializedData = br.ReadUInt32();
                OptionalHdr.EntrypointAdr = br.ReadUInt32();
                OptionalHdr.BaseOfCode = br.ReadUInt32();
                OptionalHdr.BaseOfData = br.ReadUInt32();
                OptionalHdr.ImageBase = br.ReadUInt32();
                OptionalHdr.SectionAlign = br.ReadUInt32();
                OptionalHdr.FileAlign = br.ReadUInt32();
                OptionalHdr.MajorOSVersion = br.ReadUInt16();
                OptionalHdr.MinorOSVersion = br.ReadUInt16();
                OptionalHdr.MajorImageVersion = br.ReadUInt16();
                OptionalHdr.MinorImageVersion = br.ReadUInt16();
                OptionalHdr.MajorSubsystemVersion = br.ReadUInt16();
                OptionalHdr.MinorSubsystemVersion = br.ReadUInt16();
                OptionalHdr.Win32Version = br.ReadUInt32();
                OptionalHdr.SizeOfImage = br.ReadUInt32();
                OptionalHdr.SizeOfHeaders = br.ReadUInt32();
                OptionalHdr.Checksum = br.ReadUInt32();
                OptionalHdr.Subsystem = br.ReadUInt16();
                OptionalHdr.DLLCharacteristics = br.ReadUInt16();
                OptionalHdr.SizeOfStackReserve = br.ReadUInt32();
                OptionalHdr.SizeOfStackCommit = br.ReadUInt32();
                OptionalHdr.SizeOfHeapReverse = br.ReadUInt32();
                OptionalHdr.SizeOfHeapCommit = br.ReadUInt32();
                OptionalHdr.LoaderFlags = br.ReadUInt32();
                OptionalHdr.RVAAndSizeCount = br.ReadUInt32();
                #region Data Directories
                OptionalHdr.DataDirs.ExportDir = br.ReadUInt64();
                OptionalHdr.DataDirs.ImportDir = br.ReadUInt64();
                OptionalHdr.DataDirs.ResourceDir = br.ReadUInt64();
                OptionalHdr.DataDirs.ExceptionDir = br.ReadUInt64();
                OptionalHdr.DataDirs.SecurityDir = br.ReadUInt64();
                OptionalHdr.DataDirs.BaseRelocationTable = br.ReadUInt64();
                OptionalHdr.DataDirs.DebugDir = br.ReadUInt64();
                OptionalHdr.DataDirs.ArchitectureSpecificData = br.ReadUInt64();
                OptionalHdr.DataDirs.GlobalPtrRVA = br.ReadUInt64();
                OptionalHdr.DataDirs.TLSDir = br.ReadUInt64();
                OptionalHdr.DataDirs.LoadConfigDir = br.ReadUInt64();
                OptionalHdr.DataDirs.HeadersImportDirBound = br.ReadUInt64();
                OptionalHdr.DataDirs.ImportAdrTable = br.ReadUInt64();
                OptionalHdr.DataDirs.DelayLoadImportDescriptors = br.ReadUInt64();
                OptionalHdr.DataDirs.DotNetHeader = br.ReadUInt64();
                #endregion
                #endregion
                #region Section Headers
                br.ReadUInt64(); // 8 byte gap
                List<SectionHeader> SectionHdrsList = new List<SectionHeader>();
                for (int x = 0; x < FileHdr.SectionCount; x++)
                {
                    SectionHeader sectionHdr = new SectionHeader();

                    sectionHdr.Name = Encoding.Default.GetString(
                                                 br.ReadBytes(sizeof(ulong)));
                    sectionHdr.VirtSize = br.ReadUInt32();
                    sectionHdr.VirtAdr = br.ReadUInt32();
                    sectionHdr.RawSize = br.ReadUInt32();
                    sectionHdr.RawAdr = br.ReadUInt32();
                    sectionHdr.PtrToReloc = br.ReadUInt32();
                    br.ReadUInt32();           // 4 byte gap
                    sectionHdr.RelocCount = br.ReadUInt16();
                    sectionHdr.LinenumCount = br.ReadUInt16();
                    sectionHdr.Characteristics = br.ReadUInt32();

                    SectionHdrsList.Add(sectionHdr);
                }
                SectionHdrs = SectionHdrsList.ToArray();
                #endregion
                #region Imports
                uint idataStart = 0xFFFFFFFF;
                foreach (SectionHeader sectionHeader in SectionHdrs) { if (sectionHeader.Name.StartsWith(".idata")) { idataStart = sectionHeader.RawAdr; break; } }

                br.BaseStream.Position = idataStart;
                List<ImportDll> importsList = new List<ImportDll>();
                for (byte[] x = br.ReadBytes(20);
                            x[0] != 0x00;
                            x = br.ReadBytes(20))
                {
                    ImportDll impDll = new ImportDll();
                    impDll.OriginalFirstThunk = GetRawOffset(BitConverter.ToUInt32(x, 0));
                    impDll.TimedateStamp = BitConverter.ToUInt32(x, 4);
                    impDll.Forwarder = BitConverter.ToUInt32(x, 8);
                    impDll.NameRVA = GetRawOffset(BitConverter.ToUInt32(x, 12));
                    impDll.FirstThunk = GetRawOffset(BitConverter.ToUInt32(x, 16));

                    using (BinaryReader callViaBr = new BinaryReader(new MemoryStream(data)))
                    {
                        callViaBr.BaseStream.Position = impDll.FirstThunk;

                        List<ImportDll.Function> impFuncList = new List<ImportDll.Function>();
                        while (true)
                        {
                            ImportDll.Function impFunc = new ImportDll.Function();
                            impFunc.CallViaAdr = (uint)callViaBr.BaseStream.Position;
                            uint callviaAdr = callViaBr.ReadUInt32();
                            if (callviaAdr == 0) { break; }
                            impFunc.OriginalThunk = impFunc.Thunk = GetRawOffset(callviaAdr);
                            impFuncList.Add(impFunc);
                        }
                        impDll.Functions = impFuncList.ToArray();
                    }
                    importsList.Add(impDll);
                }

                string[] dllNames = null;
                for (int x_dll = 0; x_dll < importsList.Count; x_dll++)
                {
                    ImportDll impDll = importsList[x_dll];
                    string[] functionNames = null;

                    int[] nullSkip = null;

                    if (dllNames == null) { dllNames = GetStrings(br, importsList.Count); }
                    if (impDll.Name == null) { impDll.Name = dllNames[x_dll]; }

                    using (BinaryReader impFuncBr = new BinaryReader(new MemoryStream(data)))
                    {
                        for (int x = 0; x < impDll.Functions.Length; x++)
                        {
                            ImportDll.Function impFunc = impDll.Functions[x];

                            impFuncBr.BaseStream.Position = impFunc.CallViaAdr;
                            uint ThunkAdr = GetRawOffset(impFuncBr.ReadUInt32());
                            impFuncBr.BaseStream.Position = ThunkAdr;
                            if (functionNames == null) { functionNames = GetStrings(impFuncBr, impDll.Functions.Length, out nullSkip); impFuncBr.BaseStream.Position = ThunkAdr; }

                            impFunc.Hint = impFuncBr.ReadUInt16();
                            impFunc.Name = functionNames[x];

                            impFuncBr.BaseStream.Position += functionNames[x].Length + nullSkip[x];
                        }
                    }
                }

                Imports = importsList.ToArray();
                #endregion
            }

            public class DOSHeader
            {
                public string Magic;

                public ushort LastPageByteCount,
                              PageCount,
                              RelocationCount,
                              SizeOfHeaderParagraph,
                              MinExtraParagraph,
                              MaxExtraParagraph,
                              SSValue,
                              SPValue,
                              Checksum,
                              IPValue,
                              CSValue,
                              RelocationTableAdr,
                              OverlayNum,
                              OEMID,
                              OEMInfo;

                public uint NewExeHeaderAdr;

                public ushort[] ReservedWords1 = new ushort[4],
                                ReservedWords2 = new ushort[10];
            }
            public class FileHeader
            {
                public ushort Machine,
                              SectionCount,
                              OptionalHeaderSize,
                              Characteristics;

                public DateTime Timestamp;

                public uint SymbolTableAdr,
                            SymbolCount;
            }
            public class OptionalHeader
            {
                public OptionalHeader() { DataDirs = new DataDirectory(); }

                public class DataDirectory
                {
                    public ulong ExportDir,
                                 ImportDir,
                                 ResourceDir,
                                 ExceptionDir,
                                 SecurityDir,
                                 BaseRelocationTable,
                                 DebugDir,
                                 ArchitectureSpecificData,
                                 GlobalPtrRVA,
                                 TLSDir,
                                 LoadConfigDir,
                                 HeadersImportDirBound,
                                 ImportAdrTable,
                                 DelayLoadImportDescriptors,
                                 DotNetHeader;
                }

                public ushort Magic;

                public byte MajorLinkerVersion,
                            MinorLinkerVersion;

                public ushort MajorOSVersion,
                              MinorOSVersion,
                              MajorImageVersion,
                              MinorImageVersion,
                              MajorSubsystemVersion,
                              MinorSubsystemVersion,
                              Subsystem,
                              DLLCharacteristics;

                public uint SizeOfCode,
                            SizeOfInitializedData,
                            SizeOfUnInitializedData,
                            EntrypointAdr,
                            BaseOfCode,
                            BaseOfData,
                            ImageBase,
                            SectionAlign,
                            FileAlign,
                            Win32Version,
                            SizeOfImage,
                            SizeOfHeaders,
                            Checksum,
                            SizeOfStackReserve,
                            SizeOfStackCommit,
                            SizeOfHeapReverse,
                            SizeOfHeapCommit,
                            LoaderFlags,
                            RVAAndSizeCount;

                public DataDirectory DataDirs { get; set; }
            }
            public class SectionHeader
            {
                public string Name;

                public ushort RelocCount,
                              LinenumCount;

                public uint VirtSize,
                            VirtAdr,
                            RawSize,
                            RawAdr,
                            PtrToReloc,
                            Characteristics;
            }
            public class ImportDll
            {
                public class Function
                {
                    public string Name;

                    public uint OriginalThunk, Thunk, CallViaAdr;

                    public ushort Hint;
                }

                public string Name;

                public uint OriginalFirstThunk,
                            TimedateStamp,
                            Forwarder,
                            NameRVA,
                            FirstThunk;

                public bool IsBound { get { return TimedateStamp == 0xFFFFFFFF; } }

                public Function[] Functions { get; set; }
            }

            public DOSHeader DOSHdr { get; set; }
            public FileHeader FileHdr { get; set; }
            public OptionalHeader OptionalHdr { get; set; }
            public SectionHeader[] SectionHdrs { get; set; }
            public ImportDll[] Imports { get; set; }

            private string[] GetStrings(BinaryReader br, int expectedCount)
            {
                int[] nullSkip = null;
                return GetStrings(br, expectedCount, out nullSkip);
            }
            private string[] GetStrings(BinaryReader br, int expectedCount, out int[] nullSkip)
            {
                string[] result = new string[expectedCount];
                nullSkip = new int[expectedCount]; 
                string tempString = "";
                int arrPos = 0;
                for (byte x = br.ReadByte(); arrPos != expectedCount; x = br.ReadByte())
                {
                    if (arrPos == expectedCount) { break; }
                    if (x == 0x00)
                    {
                        string filter = tempString.Replace("\0", null);
                        if (filter == "") { continue; }
                        nullSkip[arrPos] = tempString.Split("\0".ToCharArray()).Length;
                        result[arrPos] = filter;
                        tempString = "";
                        arrPos++;
                    }
                    tempString += (char)x;
                }
                return result.ToArray();
            }

            public SectionHeader GetSectionHeader(uint adr, bool rawCompare = false)
            {
                foreach (SectionHeader sectionHdr in SectionHdrs)
                {
                    if (rawCompare)
                    {
                        if (sectionHdr.RawAdr <= adr &&
                        sectionHdr.RawAdr + sectionHdr.RawSize > adr)
                        {
                            return sectionHdr;
                        }
                    }
                    else
                    {
                        if (sectionHdr.VirtAdr <= adr &&
                        sectionHdr.VirtAdr + sectionHdr.VirtSize > adr)
                        {
                            return sectionHdr;
                        }
                    }
                }
                return null;
            }
            public ImportDll GetImport(uint RAWadr)
            {
                ImportDll result = new ImportDll();
                foreach (ImportDll impDll in Imports)
                {
                    foreach (ImportDll.Function impFunc in impDll.Functions)
                    {
                        if (impFunc.CallViaAdr == RAWadr)
                        {
                            result = impDll;
                            List<ImportDll.Function> impFuncs = result.Functions.ToList();
                            impFuncs.Clear();
                            impFuncs.Add(impFunc);
                            result.Functions = impFuncs.ToArray();
                            return result;
                        }
                    }
                }
                return null;
            }

            /*public ulong ToRVA(ulong adr) { return adr + OptionalHdr.ImageBase; }
            public ulong ToVA (ulong adr) { return adr - OptionalHdr.ImageBase; }*/

            public uint GetRawOffset(uint adr)
            {
                SectionHeader sectionHdr = GetSectionHeader(adr);
                return (adr - sectionHdr.VirtAdr) + sectionHdr.RawAdr;
            }

            public uint GetRawOffset(uint adr, uint imageBase) { return GetRawOffset(adr - imageBase); }
        }

        public class InstructionUtils
        {
            public static long GetCallAdr(X86Instruction callInst, uint imageBase = 0xFFFFFFFF)
            {
                if (callInst.HasDetails) { return imageBase != 0xFFFFFFFF ? callInst.Details.Displacement - imageBase : callInst.Details.Displacement; }
                else
                {
                    if (callInst.Operand.Contains(" ptr ["))
                    {
                        foreach (string str in callInst.Operand.Split('['))
                        {
                            try { return imageBase != 0xFFFFFFFF ? Utils.FromHexInt64(str) - imageBase : Utils.FromHexInt64(str); }
                            catch { }
                        }
                        throw new Exception("Could not process call operand!");
                    }
                    else { return imageBase != 0xFFFFFFFF ? Utils.FromHexInt64(callInst.Operand) - imageBase : Utils.FromHexInt64(callInst.Operand); }
                }
            }
        }
    }
}
