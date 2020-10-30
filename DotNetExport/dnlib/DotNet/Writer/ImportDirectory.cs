// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using DotNetExport.dnlib.IO;
using DotNetExport.dnlib.PE;
using System.IO;
using System.Linq;
using System.Text;

namespace DotNetExport.dnlib.DotNet.Writer
{
    /// <summary>
    /// Import directory chunk
    /// </summary>
    public sealed class ImportDirectory : IChunk {

        public sealed class ImportAddressTable : IChunk
        {
            readonly bool is64bit;
            FileOffset offset;
            RVA rva;
            private ImportDirectory dir;
            public FileOffset FileOffset => offset;
            public RVA RVA => rva;
            private byte[] table;

            public ImportAddressTable(ImportDirectory dir, bool is64bit)
            {
                this.dir = dir;
                this.is64bit = is64bit;
            }

            public void SetList(List<uint> list)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                    foreach (var u in list)
                    {
                        if (is64bit)
                            writer.Write((ulong)u);
                        else
                            writer.Write(u);
                    }

                    table = memoryStream.ToArray();
                }
            }

            public void SetOffset(FileOffset offset, RVA rva)
            {
                this.offset = offset;
                this.rva = rva;
            }

            public uint GetFileLength()
            {
                if (!dir.Enable)
                    return 0;
                return (uint)table.Length;
            }

            public uint GetVirtualSize() => GetFileLength();

            public void WriteTo(DataWriter dataWriter)
            {
                if (!dir.Enable)
                    return;
                dataWriter.WriteBytes(table);
            }
        }

		readonly bool is64bit;
		FileOffset offset;
		RVA rva;
		bool isExeFile;
		uint length;

        public ImportList Imports { get; }
        internal ImportAddressTable ImportAddress { get; }

        /// <summary>
        /// Gets/sets the <see cref="ImportAddressTable"/>
        /// </summary>
        //public ImportAddressTable ImportAddressTable { get; set; }

        /// <summary>
        /// Gets RVA of _CorExeMain/_CorDllMain in the IAT
        /// </summary>
        public RVA IatCorXxxMainRVA => Imports.Find("mscoree.dll", IsExeFile ? "_CorExeMain" : "_CorDllMain") +
                                       (uint) ImportAddress.RVA;

		/// <summary>
		/// Gets/sets a value indicating whether this is a EXE or a DLL file
		/// </summary>
		public bool IsExeFile {
			get => isExeFile;
            set
            {
                isExeFile = value;
				Imports.Remove("mscoree.dll", "_CorExeMain");
                Imports.Remove("mscoree.dll", "_CorDllMain");
				Imports.Add("mscoree.dll", IsExeFile ? "_CorExeMain" : "_CorDllMain");
			}
        }

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

        internal bool Enable { get; set; }

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="is64bit">true if it's a 64-bit PE file, false if it's a 32-bit PE file</param>
		public ImportDirectory(bool is64bit)
        {
            ImportAddress = new ImportAddressTable(this, is64bit);
            Imports = new ImportList(is64bit) {OnSizeChange = () =>
                {
                    length = (uint)BuildImportTable().Length;
                }
            };
            this.is64bit = is64bit;
            IsExeFile = true;
        }

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
            length = (uint)BuildImportTable().Length;

        }

        public byte[] BuildImportTable()
        {
            var pointerSize = is64bit ? 8 : 4;
            var headersSize = Imports.Imports.Count * 20;
            var functionsSize = Imports.Imports.Select(i => i.Value.Count + 1).Sum() * pointerSize;
            var processedFunctions = 0u;
            using (var stream = new MemoryStream())
            {
                stream.SetLength(headersSize + functionsSize + 20);
                stream.Seek(0, SeekOrigin.End);
                var functionPos = headersSize + 20;
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    var list = new List<uint>();
                    for (var i = 0; i < Imports.Imports.Count; i++)
                    {
                        var import = Imports.Imports.ElementAt(i);
                        var relativeNamePointer = functionPos + (i * pointerSize) + (processedFunctions * pointerSize);
                        for (var j = 0; j < import.Value.Count; j++)
                        {
                            var function = import.Value[j];
                            var funcNamePos = stream.Position;
                            writer.Write((ushort)0);
                            writer.Write(Encoding.UTF8.GetBytes(function));
                            stream.Seek(relativeNamePointer + (j * pointerSize), SeekOrigin.Begin);
                            if (is64bit)
                                writer.Write((ulong)funcNamePos + (uint)RVA);
                            else
                                writer.Write((uint)funcNamePos + (uint)RVA);
                            list.Add((uint)funcNamePos + (uint)RVA);
                            stream.Seek(0, SeekOrigin.End);
                        }
                        writer.Write((byte)0);
                        var namePos = stream.Position;
                        writer.Write(Encoding.UTF8.GetBytes(import.Key));
                        writer.Write((byte)0);
                        stream.Seek(i * 20, SeekOrigin.Begin);
                        writer.Write((uint)relativeNamePointer + (uint)RVA);
                        writer.Write(0u);
                        writer.Write(0u);
                        writer.Write((uint)namePos + (uint)RVA);
                        writer.Write(processedFunctions * pointerSize + (uint)ImportAddress.RVA);
                        stream.Seek(0, SeekOrigin.End);
                        processedFunctions += (uint)import.Value.Count;
                    }
                    writer.Write((byte)0);
                    ImportAddress.SetList(list);
                }

                return stream.ToArray();
            }
        }

        /// <inheritdoc/>
        public uint GetFileLength() {
			if (!Enable)
				return 0;
			return length;
		}

        /// <inheritdoc/>
        public uint GetVirtualSize() => GetFileLength();

        public uint GetTableSize()
        {
            return (uint) (Imports.Imports.Count * 20) + 20;
        }

        /// <inheritdoc/>
        public void WriteTo(DataWriter dataWriter) {
			if (!Enable)
				return;
			dataWriter.WriteBytes(BuildImportTable());
		}
	}
}
