namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DllMainAttribute : System.Attribute
    {
        public DllMainAttribute() { }
    }
}