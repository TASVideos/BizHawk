using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Common.IOExtensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static BHTest.Integration.TestRoms.GBHelper;

namespace BHTest.Integration.TestRoms
{
	[TestClass]
	public sealed class BullyGB
	{
		[AttributeUsage(AttributeTargets.Method)]
		private sealed class BullyTestData : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				var testCases = new[] { ConsoleVariant.CGB_C, ConsoleVariant.DMG }.SelectMany(CoreSetup.ValidSetupsFor).ToList();
//				testCases.RemoveAll(setup => setup.Variant is not ConsoleVariant.DMG); // uncomment and modify to run a subset of the test cases...
				testCases.RemoveAll(setup => TestUtils.ShouldIgnoreCase(SUITE_ID, DisplayNameFor(setup))); // ...or use the global blocklist in TestUtils
				return testCases.OrderBy(setup => setup.ToString())
					.Select(setup => new object?[] { setup });
			}

			public string GetDisplayName(MethodInfo methodInfo, object?[] data)
				=> $"{methodInfo.Name}({(CoreSetup) data[0]!})";
		}

		private const string SUITE_ID = "BullyGB";

		private static readonly IReadOnlyCollection<string> KnownFailures = new[]
		{
			"BullyGB on CGB_C in GBHawk",
		};

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx) => TestUtils.PrepareDBAndOutput(SUITE_ID);

		private static string DisplayNameFor(CoreSetup setup) => $"BullyGB on {setup}";

		[BullyTestData]
		[DataTestMethod]
		public void RunBullyTest(CoreSetup setup)
		{
			ShortCircuitGambatte(setup);
			var caseStr = DisplayNameFor(setup);
			var knownFail = TestUtils.IsKnownFailure(caseStr, KnownFailures);
			TestUtils.ShortCircuitKnownFailure(knownFail);
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(setup, "bully.gbc", ReflectionCache.EmbeddedResourceStream("res.BullyGB_artifact.bully.gb").ReadAllBytes()),
				fe => fe.FrameAdvanceBy(18));
			var state = GBScreenshotsEqual(
				ReflectionCache.EmbeddedResourceStream($"res.BullyGB_artifact.expected_{(setup.Variant.IsColour() ? "cgb" : "dmg")}.png"),
				actualUnnormalised,
				knownFail,
				setup,
				(SUITE_ID, caseStr));
			switch (state)
			{
				case TestUtils.TestSuccessState.ExpectedFailure:
					Assert.Inconclusive("expected failure, verified");
					break;
				case TestUtils.TestSuccessState.Failure:
					Assert.Fail("expected and actual screenshots differ");
					break;
				case TestUtils.TestSuccessState.UnexpectedSuccess:
					Assert.Fail("expected and actual screenshots matched unexpectedly (this is a good thing)");
					break;
			}
		}
	}
}
