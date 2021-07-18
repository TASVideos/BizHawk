using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BHTest.Integration.TestRoms
{
	public static class TestUtils
	{
		public enum TestSuccessState { ExpectedFailure, Failure, Success, UnexpectedSuccess }

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern uint SetDllDirectory(string lpPathName);

		private static readonly SortedSet<string> _initialised = new();

		public static bool IsKnownFailure(string caseStr, IReadOnlyCollection<string> knownFailures)
			=> knownFailures is string[] a
				? Array.BinarySearch(a, caseStr) >= 0
				: knownFailures.Contains(caseStr);

		public static void PrepareDBAndOutput(string suiteID)
		{
			if (_initialised.Contains(suiteID)) return;
			if (_initialised.Count == 0)
			{
				Database.InitializeDatabase(Path.Combine(".", "gamedb", "gamedb.txt"), warnForCollisions: false); // runs in the background; required for Database.GetGameInfo calls
				if (!OSTailoredCode.IsUnixHost) SetDllDirectory(Path.Combine("..", "output", "dll")); // on Linux, this is done by the shell script with the env. var. LD_LIBRARY_PATH
			}
			_initialised.Add(suiteID);
			DirectoryInfo di = new(suiteID);
			if (di.Exists) di.Delete(recursive: true);
			di.Create();
		}

		[Conditional("SKIP_KNOWN_FAILURES")] // run with env. var BIZHAWKTEST_RUN_KNOWN_FAILURES=1
		public static void ShortCircuitKnownFailure(bool knownFail)
		{
			if (knownFail) Assert.Inconclusive("short-circuiting this test which is known to fail");
		}

		/// <remarks>programmatically veto any test cases by modifying this method</remarks>
		public static bool ShouldIgnoreCase(string suiteID, string caseStr)
		{
//			if (caseStr.Contains("timing")) return true;
			return false;
		}

		public static TestSuccessState SuccessState(bool didPass, bool shouldNotPass)
			=> shouldNotPass
				? didPass ? TestSuccessState.UnexpectedSuccess : TestSuccessState.ExpectedFailure
				: didPass ? TestSuccessState.Success : TestSuccessState.Failure;
	}
}
