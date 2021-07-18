using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Common.IOExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static BHTest.Integration.TestRoms.GBHelper;

namespace BHTest.Integration.TestRoms
{
	[TestClass]
	public sealed class MealybugTearoomTests
	{
		public readonly struct MealybugTestCase
		{
			public static readonly IReadOnlyCollection<string> KnownFailures = new[]
			{
				"m3_bgp_change on CGB_C in Gambatte", // Gambatte's GBC emulation matches CGB D variant
				"m3_bgp_change on CGB_C in Gambatte (no BIOS)", // Gambatte's GBC emulation matches CGB D variant
				"m3_bgp_change on CGB_C in GBHawk",
				"m3_bgp_change on CGB_D in GBHawk",
				"m3_bgp_change on DMG in Gambatte",
				"m3_bgp_change on DMG in Gambatte (no BIOS)",
				"m3_bgp_change on DMG in GBHawk",
				"m3_bgp_change_sprites on CGB_C in Gambatte", // Gambatte's GBC emulation matches CGB D variant
				"m3_bgp_change_sprites on CGB_C in Gambatte (no BIOS)", // Gambatte's GBC emulation matches CGB D variant
				"m3_bgp_change_sprites on CGB_C in GBHawk",
				"m3_bgp_change_sprites on CGB_D in GBHawk",
				"m3_bgp_change_sprites on DMG in Gambatte",
				"m3_bgp_change_sprites on DMG in Gambatte (no BIOS)",
				"m3_bgp_change_sprites on DMG in GBHawk",
				"m3_lcdc_bg_en_change on DMG in Gambatte",
				"m3_lcdc_bg_en_change on DMG in Gambatte (no BIOS)",
				"m3_lcdc_bg_en_change on DMG in GBHawk",
				"m3_lcdc_bg_en_change on DMG_B in Gambatte",
				"m3_lcdc_bg_en_change on DMG_B in Gambatte (no BIOS)",
				"m3_lcdc_bg_en_change on DMG_B in GBHawk",
				"m3_lcdc_bg_map_change on CGB_C in GBHawk",
				"m3_lcdc_bg_map_change on DMG in GBHawk",
				"m3_lcdc_bg_map_change2 on CGB_C in GBHawk",
				"m3_lcdc_obj_en_change on DMG in Gambatte",
				"m3_lcdc_obj_en_change on DMG in Gambatte (no BIOS)",
				"m3_lcdc_obj_en_change on DMG in GBHawk",
				"m3_lcdc_obj_en_change_variant on CGB_C in Gambatte", // Gambatte's GBC emulation matches CGB D variant
				"m3_lcdc_obj_en_change_variant on CGB_C in Gambatte (no BIOS)", // Gambatte's GBC emulation matches CGB D variant
				"m3_lcdc_obj_en_change_variant on CGB_C in GBHawk",
				"m3_lcdc_obj_en_change_variant on CGB_D in GBHawk",
				"m3_lcdc_obj_en_change_variant on DMG in Gambatte",
				"m3_lcdc_obj_en_change_variant on DMG in Gambatte (no BIOS)",
				"m3_lcdc_obj_en_change_variant on DMG in GBHawk",
				"m3_lcdc_obj_size_change on CGB_C in GBHawk",
				"m3_lcdc_obj_size_change on DMG in Gambatte",
				"m3_lcdc_obj_size_change on DMG in Gambatte (no BIOS)",
				"m3_lcdc_obj_size_change on DMG in GBHawk",
				"m3_lcdc_obj_size_change_scx on CGB_C in GBHawk",
				"m3_lcdc_obj_size_change_scx on DMG in Gambatte",
				"m3_lcdc_obj_size_change_scx on DMG in Gambatte (no BIOS)",
				"m3_lcdc_obj_size_change_scx on DMG in GBHawk",
				"m3_lcdc_tile_sel_change on CGB_C in Gambatte",
				"m3_lcdc_tile_sel_change on CGB_C in Gambatte (no BIOS)",
				"m3_lcdc_tile_sel_change on CGB_C in GBHawk",
				"m3_lcdc_tile_sel_change on DMG in GBHawk",
				"m3_lcdc_tile_sel_change2 on CGB_C in Gambatte",
				"m3_lcdc_tile_sel_change2 on CGB_C in Gambatte (no BIOS)",
				"m3_lcdc_tile_sel_change2 on CGB_C in GBHawk",
				"m3_lcdc_tile_sel_win_change on CGB_C in Gambatte",
				"m3_lcdc_tile_sel_win_change on CGB_C in Gambatte (no BIOS)",
				"m3_lcdc_tile_sel_win_change on CGB_C in GBHawk",
				"m3_lcdc_tile_sel_win_change on DMG in GBHawk",
				"m3_lcdc_tile_sel_win_change2 on CGB_C in Gambatte",
				"m3_lcdc_tile_sel_win_change2 on CGB_C in Gambatte (no BIOS)",
				"m3_lcdc_tile_sel_win_change2 on CGB_C in GBHawk",
				"m3_lcdc_win_en_change_multiple on CGB_C in GBHawk",
				"m3_lcdc_win_en_change_multiple on DMG in GBHawk",
				"m3_lcdc_win_en_change_multiple_wx on DMG in Gambatte",
				"m3_lcdc_win_en_change_multiple_wx on DMG in Gambatte (no BIOS)",
				"m3_lcdc_win_en_change_multiple_wx on DMG in GBHawk",
				"m3_lcdc_win_en_change_multiple_wx on DMG_B in Gambatte",
				"m3_lcdc_win_en_change_multiple_wx on DMG_B in Gambatte (no BIOS)",
				"m3_lcdc_win_en_change_multiple_wx on DMG_B in GBHawk",
				"m3_lcdc_win_map_change on CGB_C in GBHawk",
				"m3_lcdc_win_map_change on DMG in GBHawk",
				"m3_lcdc_win_map_change2 on CGB_C in GBHawk",
				"m3_obp0_change on CGB_C in Gambatte", // Gambatte's GBC emulation matches CGB D variant
				"m3_obp0_change on CGB_C in Gambatte (no BIOS)", // Gambatte's GBC emulation matches CGB D variant
				"m3_obp0_change on CGB_C in GBHawk",
				"m3_obp0_change on CGB_D in GBHawk",
				"m3_obp0_change on DMG in GBHawk",
				"m3_scx_high_5_bits on CGB_C in GBHawk",
				"m3_scx_high_5_bits on DMG in GBHawk",
				"m3_scx_high_5_bits_change2 on CGB_C in GBHawk",
				"m3_scy_change on CGB_C in GBHawk",
				"m3_scy_change on CGB_D in Gambatte", // Gambatte's GBC emulation matches CGB C variant
				"m3_scy_change on CGB_D in Gambatte (no BIOS)", // Gambatte's GBC emulation matches CGB C variant
				"m3_scy_change on CGB_D in GBHawk",
				"m3_scy_change on DMG in GBHawk",
				"m3_scy_change2 on CGB_C in GBHawk",
				"m3_window_timing on CGB_C in Gambatte", // Gambatte's GBC emulation matches CGB D variant
				"m3_window_timing on CGB_C in Gambatte (no BIOS)", // Gambatte's GBC emulation matches CGB D variant
				"m3_window_timing on CGB_C in GBHawk",
				"m3_window_timing on CGB_D in GBHawk",
				"m3_window_timing on DMG in GBHawk",
				"m3_window_timing_wx_0 on CGB_C in Gambatte",
				"m3_window_timing_wx_0 on CGB_C in Gambatte (no BIOS)",
				"m3_window_timing_wx_0 on CGB_C in GBHawk",
				"m3_window_timing_wx_0 on CGB_D in Gambatte",
				"m3_window_timing_wx_0 on CGB_D in Gambatte (no BIOS)",
				"m3_window_timing_wx_0 on CGB_D in GBHawk",
				"m3_window_timing_wx_0 on DMG in Gambatte",
				"m3_window_timing_wx_0 on DMG in Gambatte (no BIOS)",
				"m3_window_timing_wx_0 on DMG in GBHawk",
				"m3_wx_4_change on DMG in Gambatte",
				"m3_wx_4_change on DMG in Gambatte (no BIOS)",
				"m3_wx_4_change on DMG in GBHawk",
				"m3_wx_4_change_sprites on CGB_C in Gambatte",
				"m3_wx_4_change_sprites on CGB_C in Gambatte (no BIOS)",
				"m3_wx_4_change_sprites on CGB_C in GBHawk",
				"m3_wx_4_change_sprites on DMG in Gambatte",
				"m3_wx_4_change_sprites on DMG in Gambatte (no BIOS)",
				"m3_wx_4_change_sprites on DMG in GBHawk",
				"m3_wx_5_change on DMG in Gambatte",
				"m3_wx_5_change on DMG in Gambatte (no BIOS)",
				"m3_wx_5_change on DMG in GBHawk",
				"m3_wx_6_change on DMG in GBHawk",
			};

			public readonly string ExpectEmbedPath;

			public readonly string RomEmbedPath;

			public readonly CoreSetup Setup;

			public readonly string TestName;

			public MealybugTestCase(string testName, CoreSetup setup, string romEmbedPath, string expectEmbedPath)
			{
				TestName = testName;
				Setup = setup;
				RomEmbedPath = romEmbedPath;
				ExpectEmbedPath = expectEmbedPath;
			}

			public readonly string DisplayName() => $"{TestName} on {Setup}";
		}

