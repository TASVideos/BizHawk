using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static BizHawk.Emulation.Cores.Nintendo.Gameboy.Gameboy;
using static BizHawk.Emulation.Cores.Nintendo.GBHawk.GBHawk;

namespace BHTest.Integration.TestRoms
{
	public static class GBHelper
	{
		public enum ConsoleVariant { CGB_C, CGB_D, DMG, DMG_B }

		public readonly struct CoreSetup
		{
			public static IReadOnlyCollection<CoreSetup> ValidSetupsFor(ConsoleVariant variant)
				=> new CoreSetup[] { new(CoreNames.Gambatte, variant), new(CoreNames.Gambatte, variant, useBios: false), new(CoreNames.GbHawk, variant) };

			public readonly string CoreName;

			public readonly bool UseBIOS;

			public readonly ConsoleVariant Variant;

			public CoreSetup(string coreName, ConsoleVariant variant, bool useBios = true)
			{
				CoreName = coreName;
				UseBIOS = useBios;
				Variant = variant;
			}

			public override readonly string ToString() => $"{Variant} in {CoreName}{(UseBIOS ? string.Empty : " (no BIOS)")}";
		}

		private static readonly GambatteSettings GambatteSettings = new() { CGBColors = GBColors.ColorType.vivid };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GB_NOBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GB, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GB_USEBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GB, EnableBIOS = true, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GBC_NOBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GBC, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GBC_USEBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GBC, EnableBIOS = true, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GBSyncSettings GBHawkSyncSettings_GB = new() { ConsoleMode = GBSyncSettings.ConsoleModeType.GB };

		private static readonly GBSyncSettings GBHawkSyncSettings_GBC = new() { ConsoleMode = GBSyncSettings.ConsoleModeType.GBC };

		public static readonly IReadOnlyDictionary<int, int> MattCurriePaletteMap = new Dictionary<int, int>
		{
			[0x0F3EAA] = 0x0000FF,
			[0x137213] = 0x009C00,
			[0x187890] = 0x0063C6,
			[0x695423] = 0x737300,
			[0x7BC8D5] = 0x6BBDFF,
			[0x7F3848] = 0x943939,
			[0x83C656] = 0x7BFF31,
			[0x9D7E34] = 0xADAD00,
			[0xE18096] = 0xFF8484,
			[0xE8BA4D] = 0xFFFF00,
			[0xF8F8F8] = 0xFFFFFF,
		};

		public static readonly IReadOnlyDictionary<int, int> UnVividPaletteMap = new Dictionary<int, int>
		{
			[0x0063C5] = 0x0063C6,
			[0x00CE00] = 0x199619,
			[0x089C84] = 0x21926C,
			[0x424242] = 0x404040,
			[0x52AD52] = 0x5B925B,
			[0x943A3A] = 0x943939,
			[0xA5A5A5] = 0xA0A0A0,
			[0xAD52AD] = 0x9D669D,
			[0xFFFFFF] = 0xF8F8F8,
		};

		private static bool AddEmbeddedGBBIOS(this DummyFrontend.EmbeddedFirmwareProvider efp, ConsoleVariant variant)
			=> variant.IsColour()
				? efp.AddIfExists(new("GBC", "World"), false ? "res.fw.GBC__World__AGB.bin" : "res.fw.GBC__World__CGB.bin")
				: efp.AddIfExists(new("GB", "World"), "res.fw.GB__World__DMG.bin");

		public static GambatteSyncSettings GetGambatteSyncSettings(ConsoleVariant variant, bool biosAvailable)
			=> biosAvailable
				? variant.IsColour()
					? GambatteSyncSettings_GBC_USEBIOS
					: GambatteSyncSettings_GB_USEBIOS
				: variant.IsColour()
					? GambatteSyncSettings_GBC_NOBIOS
					: GambatteSyncSettings_GB_NOBIOS;

		public static GBSyncSettings GetGBHawkSyncSettings(ConsoleVariant variant)
			=> variant.IsColour()
				? GBHawkSyncSettings_GBC
				: GBHawkSyncSettings_GB;

		public static DummyFrontend.ClassInitCallbackDelegate InitGBCore(CoreSetup setup, string romFilename, byte[] rom)
			=> (efp, _, coreComm) =>
			{
				if (setup.UseBIOS && !efp.AddEmbeddedGBBIOS(setup.Variant)) Assert.Inconclusive("BIOS not provided");
				var game = Database.GetGameInfo(rom, romFilename);
				IEmulator newCore = setup.CoreName switch
				{
					CoreNames.Gambatte => new Gameboy(coreComm, game, rom, GambatteSettings, GetGambatteSyncSettings(setup.Variant, setup.UseBIOS), deterministic: true),
					CoreNames.GbHawk => new GBHawk(coreComm, game, rom, new(), GetGBHawkSyncSettings(setup.Variant)),
					_ => throw new Exception()
				};
				var biosWaitDuration = setup.UseBIOS
					? setup.Variant.IsColour()
						? 186
						: 334
					: 0;
				return (newCore, biosWaitDuration);
			};

		public static bool IsColour(this ConsoleVariant variant)
			=> variant is ConsoleVariant.CGB_C or ConsoleVariant.CGB_D;

		/// <summary>converts Gambatte's GBC palette to GBHawk's; GB palette is the same</summary>
		public static Image NormaliseGBScreenshot(Image img, CoreSetup setup)
			=> setup.Variant.IsColour() && setup.CoreName is CoreNames.Gambatte
				? ImageUtils.PaletteSwap(img, UnVividPaletteMap)
				: img;

		public static TestUtils.TestSuccessState GBScreenshotsEqual(
			Stream expectFile,
			Image? actualUnnormalised,
			bool expectingNotEqual,
			CoreSetup setup,
			(string Suite, string Case) id,
			IReadOnlyDictionary<int, int>? extraPaletteMap = null)
		{
			if (actualUnnormalised is null)
			{
				Assert.Fail("actual screenshot was null");
				return TestUtils.TestSuccessState.Failure; // never hit
			}
			var actual = NormaliseGBScreenshot(actualUnnormalised, setup);
//			ImageUtils.PrintPalette(Image.FromStream(expectFile), "expected image", actual, "actual image (after normalisation, before extra map)");
			return ImageUtils.ScreenshotsEqualMagickDotNET(
				expectFile,
				extraPaletteMap is null ? actual : ImageUtils.PaletteSwap(actual, extraPaletteMap),
				expectingNotEqual,
				id);
		}

		public static void ShortCircuitGambatte(CoreSetup setup)
		{
			if (OSTailoredCode.IsUnixHost && setup.CoreName is CoreNames.Gambatte) Assert.Inconclusive("Gambatte unavailable on Linux");
		}
	}
}
