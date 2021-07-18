using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Cores;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static BHTest.Integration.TestRoms.GBHelper;

namespace BHTest.Integration.TestRoms
{
	[TestClass]
	public sealed class RTC3Test
	{
		[AttributeUsage(AttributeTargets.Method)]
		private sealed class RTC3TestData : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				var testCases = new[] { ConsoleVariant.CGB_C, ConsoleVariant.DMG }.SelectMany(CoreSetup.ValidSetupsFor).ToList();
//				testCases.RemoveAll(setup => setup.Variant is not ConsoleVariant.DMG); // uncomment and modify to run a subset of the test cases...
				foreach (var subTest in new[] { "basic", "range", "subSecond" })
				{
					testCases.RemoveAll(setup => TestUtils.ShouldIgnoreCase(SUITE_ID, DisplayNameFor(setup, subTest))); // ...or use the global blocklist in TestUtils
				}
				return testCases.OrderBy(setup => setup.ToString())
					.Select(setup => new object?[] { setup });
			}

			public string GetDisplayName(MethodInfo methodInfo, object?[] data)
				=> $"{methodInfo.Name}({(CoreSetup) data[0]!})";
		}

		private const string SUITE_ID = "RTC3Test";

		private static readonly IReadOnlyCollection<string> KnownFailures = new[]
		{
			"",
		};

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx) => TestUtils.PrepareDBAndOutput(SUITE_ID);

		private static string DisplayNameFor(CoreSetup setup, string subTest) => $"RTC3Test.{subTest} on {setup}";

		[DataTestMethod]
		[RTC3TestData]
		public void RunRTC3Test(CoreSetup setup)
		{
#if SKIP_KNOWN_FAILURES
			if (new[] { "basic", "range", "subSecond" }.Select(subTest => DisplayNameFor(setup, subTest)).All(KnownFailures.Contains))
			{
				Assert.Inconclusive("short-circuiting this test which is known to fail");
			}
#endif
			ShortCircuitGambatte(setup);
			DummyFrontend fe = new(InitGBCore(setup, "rtc3test.gb", ReflectionCache.EmbeddedResourceStream("res.rtc3test_artifact.rtc3test.gb").ReadAllBytes()));
			bool DoSubcaseAssertion(string subTest, Bitmap actualUnnormalised)
			{
				var caseStr = DisplayNameFor(setup, subTest);
				var knownFail = TestUtils.IsKnownFailure(caseStr, KnownFailures);
				var state = GBScreenshotsEqual(
					ReflectionCache.EmbeddedResourceStream($"res.rtc3test_artifact.expected_{subTest.ToLowerInvariant()}_{(setup.Variant.IsColour() ? "cgb" : "dmg")}.png"),
					actualUnnormalised,
					knownFail,
					setup,
					(SUITE_ID, caseStr));
				switch (state)
				{
					case TestUtils.TestSuccessState.ExpectedFailure:
						Console.WriteLine("expected failure, verified");
						break;
					case TestUtils.TestSuccessState.Failure:
						Assert.Fail("expected and actual screenshots differ");
						break;
					case TestUtils.TestSuccessState.Success:
						return true;
					case TestUtils.TestSuccessState.UnexpectedSuccess:
						Assert.Fail("expected and actual screenshots matched unexpectedly (this is a good thing)");
						break;
				}
				return false;
			}
			var (buttonA, buttonDown) = setup.CoreName is CoreNames.Gambatte ? ("A", "Down") : ("P1 A", "P1 Down");
			fe.FrameAdvanceBy(6);
			fe.SetButton(buttonA);
//			fe.FrameAdvanceBy(setup.Variant.IsColour() ? 676 : 648);
			fe.FrameAdvanceBy(685);
			var basicPassed = DoSubcaseAssertion("basic", fe.Screenshot());
#if true
			fe.Dispose();
			if (!basicPassed) Assert.Inconclusive(); // for this to be false, it must have been an expected failure or execution would have stopped with an Assert.Fail call
			Assert.Inconclusive("(other subtests aren't implemented)");
#else // screenshot seems to freeze emulation, or at least rendering
			fe.SetButton(buttonA);
			fe.FrameAdvanceBy(3);
			fe.SetButton(buttonDown);
			fe.FrameAdvanceBy(2);
			fe.SetButton(buttonA);
			fe.FrameAdvanceBy(429);
			var rangePassed = DoSubcaseAssertion("range", fe.Screenshot());
			fe.SetButton(buttonA);
			// didn't bother TASing the remaining menu navigation because it doesn't work
//			var subSecondPassed = DoSubcaseAssertion("subSecond", fe.Screenshot());
			fe.Dispose();
			if (!(basicPassed && rangePassed /*&& subSecondPassed*/)) Assert.Inconclusive(); // for one of these to be false, it must have been an expected failure or execution would have stopped with an Assert.Fail call
#endif
		}
	}
}
