// dnlib: See LICENSE.txt for more info

using DotNetExport.dnlib.DotNet.MD;
using DotNetExport.dnlib.IO;
using DotNetExport.dnlib.PE;

namespace DotNetExport.dnlib.DotNet.Writer {
	/// <summary>
	/// Options to <see cref="ImageCor20Header"/>
	/// </summary>
	public sealed class Cor20HeaderOptions {
		/// <summary>
		/// Default major runtime version
		/// </summary>
		public const ushort DEFAULT_MAJOR_RT_VER = 2;

		/// <summary>
		/// Default minor runtime version
		/// </summary>
		public const ushort DEFAULT_MINOR_RT_VER = 5;

		/// <summary>
		/// Major runtime version
		/// </summary>
		public ushort? MajorRuntimeVersion;

		/// <summary>
		/// Minor runtime version
		/// </summary>
		public ushort? MinorRuntimeVersion;

		/// <summary>
		/// Flags
		/// </summary>
		public ComImageFlags? Flags;

		/// <summary>
		/// Entry point or <c>null</c>. Either a Method/File token or an RVA.
		/// </summary>
		public uint? EntryPoint;

		/// <summary>
		/// Default constructor
		/// </summary>
		public Cor20HeaderOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public Cor20HeaderOptions(ComImageFlags flags) => Flags = flags;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="major">Major runtime version (default is <see cref="DEFAULT_MAJOR_RT_VER"/>)</param>
		/// <param name="minor">Minor runtime version (default is <see cref="DEFAULT_MINOR_RT_VER"/>)</param>
		/// <param name="flags">Flags</param>
		public Cor20HeaderOptions(ushort major, ushort minor, ComImageFlags flags) {
			MajorRuntimeVersion = major;
			MinorRuntimeVersion = minor;
			Flags = flags;
		}
	}

	/// <summary>
	/// .NET header
	/// </summary>
	public sealed class ImageCor20Header : IChunk {
		FileOffset offset;
		RVA rva;
		Cor20HeaderOptions options;

		/// <summary>
		/// Gets/sets the <see cref="Metadata"/>
		/// </summary>
		public Metadata Metadata { get; set; }

		/// <summary>
		/// Gets/sets the .NET resources
		/// </summary>
		public NetResources NetResources { get; set; }

		/// <summary>
		/// Gets/sets the strong name signature
		/// </summary>
		public StrongNameSignature StrongNameSignature { get; set; }

		internal IChunk VtableFixups { get; set; }

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public ImageCor20Header(Cor20HeaderOptions options) => this.options = options;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => 0x48;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter dataWriter) {
			dataWriter.WriteInt32(0x48);	// cb
			dataWriter.WriteUInt16(options.MajorRuntimeVersion ?? Cor20HeaderOptions.DEFAULT_MAJOR_RT_VER);
			dataWriter.WriteUInt16(options.MinorRuntimeVersion ?? Cor20HeaderOptions.DEFAULT_MINOR_RT_VER);
			dataWriter.WriteDataDirectory(Metadata);
			dataWriter.WriteUInt32((uint)(options.Flags ?? ComImageFlags.ILOnly));
			dataWriter.WriteUInt32(options.EntryPoint ?? 0);
			dataWriter.WriteDataDirectory(NetResources);
			dataWriter.WriteDataDirectory(StrongNameSignature);
			dataWriter.WriteDataDirectory(null);	// Code manager table
			dataWriter.WriteDataDirectory(VtableFixups);
			dataWriter.WriteDataDirectory(null);	// Export address table jumps
			dataWriter.WriteDataDirectory(null);	// Managed native header
		}
	}
}
