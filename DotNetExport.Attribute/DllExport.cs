namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DllExportAttribute : System.Attribute
    {
        public DllExportAttribute(string name, CallingConvention callingConvention) { }

        public DllExportAttribute(string name) { }

        public DllExportAttribute(CallingConvention callingConvention) { }

        public DllExportAttribute() { }
    }
}