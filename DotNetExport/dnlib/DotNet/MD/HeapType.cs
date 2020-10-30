// dnlib: See LICENSE.txt for more info

namespace DotNetExport.dnlib.DotNet.MD {
	/// <summary>
	/// Heap type. The values are set in stone by MS. Don't change.
	/// </summary>
	public enum HeapType : uint {
		/// <summary>#Strings heap</summary>
		Strings	= 0,
		/// <summary>#GUID heap</summary>
		Guid	= 1,
		/// <summary>#Blob heap</summary>
		Blob	= 2,
		/// <summary>#US heap</summary>
		US		= 3,
	}
}
