using Microsoft.VisualStudio.TestTools.UnitTesting;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Emulation.Common
{
	[TestClass]
	public class FirmwareDatabaseTests
	{
		private static FirmwareDatabase DB => FirmwareDatabase.Instance;

		[TestMethod]
		public void CheckFilesInOptions()
		{
			foreach (var fo in DB.FirmwareOptions) Assert.IsTrue(DB.FirmwareFilesByHash.ContainsKey(fo.Hash), $"option {fo.ID} references unknown file {fo.Hash}");
		}

		[TestMethod]
		public void CheckFormatOfHashes()
		{
			static void CustomAssert(string hash)
				=> Assert.IsTrue(hash.Length == 40 && hash == hash.ToUpperInvariant() && hash.IsHex(), $"incorrectly formatted: {hash}");
			foreach (var hash in DB.FirmwareFilesByHash.Keys) CustomAssert(hash);
			foreach (var fo in DB.FirmwareOptions) CustomAssert(fo.Hash);
		}
	}
}