		[AttributeUsage(AttributeTargets.Method)]
		private sealed class MealybugTestDataAttribute : Attribute, ITestDataSource
		{
			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
			{
				const string SUITE_PREFIX = "res.mealybug_tearoom_tests_artifact.";
				var variants = new[] { ("expected.CPU_CGB_C.", ConsoleVariant.CGB_C), ("expected.CPU_CGB_D.", ConsoleVariant.CGB_D), ("expected.DMG_blob.", ConsoleVariant.DMG), ("expected.DMG_CPU_B.", ConsoleVariant.DMG_B) };
				List<MealybugTestCase> testCases = new();
				foreach (var item in ReflectionCache.EmbeddedResourceList(SUITE_PREFIX).Where(item => item.EndsWith(".png")))
				{
					var (prefix, variant) = variants.First(kvp => item.StartsWith(kvp.Item1));
					var testName = item.RemovePrefix(prefix).RemoveSuffix(".png");
					var romEmbedPath = SUITE_PREFIX + $"build.ppu.{testName}.gb";
					var expectEmbedPath = SUITE_PREFIX + item;
					foreach (var setup in CoreSetup.ValidSetupsFor(variant)) testCases.Add(new(testName, setup, romEmbedPath, expectEmbedPath));
				}
				// expected value is a "no screenshot available" message
				testCases.RemoveAll(testCase =>
					testCase.Setup.Variant is ConsoleVariant.CGB_C or ConsoleVariant.CGB_D
						&& testCase.TestName is "m3_lcdc_win_en_change_multiple_wx" or "m3_wx_4_change" or "m3_wx_5_change" or "m3_wx_6_change");
				// these are identical to CGB_C
				testCases.RemoveAll(testCase =>
					testCase.Setup.Variant is ConsoleVariant.CGB_D
						&& testCase.TestName is "m2_win_en_toggle" or "m3_lcdc_bg_en_change" or "m3_lcdc_bg_map_change" or "m3_lcdc_obj_en_change" or "m3_lcdc_obj_size_change" or "m3_lcdc_obj_size_change_scx" or "m3_lcdc_tile_sel_change" or "m3_lcdc_tile_sel_win_change" or "m3_lcdc_win_en_change_multiple" or "m3_lcdc_win_map_change" or "m3_scx_high_5_bits" or "m3_scx_low_3_bits" or "m3_wx_4_change" or "m3_wx_4_change_sprites" or "m3_wx_5_change" or "m3_wx_6_change");

//				testCases.RemoveAll(testCase => testCase.Setup.Variant is not ConsoleVariant.DMG); // uncomment and modify to run a subset of the test cases...
				testCases.RemoveAll(testCase => TestUtils.ShouldIgnoreCase(SUITE_ID, testCase.DisplayName())); // ...or use the global blocklist in TestUtils
				return testCases.OrderBy(testCase => testCase.DisplayName())
					.Select(testCase => new object?[] { testCase });
			}

