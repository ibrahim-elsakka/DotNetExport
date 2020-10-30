// dnlib: See LICENSE.txt for more info

using DotNetExport.dnlib.IO;
using DotNetExport.dnlib.PE;

namespace DotNetExport.dnlib.DotNet.Writer {
	/// <summary>
	/// Strong name signature chunk
	/// </summary>
	public sealed class StrongNameSignature : IReuseChunk {
		FileOffset offset;
		RVA rva;
		int size;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="size">Size of strong name signature</param>
		public StrongNameSignature(int size) => this.size = size;

		bool IReuseChunk.CanReuse(RVA origRva, uint origSize) => (uint)size <= origSize;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => (uint)size;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter dataWriter) => dataWriter.WriteZeroes(size);
	}
}
