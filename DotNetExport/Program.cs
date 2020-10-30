using DotNetExport.dnlib.DotNet;
using DotNetExport.dnlib.DotNet.MD;
using DotNetExport.dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CallingConvention = System.Runtime.InteropServices.CallingConvention;

namespace DotNetExport
{
    class Program
    {
        private static MethodDef _nativeEntryPoint;

        private static MethodDef FindNativeEntryPoint(IEnumerable<MethodDef> methods)
        {
            var methodsWithAttribute = methods.Where(m =>
                m.CustomAttributes.IsDefined("System.Runtime.InteropServices.DllMainAttribute")).ToList();
            if (methodsWithAttribute.Count > 1) 
                throw new AmbiguousMatchException("More then one DllMain found!");
            if (methodsWithAttribute.Count == 0) return null;
            var method = methodsWithAttribute[0];
            if (!method.IsStatic || method.ReturnType.FullName != "System.Void" || method.Parameters.Count != 1 || method.Parameters[0].Type.FullName != "System.IntPtr")
                throw new ArgumentException("DllMain should match signature - static void DllMain(IntPtr hModule)");
            method.CustomAttributes.Remove(
                method.CustomAttributes.Find("System.Runtime.InteropServices.DllMainAttribute"));
            return method;
        }

        public static object GetExportArgument(CAArgument argument)
        {
            switch (argument.Type.FullName)
            {
                case "System.String":
                    return argument.Value.ToString();
                case "System.Runtime.InteropServices.CallingConvention":
                    return (CallingConvention) (int) argument.Value;
                default:
                    throw new ArgumentException("Unknown DllExport argument!");
            }
        }

        private static List<ExportableMethod> FindExports(IEnumerable<MethodDef> methods)
        {
            var result = new List<ExportableMethod>();
            foreach (var method in methods)
            {
                var attribute = method.CustomAttributes.Find("System.Runtime.InteropServices.DllExportAttribute");
                if (attribute == null) continue;
                switch (attribute.ConstructorArguments.Count)
                {
                    case 0:
                        result.Add(new ExportableMethod(method));
                        break;
                    case 1:
                        var argument = GetExportArgument(attribute.ConstructorArguments[0]);
                        if (argument is string name)
                            result.Add(new ExportableMethod(method, name));
                        else
                            result.Add(new ExportableMethod(method, method.Name.ToString(), (CallingConvention)argument));
                        break;
                    case 2:
                        argument = GetExportArgument(attribute.ConstructorArguments[0]);
                        var argument1 = GetExportArgument(attribute.ConstructorArguments[1]);
                        if (argument1.GetType() == argument.GetType())
                            throw new ArgumentException("Unknown DllExport argument!");
                        if (argument is CallingConvention callingConvention)
                            result.Add(new ExportableMethod(method, (string)argument1, callingConvention));
                        else
                            result.Add(new ExportableMethod(method, (string)argument, (CallingConvention)argument1));
                        break;
                }

                method.CustomAttributes.Remove(attribute);
            }
            if (new HashSet<string>(result.Select(m => m.Name),StringComparer.InvariantCultureIgnoreCase).Count != result.Count)
                throw new AmbiguousMatchException("Found duplicated export names!");
            return result;
        }

        private static string CallingConventionString(dnlib.DotNet.CallingConvention callingConvention)
        {
            switch (callingConvention)
            {
                case dnlib.DotNet.CallingConvention.C:
                    return "CallConvCdecl";
                case dnlib.DotNet.CallingConvention.StdCall:
                    return "CallConvStdcall";
                case dnlib.DotNet.CallingConvention.ThisCall:
                    return "CallConvThiscall";
                default:
                    throw new ArgumentOutOfRangeException(nameof(callingConvention), callingConvention, null);
            }
        }

