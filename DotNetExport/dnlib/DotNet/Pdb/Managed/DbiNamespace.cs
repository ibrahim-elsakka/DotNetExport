// dnlib: See LICENSE.txt for more info

using DotNetExport.dnlib.DotNet.Pdb.Symbols;

namespace DotNetExport.dnlib.DotNet.Pdb.Managed {
	sealed class DbiNamespace : SymbolNamespace {
		public override string Name => name;
		readonly string name;

		public DbiNamespace(string ns) => name = ns;
	}
}
