using System;
using System.Collections.Generic;
using System.Linq;
using DotNetExport.dnlib.PE;

namespace DotNetExport.dnlib.DotNet.Writer
{
    public class ImportList
    {
        public Dictionary<string, List<string>> Imports { get; } = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);
        private readonly bool _is64Bit;
        internal Action OnSizeChange = null;

        public ImportList(bool is64Bit)
        {
            _is64Bit = is64Bit;
        }

        public RVA Find(string dllName, string funcName)
        {
            if (!Imports.TryGetValue(dllName, out var functions)) return 0;
            var importIndex = Imports.Keys.ToList().FindIndex(k => string.Equals(k, dllName, StringComparison.InvariantCultureIgnoreCase));
            var functionIndex = functions.IndexOf(funcName);
            var pointerSize = _is64Bit ? 8 : 4;
            return (RVA)(Imports.Values.Take(importIndex).Sum(i => i.Count) * pointerSize + functionIndex * pointerSize);
        }

        public void Add(string dllName, string funcName)
        {
            if (Imports.TryGetValue(dllName, out var functions))
                functions.Add(funcName);
            else
                Imports.Add(dllName, new List<string> {funcName});
            OnSizeChange?.Invoke();
        }

        public void Remove(string dllName, string funcName)
        {
            if (!Imports.TryGetValue(dllName, out var functions)) return;
            functions.Remove(funcName);
            if (functions.Count == 0)
                RemoveDll(dllName);
            OnSizeChange?.Invoke();
        }

        public void RemoveDll(string dllName)
        {
            Imports.Remove(dllName);
            OnSizeChange?.Invoke();
        }

        public void Clear()
        {
            Imports.Clear();
            OnSizeChange?.Invoke();
        }
    }
}