using System;
using System.Collections.Generic;
using System.Linq;
using DotNetExport.dnlib.DotNet.Emit;
using DotNetExport.dnlib.IO;
using DotNetExport.dnlib.PE;

namespace DotNetExport.dnlib.DotNet.Writer
{
    public class NativeEntryPoint : IChunk
    {
        public MethodDef ManagedEntryPoint { get; set; }

        private byte[] EntryPointCode;

        private static readonly byte[] TemplateX32 = {
            0x83, 0x7C, 0x24, 0x08, 0x01, 0x75, 0x2D, 0x64, 0xA1, 0x18, 0x00, 0x00,
            0x00, 0x8B, 0x40, 0x30, 0x50, 0xFF, 0xB0, 0xA0, 0x00, 0x00, 0x00, 0xFF,
            0x15, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x74, 0x24, 0x08, 0xFF, 0x15, 0x00,
            0x00, 0x00, 0x00, 0x58, 0xFF, 0xB0, 0xA0, 0x00, 0x00, 0x00, 0xFF, 0x15,
            0x00, 0x00, 0x00, 0x00, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC2, 0x0C, 0x00
        };

        private static readonly byte[] TemplateX64 = {
            0x48, 0x83, 0xFA, 0x01, 0x75, 0x61, 0x55, 0x51, 0x50, 0x48, 0x89, 0xE5,
            0x48, 0x81, 0xE4, 0xF0, 0xFF, 0xFF, 0xFF, 0x48, 0x83, 0xEC, 0x20, 0x65,
            0x48, 0x8B, 0x04, 0x25, 0x30, 0x00, 0x00, 0x00, 0x48, 0x8B, 0x40, 0x60,
            0x48, 0x89, 0x45, 0x00, 0x48, 0x8B, 0x88, 0x10, 0x01, 0x00, 0x00, 0x48,
            0xB8, 0x08, 0x20, 0x8B, 0x50, 0xF9, 0x7F, 0x00, 0x00, 0xFF, 0x10, 0x48,
            0x8B, 0x4D, 0x08, 0x48, 0xB8, 0x00, 0x40, 0x8B, 0x50, 0xF9, 0x7F, 0x00,
            0x00, 0xFF, 0x10, 0x48, 0x8B, 0x45, 0x00, 0x48, 0x8B, 0x88, 0x10, 0x01,
            0x00, 0x00, 0x48, 0xB8, 0x00, 0x20, 0x8B, 0x50, 0xF9, 0x7F, 0x00, 0x00,
            0xFF, 0x10, 0x48, 0x8D, 0x65, 0x10, 0x5D, 0xB8, 0x01, 0x00, 0x00, 0x00,
            0xC3
        };

        private bool _enable;
        public bool Enable
        {
            get => _enable;
            set
            {
                if (value)
                {
                    writer.ImportDirectory.Imports.Clear();
                    writer.ImportDirectory.Imports.Add("ntdll.dll", "RtlEnterCriticalSection");
                    writer.ImportDirectory.Imports.Add("ntdll.dll", "RtlLeaveCriticalSection");
                    writer.ImportDirectory.Imports.Add("mscoree.dll", writer.ImportDirectory.IsExeFile ? "_CorExeMain" : "_CorDllMain");
                }
                else
                {
                    writer.ImportDirectory.Imports.Clear();
                    writer.ImportDirectory.Imports.Add("mscoree.dll", writer.ImportDirectory.IsExeFile ? "_CorExeMain" : "_CorDllMain");
                }
                _enable = value;
            }
        }

        private bool is64bit;
        private ModuleWriter writer;

        public NativeEntryPoint(ModuleWriter writer, bool is64bit)
        {
            this.writer = writer;
            this.is64bit = is64bit;
            if (is64bit)
            {
                EntryPointCode = new byte[TemplateX64.Length];
                Array.Copy(TemplateX64, 0, EntryPointCode, 0, TemplateX64.Length);
            }
            else
            {
                EntryPointCode = new byte[TemplateX32.Length];
                Array.Copy(TemplateX32, 0, EntryPointCode, 0, TemplateX32.Length);
            }
        }

        private FileOffset offset;
        private RVA rva;
        public FileOffset FileOffset => offset;
        public RVA RVA => rva;

        public void SetOffset(FileOffset offset, RVA rva)
        {
            this.offset = offset;
            this.rva = rva;
            if (!Enable) return;
            if (is64bit)
            {
                writer.RelocDirectory.Add(this, 0x31);
                writer.RelocDirectory.Add(this, 0x41);
                writer.RelocDirectory.Add(this, 0x58);

            } else 
            {
                writer.RelocDirectory.Add(this, 0x19);
                writer.RelocDirectory.Add(this, 0x23);
                writer.RelocDirectory.Add(this, 0x30);
            }
        }

        public uint GetFileLength()
        {
            if (!Enable) return 0;
            return (uint)EntryPointCode.Length;
        }

        public uint GetVirtualSize() => GetFileLength();

        public void WriteTo(DataWriter dataWriter)
        {
            if (!Enable) return;
            var entryPoint =
                writer.ExportsWriter.allMethodInfos.FirstOrDefault(m =>
                    m.Method.FullName == ManagedEntryPoint.FullName);
            if (entryPoint == null)
                throw new InvalidMethodException("Can't find managed DllMain!");
            if (is64bit)
            {
                Array.Copy(BitConverter.GetBytes((ulong)(writer.PEHeaders.ImageBase + (uint)writer.ImportDirectory.ImportAddress.RVA + (uint)writer.ImportDirectory.Imports.Find("ntdll.dll", "RtlLeaveCriticalSection"))), 0, EntryPointCode, 0x31, 8);
                Array.Copy(BitConverter.GetBytes((ulong)(writer.PEHeaders.ImageBase + (uint)writer.ExportsWriter.sdataChunk.RVA + entryPoint.ManagedVtblOffset)), 0, EntryPointCode, 0x41, 8);
                Array.Copy(BitConverter.GetBytes((ulong)(writer.PEHeaders.ImageBase + (uint)writer.ImportDirectory.ImportAddress.RVA + (uint)writer.ImportDirectory.Imports.Find("ntdll.dll", "RtlEnterCriticalSection"))), 0, EntryPointCode, 0x58, 8);

            }
            else
            {
                Array.Copy(BitConverter.GetBytes((uint)(writer.PEHeaders.ImageBase + (uint)writer.ImportDirectory.ImportAddress.RVA + (uint)writer.ImportDirectory.Imports.Find("ntdll.dll", "RtlLeaveCriticalSection"))), 0, EntryPointCode, 0x19, 4);
                Array.Copy(BitConverter.GetBytes((uint)(writer.PEHeaders.ImageBase + (uint)writer.ExportsWriter.sdataChunk.RVA + entryPoint.ManagedVtblOffset)), 0, EntryPointCode, 0x23, 4);
                Array.Copy(BitConverter.GetBytes((uint)(writer.PEHeaders.ImageBase + (uint)writer.ImportDirectory.ImportAddress.RVA + (uint)writer.ImportDirectory.Imports.Find("ntdll.dll", "RtlEnterCriticalSection"))), 0, EntryPointCode, 0x30, 4);
            }
            dataWriter.WriteBytes(EntryPointCode);
        }
    }
}