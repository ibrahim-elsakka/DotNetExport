// dnlib: See LICENSE.txt for more info

namespace DotNetExport.dnlib {
	/// <summary>
	/// dnlib settings
	/// </summary>
	public static class Settings {
		/// <summary>
		/// <c>true</c> if dnlib is thread safe. (<c>THREAD_SAFE</c> was defined during compilation)
		/// </summary>
		public static bool IsThreadSafe {
			get {
#if THREAD_SAFE
				return true;
#else
				return false;
#endif
			}
		}
	}
}
