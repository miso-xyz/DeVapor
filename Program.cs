using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.IO;

namespace VaporDeobfuscator
{
    class Program
    {
        private static ModuleDefMD asm;
        private static int stringfixcounter = 0, removedTypecounter = 0, antiDe4dotCounter = 0;
        private static bool showAll = false;

        static void Main(string[] args)
        {
            Console.Title = "DeVapor";
            Console.WriteLine("DeVapor - Deobfuscator for VaporObfuscator | by misonothx");
            Console.WriteLine(" |- https://github.com/miso-xyz/DeVapor/");
            Console.WriteLine();
            asm = ModuleDefMD.Load(args[0]);
            showAll = args.Contains("-showAll");
            if (asm.Name == "ObfuscatedByVapor")
            {
                asm.Name = asm.Assembly.Name + Path.GetExtension(args[0]);
            }
            removeJunkTypes();
            if (showAll) { Console.WriteLine(); }
            fixStrings();
            ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(asm);
            moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            NativeModuleWriterOptions nativeModuleWriterOptions = new NativeModuleWriterOptions(asm, true);
            nativeModuleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            nativeModuleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            if (showAll) { Console.WriteLine(); }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("###################################################");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("    " + stringfixcounter + " Strings Fixed (Base64 Encoded)");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("    " + removedTypecounter + " Removed Types (" + antiDe4dotCounter + " AntiDe4dots Removed)");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("###################################################");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Now saving...");
            try
            {
                asm.Write(Path.GetFileNameWithoutExtension(args[0]) + "-DeVapor" + Path.GetExtension(args[0]));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to save! (" + ex.Message + ")");
                goto end_;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfully saved!");
        end_:
            Console.ResetColor();
            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }

        static void PrintRemoved(string name, string type)
        {
            if (!showAll) { return; }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("[Removed]: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("'" + name + "'");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" (" + type + ")");
            Console.ResetColor();
        }

        static void PrintStringFixed(string from, string to)
        {
            if (!showAll) { return; }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("[Fixed]: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("'" + from + "'");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("'" + to + "'");
            Console.ResetColor();
        }

        static void removeJunkTypes()
        {
            List<string> attribs = new List<string>() { "YanoAttribute", "Xenocode.Client.Attributes.AssemblyAttributes.ProcessedByXenocode", "PoweredByAttribute", "ObfuscatedByGoliath", "NineRays.Obfuscator.Evaluation", "NetGuard", "dotNetProtector", "DotNetPatcherPackerAttribute", "DotNetPatcherObfuscatorAttribute", "DotfuscatorAttribute", "CryptoObfuscator.ProtectedWithCryptoObfuscatorAttribute", "BabelObfuscatorAttribute", "BabelAttribute", "AssemblyInfoAttribute" };
            for (int x = 0; x < asm.Types.Count; x++)
            {
                if (attribs.Contains(asm.Types[x].Name) && attribs.Contains(asm.Types[x].Namespace)) { antiDe4dotCounter++; removedTypecounter++; PrintRemoved(asm.Types[x].Name, "Class (AntiDe4dot)"); asm.Types.RemoveAt(x); x--; }
                if (asm.Types[x].Name.Contains("俺ム仮 ｎｏ　ｓｌｅｅｐ　俺ム仮") && asm.Types[x].Namespace.Contains("俺ム仮 ｎｏ　ｓｌｅｅｐ　俺ム仮")) { removedTypecounter++; PrintRemoved(asm.Types[x].Name, "Class"); asm.Types.RemoveAt(x); x--; }
                if (asm.Types[x].HasInterfaces)
                {
                    foreach (InterfaceImpl interface_ in asm.Types[x].Interfaces)
                    {
                        if (interface_.Interface.Name.Contains(asm.Types[x].Name))
                        {
                            removedTypecounter++;
                            PrintRemoved(interface_.Interface.Name, "Interface Type");
                            asm.Types.RemoveAt(x);
                            x--;
                        }
                    }
                }
            }
        }

        static void fixStrings()
        {
            foreach (TypeDef t_ in asm.Types)
            {
                if (!t_.HasMethods) { continue; }
                foreach (MethodDef methods in t_.Methods)
                {
                    methods.Body.KeepOldMaxStack = true;
                    if (!methods.HasBody) { continue; }
                    for (int x = 0; x < methods.Body.Instructions.Count; x++)
                    {
                        Instruction inst = methods.Body.Instructions[x];
                        if (inst.OpCode.Equals(OpCodes.Ldstr) && methods.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Call))
                        {
                            if (methods.Body.Instructions[x + 1].Operand.ToString().Contains("FromBase64String"))
                            {
                                PrintStringFixed(inst.Operand.ToString(), Encoding.UTF8.GetString(Convert.FromBase64String(inst.Operand.ToString())));
                                methods.Body.Instructions.RemoveAt(x - 1); // Removes GetUTF8 String Call
                                methods.Body.Instructions[x - 1].Operand = Encoding.UTF8.GetString(Convert.FromBase64String(inst.Operand.ToString()));
                                methods.Body.Instructions.RemoveAt(x); // Removes Original String (Base64 Encoded)
                                methods.Body.Instructions.RemoveAt(x); // Removes FromBase64String Call
                                stringfixcounter++;
                                //methods.Body.Instructions.RemoveAt(x + 1); // Removes ToUTF8String Call
                            }
                        }
                    }
                }
            }
        }
    }
}
