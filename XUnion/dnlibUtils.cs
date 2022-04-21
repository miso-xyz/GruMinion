using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet.Emit;
using dnlib.DotNet;
using dnlib;

namespace XUnion
{
    class dnlibUtils
    {
        public static int IndexOf(Instruction[] insts, Instruction inst, bool throwIfInvalid = false)
        {
            if (insts.Contains(inst)) { return insts.ToList().IndexOf(inst); } else { if (throwIfInvalid) { new IndexOutOfRangeException(); } return -1; }
        }
    }

    class MSILArray
    {
       /* 0x00 = Array Length                                              | ldc
        * 0x01 = Array Type                                                | newarr
        * 0x02 = Set 0x00 and 0x01 to defined Array Stack Value/Local      | stloc
        * 
        * --- ITEM LAYOUT ---
        * 
        * 0x00 = Load defined Array Stack Value/Local                      | ldloc
        * 0x01 = Index of current item                                     | ldc
        * 0x02 = Data                                                      | OpCode with Operand of matching nature with Array Type
        * 0x03 = Replaces data at 0x01 to 0x02                             | stelem                                                  */

        public MSILArray()                                   { }
        public MSILArray(Instruction[] insts)                { ParseArray(insts); }
        public MSILArray(Instruction[] insts, int index = 0) { ParseArray(insts, index); }
        public MSILArray(MethodDef method   , int index = 0) { ParseArray(method.Body.Instructions.ToArray(), index); }

        public static MSILArray ParseArray(Instruction[] insts, int index = 0)
        {
            int ARR_SIZE = insts[index].GetLdcI4Value();
            Local ARR_VAL = (Local)insts[index + 2].Operand;
            ITypeDefOrRef ARR_TYPE = (ITypeDefOrRef)insts[index + 1].Operand;

            List<Instruction> arr = new List<Instruction>(ARR_SIZE);
            MSILArray result = new MSILArray();

            for (int x_item = index + 5; arr.Count < ARR_SIZE; x_item += 4)
            {
                Instruction CurrentItem = insts[x_item],
                            IndexInstruction = insts[x_item-1];

                if (!IndexInstruction.IsLdcI4()) { break; }

                int CurrentIndex = IndexInstruction.GetLdcI4Value();
                if (IndexInstruction.GetLdcI4Value() != arr.Count)
                {
                    for (int x_offset = arr.Count; x_offset < CurrentIndex; x_offset++)
                    {
                        if (IsLdcI4Array(ARR_TYPE)) { arr.Add(    Instruction.CreateLdcI4(0));    }
                        else                        { arr.Add(new Instruction(OpCodes.UNKNOWN1)); }
                    }
                }
                arr.Add(insts[x_item]);
            }

            result.Type = ARR_TYPE;
            result.Items = arr.ToArray();
            result.LocalStack = ARR_VAL;
            result.RawSource = insts.ToList().GetRange(index, ARR_SIZE * 4).ToArray();

            return result;
        }

        private static bool IsLdcI4Array(ITypeDefOrRef srcType)
        {
            if (srcType.FullName == typeof(int).FullName ||
                srcType.FullName == typeof(short).FullName ||
                srcType.FullName == typeof(ushort).FullName ||
                srcType.FullName == typeof(byte).FullName ||
                srcType.FullName == typeof(long).FullName ||
                srcType.FullName == typeof(ulong).FullName ||
                srcType.FullName == typeof(uint).FullName)
            { return true; }
            return false;
        }

        public object[] ToArray() 
        {
            object[] obj = new object[Items.Length];
            for (int x = 0; x < Items.Length; x++) { if (Items[x].IsLdcI4()) { obj[x] = Items[x].GetLdcI4Value(); } else { obj[x] = Items[x].Operand; } }
            return obj;
        }

        public int RawCount             { get { return (Items.Length * 4) + 3; } }
        public Instruction[] RawSource  { get; set; }
        public Instruction[] Items      { get; set; }
        public ITypeDefOrRef Type       { get; set; }
        public Local LocalStack         { get; set; }
    }

    class MSILTryCatch
    {
        public MSILTryCatch() { }
        public MSILTryCatch(CilBody body       , int exhdIndex        ) { Parse(body.Instructions.ToArray(), body.ExceptionHandlers[exhdIndex]); }
        public MSILTryCatch(MethodDef method   , int exhdIndex        ) { Parse(method.Body.Instructions.ToArray(), method.Body.ExceptionHandlers[exhdIndex]); }
        public MSILTryCatch(Instruction[] insts, ExceptionHandler exhd) { Parse(insts, exhd); }

        public static MSILTryCatch Parse(Instruction[] insts, ExceptionHandler exhd)
        {
            List<Instruction> TryInstructions     = new List<Instruction>(),
                              HandlerInstructions = new List<Instruction>();

            // Try Instructions
            for (int x_tryInst = dnlibUtils.IndexOf(insts, exhd.TryStart    , true); x_tryInst < dnlibUtils.IndexOf(insts, exhd.TryEnd);     x_tryInst++) { TryInstructions.Add(insts[x_tryInst]); }

            // Handler Instructions
            for (int x_hdInst  = dnlibUtils.IndexOf(insts, exhd.HandlerStart, true); x_hdInst  < dnlibUtils.IndexOf(insts, exhd.HandlerEnd); x_hdInst++)  { HandlerInstructions.Add(insts[x_hdInst]); }

            MSILTryCatch result = new MSILTryCatch()
            {
                TryInstructions     = TryInstructions.ToArray(),
                HandlerInstructions = HandlerInstructions.ToArray(),
                CatchType           = exhd.CatchType,
                HandlerType         = exhd.HandlerType,

                TryStart            = dnlibUtils.IndexOf(insts, exhd.TryStart),
                TryEnd              = dnlibUtils.IndexOf(insts, exhd.TryEnd),
                HandlerStart        = dnlibUtils.IndexOf(insts, exhd.HandlerStart),
                HandlerEnd          = dnlibUtils.IndexOf(insts, exhd.HandlerEnd),
                FilterStart         = dnlibUtils.IndexOf(insts, exhd.FilterStart)
            };

            return result;
        }

        public Instruction[] TryInstructions        { get; set; }
        public Instruction[] HandlerInstructions    { get; set; }

        public int TryStart, TryEnd, HandlerStart, HandlerEnd, FilterStart;

        public ExceptionHandlerType HandlerType;
        public ITypeDefOrRef        CatchType;
    }
}