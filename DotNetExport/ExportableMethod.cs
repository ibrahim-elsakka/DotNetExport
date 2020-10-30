using System;
using DotNetExport.dnlib.DotNet;

namespace DotNetExport
{
    public class ExportableMethod
    {
        public string Name { get; }
        public MethodDef Method { get; }
        public CallingConvention CallingConvention { get; }

        public ExportableMethod(MethodDef method) : this(method, method.Name.ToString())
        {

        }

        public ExportableMethod(MethodDef method, string name) : this(method, name, System.Runtime.InteropServices.CallingConvention.Cdecl)
        {

        }

        public ExportableMethod(MethodDef method, string name, System.Runtime.InteropServices.CallingConvention callingConvention)
        {
            Method = method;
            Name = name;
            switch (callingConvention)
            {
                case System.Runtime.InteropServices.CallingConvention.Winapi:
                case System.Runtime.InteropServices.CallingConvention.StdCall:
                    CallingConvention = CallingConvention.StdCall;
                    break;
                case System.Runtime.InteropServices.CallingConvention.Cdecl:
                    CallingConvention = CallingConvention.C;
                    break;
                case System.Runtime.InteropServices.CallingConvention.ThisCall:
                    CallingConvention = CallingConvention.ThisCall;
                    break;
                case System.Runtime.InteropServices.CallingConvention.FastCall:
                    throw new NotImplementedException("Sorry FastCall is not supported by clr (https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.callconvfastcall)");
                default:
                    throw new ArgumentOutOfRangeException(nameof(callingConvention), callingConvention, null);
            }

        }
    }
}