        public static void ApplyExportBugFix(MethodDef method, dnlib.DotNet.CallingConvention callingConvention)
        {
            var type = method.MethodSig.RetType;
            type = new CModOptSig(method.Module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", CallingConventionString(callingConvention)), type);
            method.MethodSig.RetType = type;
            var ca = method.Module.Assembly.CustomAttributes.Find("System.Diagnostics.DebuggableAttribute");
            if (!(ca is null) && ca.ConstructorArguments.Count == 1)
            {
                var arg = ca.ConstructorArguments[0];
                if (arg.Type.FullName == "System.Diagnostics.DebuggableAttribute/DebuggingModes" && arg.Value is int value && value == 0x107)
                {
                    arg.Value = value & ~(int)DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
                    ca.ConstructorArguments[0] = arg;
                }
            }

        }

        private static void DeleteFile(string fileName)
        {
            try
            {
                File.Delete(fileName);
                return;
            }
            catch
            {
                // try via cmd
            }

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C del \"{fileName}\""
                }
            };
            process.Start();
            process.WaitForExit(3000);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("No input provided. Skipping...");
                goto clean;
            }
            var module = ModuleDefMD.Load(args[0]);
            var methods = module.Types.SelectMany(t => t.Methods);
            _nativeEntryPoint = FindNativeEntryPoint(methods);
            var exportableMethods = FindExports(methods);
            if (_nativeEntryPoint == null && exportableMethods.Count == 0)
            {
                Console.WriteLine("No DllMain or DllExport attributes found. Skipping...");
                goto clean;
            }

            var ordinal = (ushort)0;
            if (_nativeEntryPoint != null)
            {
                ApplyExportBugFix(_nativeEntryPoint, dnlib.DotNet.CallingConvention.StdCall);
                _nativeEntryPoint.ExportInfo = new MethodExportInfo(ordinal++);
            }

            foreach (var method in exportableMethods)
            {
                ApplyExportBugFix(method.Method, method.CallingConvention);
                method.Method.ExportInfo = new MethodExportInfo(method.Name, ordinal++, MethodExportInfoOptions.FromUnmanaged);
            }

            using (var stream = new MemoryStream())
            {
                var options = new ModuleWriterOptions(module);
                if ((options.Cor20HeaderOptions.Flags & ComImageFlags.NativeEntryPoint) == 0)
                    options.Cor20HeaderOptions.Flags |= ComImageFlags.NativeEntryPoint;
                if ((options.Cor20HeaderOptions.Flags & ComImageFlags.ILOnly) != 0)
                    options.Cor20HeaderOptions.Flags &= ComImageFlags.NativeEntryPoint;
                options.WriterEvent += OptionsOnWriterEvent;
                module.Write(stream, options);
                Console.WriteLine($"All exports are successfully added!");
                DeleteFile(args[0]);
                File.WriteAllBytes(args[0], stream.ToArray());
            }
            clean:
            var manifestDll = Path.Combine(Path.GetDirectoryName(args[0]), "DotNetExport.Attribute.dll");
            if (File.Exists(manifestDll))
                DeleteFile(manifestDll);
        }

        private static void OptionsOnWriterEvent(object sender, ModuleWriterEventArgs e)
        {
            switch (e.Event)
            {
                case ModuleWriterEvent.ChunksAddedToSections:
                {
                    var writerNativeEntryPoint = ((ModuleWriter)e.Writer).NativeEntryPoint;
                    if (_nativeEntryPoint != null)
                    {
                        writerNativeEntryPoint.Enable = true;
                        writerNativeEntryPoint.ManagedEntryPoint = _nativeEntryPoint;
                    }
                    else
                    {
                        writerNativeEntryPoint.Enable = false;
                    }
                    return;
                }
                case ModuleWriterEvent.EndCalculateRvasAndFileOffsets:
                    if (_nativeEntryPoint != null)
                        ((ModuleWriter) e.Writer).Metadata.module.NativeEntryPoint = ((ModuleWriter) e.Writer).NativeEntryPoint.RVA;
                    break;
            }
        }
    }
}
