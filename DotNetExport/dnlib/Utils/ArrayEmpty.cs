// dnlib: See LICENSE.txt for more info

// System namespace so it can easily be replaced with Array.Empty<T> later
namespace DotNetExport.dnlib.Utils {
	static class Array2 {
		public static T[] Empty<T>() => EmptyClass<T>.Empty;

		static class EmptyClass<T> {
			public static readonly T[] Empty = new T[0];
		}
	}
}
