using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace XUnion.Deobfuscators
{
    public class v4
    {
        private XUnion.v4.SharedItems SharedItems;
        private XORManager xor;
        private ModuleDefMD asm;

        public int XORFixCount, XORStringFixCount, RenameFixCount, DelegateFixCount;

        public v4(ModuleDefMD module, XORManager xorMng)                              
        {
            asm = module;
            xor = xorMng;
        }
        public v4(ref XUnion.v4.SharedItems sharedItems, ModuleDefMD module)          
        {
            SharedItems = sharedItems;
            xor = sharedItems.XORMng;
            asm = module;
        }

        public void Deobfuscate()                                                     
        {
            FixRenaming();
            FixXOR();
            FixDelegates();
        }

        public void FixRenaming()   // Renames Methods                                
        {
            asm.EntryPoint.DeclaringType.Name = "Program";
            asm.EntryPoint.Parameters[0].Name = "args";
            RenameFixCount += 2;

            if (SharedItems.ResourceDecrypt != null)
            {
                SharedItems.ResourceDecrypt.Name = "GetProtectedAppStream";
                SharedItems.ResourceDecrypt.Parameters[0].Name = "resourceName";
                RenameFixCount += 2;
            }

            List<TypeDef> typesToCleanUp = new List<TypeDef>();
            typesToCleanUp.AddRange(asm.Types);
            for (int x = 0; x < asm.EntryPoint.DeclaringType.NestedTypes.Count; x++)
            {
                TypeDef nestedType = asm.EntryPoint.DeclaringType.NestedTypes[x];
                if (nestedType.IsDelegate) { continue; }
                typesToCleanUp.Add(nestedType);
            }

            for (int x_type = 0; x_type < typesToCleanUp.Count; x_type++)
            {
                TypeDef type = typesToCleanUp[x_type];
                int delegateCount = 0;

                for (int x_nestedTypes = 0; x_nestedTypes < type.NestedTypes.Count; x_nestedTypes++)
                {
                    TypeDef nestedType = type.NestedTypes[x_nestedTypes];
                    if (nestedType.IsDelegate) { nestedType.Name = "Delegate_" + delegateCount++; RenameFixCount++; continue; }
                }

                for (int x_method = 0; x_method < type.Methods.Count; x_method++)
                {
                    MethodDef methods = type.Methods[x_method];

                    if (methods.HasImplMap)
                    {
                        methods.Name = methods.ImplMap.Module.FullName.Split('.')[0] + "." + methods.ImplMap.Name;
                        switch (methods.Name)
                        {
                            case "kernel32.GetProcAddress": methods.Parameters[0].Name = "hModule";
                                                            methods.Parameters[1].Name = "lpProcName"   ; RenameFixCount += 2; break;
                            case "kernel32.LoadLibraryA":   methods.Parameters[0].Name = "lpLibFileName"; RenameFixCount++   ; break;
                        }
                        continue;
                    }

                    if (!methods.HasBody) { continue; }
                    Instruction[] insts = methods.Body.Instructions.ToArray();
                    try
                    {
                        if ((insts[0].OpCode == OpCodes.Ldarga_S || insts[0].OpCode == OpCodes.Ldarga) &&
                            (insts[2].OpCode == OpCodes.Ldarga_S || insts[2].OpCode == OpCodes.Ldarga) &&
                            insts[4].OpCode == OpCodes.Call &&
                            insts[4].Operand is MethodSpec &&
                            insts[5].OpCode == OpCodes.Ret &&
                            Utils.CompareNamespaceMethodSpec(insts[4], typeof(Marshal), "GetDelegateForFunctionPointer", new Type[] { typeof(IntPtr) }))
                        {
                            methods.GenericParameters[0].Name = "TDelegate";
                            methods.Parameters[0].Name = "moduleName";
                            methods.Parameters[1].Name = "procName";
                            methods.Name = "GetDelegateForPtr";
                            RenameFixCount += 4;
                            continue;
                        }

                        if (insts[0].IsLdarg() &&
                            insts[1].IsLdarg() &&
                            insts[2].IsLdcI4() &&
                            insts[3].OpCode == OpCodes.Xor &&
                            insts[4].OpCode == OpCodes.Xor &&
                            insts[5].OpCode == OpCodes.Ret)
                        {
                            methods.Name = "Int32Xor";
                            methods.Parameters[0].Name = "value1";
                            methods.Parameters[1].Name = "value2";
                            RenameFixCount += 3;
                            continue;
                        }

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

                        if (insts[7].IsLdcI4() &&
                            insts[8].IsLdcI4() &&
                            insts[10].Operand != null &&
                            insts[10].Operand is MethodSpec &&
                            insts[14].Operand != null &&
                            insts[14].Operand is MethodSpec &&
                            Utils.CompareNamespaceMethodSpec(insts[10], typeof(Enumerable), "Skip"))
                        {
                            methods.Name = "UnXorToString";
                            methods.Parameters[0].Name = "data";
                            RenameFixCount += 2;
                            continue;
                        }

                        if (insts[15].OpCode == OpCodes.Callvirt &&
                            insts[15].Operand is MemberRef &&
                            insts[12].OpCode == OpCodes.Callvirt &&
                            insts[12].Operand is MemberRef &&
                            insts[6].OpCode == OpCodes.Ldftn &&
                            insts[6].Operand is MethodDef &&
                            Utils.CompareNamespaceMemberRef(insts[15], typeof(Thread), "Start", new Type[] { }) &&
                            asm.EntryPoint.DeclaringType.NestedTypes.Contains(((MethodDef)insts[6].Operand).DeclaringType))
                        {
                            MethodDef nestedMethod = ((MethodDef)insts[6].Operand);
                            if (nestedMethod.Body.Instructions[3].OpCode == OpCodes.Callvirt &&
                                nestedMethod.Body.Instructions[3].Operand is MemberRef &&
                                nestedMethod.Body.Instructions[10].OpCode == OpCodes.Ldsfld &&
                                nestedMethod.Body.Instructions[10].Operand is FieldDef &&
                                nestedMethod.Body.Instructions[13].OpCode == OpCodes.Callvirt &&
                                nestedMethod.Body.Instructions[13].Operand is MemberRef &&
                                Utils.CompareNamespaceMemberRef(nestedMethod.Body.Instructions[3], typeof(Assembly), "get_EntryPoint", new Type[] { }) &&
                                Utils.CompareNamespaceMemberRef(nestedMethod.Body.Instructions[13], typeof(MethodBase), "Invoke", new Type[] { typeof(object), typeof(object[]) }))
                            {
                                methods.Name = "InvokeProtectedApp";
                                methods.Parameters[0].Name = "rawProtectedApp";
                                RenameFixCount += 2;
                                continue;
                            }
                        }
                    }
                    catch { }

                    for (int x = 0; x < insts.Length; x++)
                    {
                        if (insts[x].OpCode == OpCodes.Callvirt &&
                            insts[x].Operand is MethodDef &&
                            ((MethodDef)insts[x].Operand).DeclaringType.IsDelegate)
                        {
                            methods.Name = "InitializeDelegates";
                            RenameFixCount++;
                            break;
                        }

                        if (insts[x].OpCode == OpCodes.Stsfld &&
                            insts[x].Operand is FieldDef &&
                            insts[x - 1].OpCode == OpCodes.Call &&
                            insts[x - 1].Operand is MethodSpec &&
                            insts[x - 7].OpCode == OpCodes.Stsfld &&
                            insts[x - 7].Operand is FieldDef &&
                            Utils.CompareNamespaceMethodSpec(insts[x - 1], typeof(Enumerable), "ToArray") &&
                            Utils.CompareNamespaceMethodSpec(insts[x - 2], typeof(Enumerable), "Skip"))
                        {
                            ((FieldDef)insts[x].Operand).Name = "protectedAppArgs";
                            ((FieldDef)insts[x - 7].Operand).Name = "PEunionArgs";
                            RenameFixCount += 2;
                            break;
                        }
                    }
                }
            }
        }
        public void FixXOR()        // Fixes XOR strings & integers                   
        {
            List<TypeDef> typesToCleanUp = new List<TypeDef>();
            typesToCleanUp.AddRange(asm.Types);
            for (int x = 0; x < asm.EntryPoint.DeclaringType.NestedTypes.Count; x++)
            {
                TypeDef nestedType = asm.EntryPoint.DeclaringType.NestedTypes[x];
                if (nestedType.IsDelegate) { continue; }
                typesToCleanUp.Add(nestedType);
            }

            for (int x_type = 0; x_type < typesToCleanUp.Count; x_type++)
            {
                TypeDef type = typesToCleanUp[x_type];
                for (int x_method = 0; x_method < type.Methods.Count; x_method++)
                {
                    MethodDef methods = type.Methods[x_method];
                    if (!methods.HasBody) { continue; }
                    for (int x = 0; x < methods.Body.Instructions.Count; x++)
                    {
                        Instruction[] insts = methods.Body.Instructions.ToArray();
                        Instruction inst = insts[x];
                        if (inst.OpCode == OpCodes.Newarr &&
                            inst.Operand is TypeRef &&
                            ((TypeRef)inst.Operand).FullName == typeof(ushort).FullName)
                        {
                            Local arrLoc = insts[x + 1].GetLocal(methods.Body.Variables);
                            MSILArray arr = MSILArray.ParseArray(insts, x - 1);
                            methods.Body.Instructions[x - 1].OpCode = OpCodes.Ldstr;
                            methods.Body.Instructions[x - 1].Operand = xor.Compute(Utils.ToUInt16(arr.ToArray()));
                            for (int i = 0; i < arr.RawCount; i++) { methods.Body.Instructions.RemoveAt(x); }
                            methods.Body.Variables.Remove(arrLoc);
                            inst = methods.Body.Instructions[x];
                            if (inst.OpCode == OpCodes.Call &&
                                inst.Operand is MethodDef &&
                                ((MethodDef)inst.Operand).Name == "UnXorToString") { methods.Body.Instructions.RemoveAt(x); }
                            XORStringFixCount++;
                            continue;
                        }

                        if (inst.OpCode == OpCodes.Call &&
                            inst.Operand is MethodDef &&
                            ((MethodDef)inst.Operand).Name == "Int32Xor" &&
                            insts[x - 2].IsLdcI4() &&
                            insts[x - 1].IsLdcI4())
                        {
                            methods.Body.Instructions[x - 2].Operand = xor.ToInt(insts[x - 2].GetLdcI4Value(), insts[x - 1].GetLdcI4Value());
                            methods.Body.Instructions.RemoveAt(x - 1);
                            methods.Body.Instructions.RemoveAt(x - 1);
                            XORFixCount++;
                            continue;
                        }
                    }
                }
            }
        }
        public void FixDelegates()  // Removes unused delegates and renames used ones 
        {
            for (int x_method = 0; x_method < asm.EntryPoint.DeclaringType.Methods.Count; x_method++)
            {
                MethodDef methods = asm.EntryPoint.DeclaringType.Methods[x_method];
                if (methods.Name != "InitializeDelegates") { continue; }

                Instruction[] insts = methods.Body.Instructions.ToArray();
                for (int x = 0; x < insts.Length; x++)
                {
                    Instruction inst = insts[x];

                    if (inst.OpCode == OpCodes.Call &&
                        inst.Operand is MethodSpec &&
                        ((MethodSpec)inst.Operand).Name == "GetDelegateForPtr" &&
                        insts[x + 1].IsStloc() &&
                        insts[x - 2].OpCode == OpCodes.Ldstr &&       // Requires FixXOR
                        insts[x - 1].OpCode == OpCodes.Ldstr)         // Requires FixXOR
                    {
                        Local tempLoc = insts[x + 1].GetLocal(methods.Body.Variables);
                        ITypeDefOrRef delType;
                        switch (insts[x - 1].Operand.ToString())
                        {
                            case "SetErrorMode":        // kernel32.SetErrorMode
                            case "VirtualAllocExNuma":  // kernel32.VirtualAllocExNuma
                                if (isLocalTypeDelegate(tempLoc, out delType)) { delType.Name = /*insts[x - 2].Operand.ToString().Split('.')[0] + "." + */ insts[x - 1].Operand.ToString(); }
                                DelegateFixCount++;
                                break;
                        }
                    }
                }
            }

            for (int x_nestedTypes = 0; x_nestedTypes < asm.EntryPoint.DeclaringType.NestedTypes.Count; x_nestedTypes++)
            {
                TypeDef nestedType = asm.EntryPoint.DeclaringType.NestedTypes[x_nestedTypes];
                if (nestedType.IsDelegate)
                {
                    switch (nestedType.Name)
                    {
                        case "SetErrorMode":
                            for (int x_method = 0; x_method < nestedType.Methods.Count; x_method++)
                            {
                                MethodDef method = nestedType.Methods[x_method];
                                switch (method.Name)
                                {
                                    case "Invoke":
                                    case "BeginInvoke":
                                        int offset = (method.Parameters[0].MethodSigIndex == -2) ? 1 : 0;
                                        method.Parameters[offset++].Name = "uMode"; break;
                                }
                            }
                            break;
                        case "VirtualAllocExNuma":
                            for (int x_method = 0; x_method < nestedType.Methods.Count; x_method++)
                            {
                                MethodDef method = nestedType.Methods[x_method];
                                switch (method.Name)
                                {
                                    case "Invoke":
                                    case "BeginInvoke":
                                        int offset = (method.Parameters[0].MethodSigIndex == -2) ? 1 : 0;
                                        method.Parameters[offset++].Name = "hProcess";
                                        method.Parameters[offset++].Name = "IpAddress";
                                        method.Parameters[offset++].Name = "dwSize";
                                        method.Parameters[offset++].Name = "flAllocationType";
                                        method.Parameters[offset++].Name = "flProtect";
                                        method.Parameters[offset++].Name = "nndPreferred";
                                        break;
                                }
                            }
                            break;
                        default:
                            asm.EntryPoint.DeclaringType.NestedTypes.RemoveAt(x_nestedTypes);
                            x_nestedTypes--;
                            break;
                    }
                }
            }
        }

        private bool isLocalTypeDelegate(Local loc, out ITypeDefOrRef delegateType)   
        {
            delegateType = null;
            if (loc.Type is TypeSig && ((TypeSig)loc.Type).IsTypeDefOrRef)
            {
                ITypeDefOrRef del = ((TypeSig)loc.Type).ToTypeDefOrRef();
                if (((TypeDef)del).IsDelegate) { delegateType = del; return true; }
            }
            return false;
        }
    }
}
