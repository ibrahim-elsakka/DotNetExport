// dnlib: See LICENSE.txt for more info

namespace DotNetExport.dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB file kind
	/// </summary>
	public enum PdbFileKind {
		/// <summary>
		/// Windows PDB
		/// </summary>
		WindowsPDB,

		/// <summary>
		/// Portable PDB
		/// </summary>
		PortablePDB,

		/// <summary>
		/// Embedded portable PDB
		/// </summary>
		EmbeddedPortablePDB,
	}
}