			public string GetDisplayName(MethodInfo methodInfo, object?[] data)
				=> $"{methodInfo.Name}({((MealybugTestCase) data[0]!).DisplayName()})";
		}

		private const string SUITE_ID = "Mealybug";

		[ClassInitialize]
		public static void BeforeAll(TestContext ctx) => TestUtils.PrepareDBAndOutput(SUITE_ID);

		[DataTestMethod]
		[MealybugTestData]
		public void RunMealybugTest(MealybugTestCase testCase)
		{
			ShortCircuitGambatte(testCase.Setup);
			var caseStr = testCase.DisplayName();
			var knownFail = TestUtils.IsKnownFailure(caseStr, MealybugTestCase.KnownFailures);
			TestUtils.ShortCircuitKnownFailure(knownFail);
			void ExecTest(DummyFrontend fe)
			{
				if (testCase.Setup.CoreName is CoreNames.Gambatte)
				{
					// without this, exec hook triggers too early and I've decided I don't want to know why ¯\_(ツ)_/¯ --yoshi
					fe.FrameAdvanceBy(5);
					if (testCase.TestName is "m3_lcdc_win_map_change2") fe.FrameAdvance(); // just happens to be an outlier
				}
				var domain = fe.CoreAsMemDomains!.SystemBus!;
				Func<long> derefPC = fe.Core switch
				{
					Gameboy => () => domain.PeekByte((long) fe.CoreAsDebuggable!.GetCpuFlagsAndRegisters()["PC"].Value),
					GBHawk gbHawk => () => domain.PeekByte(gbHawk.cpu.RegPC),
					_ => throw new Exception()
				};
				var finished = false;
				fe.CoreAsDebuggable!.MemoryCallbacks.Add(new MemoryCallback(
					domain.Name,
					MemoryCallbackType.Execute,
					"breakpoint",
					(_, _, _) =>
					{
						if (!finished && derefPC() is 0x40) finished = true;
					},
					address: null, // all addresses
					mask: null));
				Assert.IsTrue(fe.FrameAdvanceUntil(() => finished), "timed out waiting for exec hook");
			}
			var actualUnnormalised = DummyFrontend.RunAndScreenshot(
				InitGBCore(testCase.Setup, $"{testCase.TestName}.gb", ReflectionCache.EmbeddedResourceStream(testCase.RomEmbedPath).ReadAllBytes()),
				ExecTest);
			var state = GBScreenshotsEqual(
				ReflectionCache.EmbeddedResourceStream(testCase.ExpectEmbedPath),
				actualUnnormalised,
				knownFail,
				testCase.Setup,
				(SUITE_ID, caseStr),
				MattCurriePaletteMap);
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
