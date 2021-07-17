using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.VB
{
	[PortedCore(CoreNames.VirtualBoyee, "Mednafen Team", portedUrl: "https://mednafen.github.io/releases/")]
	public class VirtualBoyee : WaterboxCore, ISettable<VirtualBoyee.Settings, VirtualBoyee.SyncSettings>
	{
		private readonly LibVirtualBoyee _boyee;

		[CoreConstructor("VB")]
		public VirtualBoyee(CoreComm comm, byte[] rom, Settings settings, SyncSettings syncSettings)
			: base(comm, new Configuration
			{
				DefaultFpsNumerator = 20000000,
				DefaultFpsDenominator = 397824,
				DefaultWidth = 384,
				DefaultHeight = 224,
				MaxWidth = 1024,
				MaxHeight = 1024,
				MaxSamples = 8192,
				SystemId = "VB"
			})
		{
			_settings = settings ?? new Settings();
			_syncSettings = syncSettings ?? new SyncSettings();

			_boyee = PreInit<LibVirtualBoyee>(new WaterboxOptions
			{
				Filename = "vb.wbx",
				SbrkHeapSizeKB = 256,
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 256,
				PlainHeapSizeKB = 256,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			if (!_boyee.Load(rom, rom.Length, LibVirtualBoyee.NativeSyncSettings.FromFrontendSettings(_syncSettings)))
				throw new InvalidOperationException("Core rejected the rom");

			// do a quick hack up for frame zero size
			var tmp = new LibVirtualBoyee.FrameInfo();
			_boyee.PredictFrameSize(tmp);
			BufferWidth = tmp.Width;
			BufferHeight = tmp.Height;

			PostInit();

			_boyee.SetSettings(LibVirtualBoyee.NativeSettings.FromFrontendSettings(_settings));
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (controller.IsPressed("Power"))
				_boyee.HardReset();

			return new LibVirtualBoyee.FrameInfo { Buttons = GetButtons(controller) };
		}

		private LibVirtualBoyee.Buttons GetButtons(IController c)
		{
			var ret = 0;
			var val = 1;
			foreach (var s in CoreButtons)
			{
				if (c.IsPressed(s))
					ret |= val;
				val <<= 1;
			}
			return (LibVirtualBoyee.Buttons)ret;
		}

		private static readonly string[] CoreButtons =
		{
			"A", "B", "R", "L",
			"R_Up", "R_Right",
			"L_Right", "L_Left", "L_Down", "L_Up",
			"Start", "Select", "R_Left", "R_Down"
		};

		private static readonly Dictionary<string, int> _buttonOrdinals = new Dictionary<string, int>
		{
			["L_Up"] = 1,
			["L_Down"] = 2,
			["L_Left"] = 3,
			["L_Right"] = 4,
			["R_Up"] = 5,
			["R_Down"] = 6,
			["R_Left"] = 7,
			["R_Right"] = 8,
			["B"] = 9,
			["A"] = 10,
			["L"] = 11,
			["R"] = 12,
			["Select"] = 13,
			["Start"] = 14
		};

		private static readonly ControllerDefinition VirtualBoyController = new ControllerDefinition
		{
			Name = "VirtualBoy Controller",
			BoolButtons = CoreButtons
				.OrderBy(b => _buttonOrdinals[b])
				.Concat(new[] { "Power" })
				.ToList()
		};

		public override ControllerDefinition ControllerDefinition => VirtualBoyController;

		public class SyncSettings
		{
			[DefaultValue(false)]
			[Description("Reduce input latency.  Works with all known commercial games, may have homebrew issues.")]
			public bool InstantReadHack { get; set; }
			[DefaultValue(false)]
			[Description("Disable parallax for rendering.")]
			public bool DisableParallax { get; set; }

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class Settings
		{
			public enum ThreeDeeModes : int
			{
				Anaglyph = 0,
				CyberScope = 1,
				SideBySide = 2,
				//OverUnder,
				VerticalInterlaced = 4,
				HorizontalInterlaced = 5,
				OnlyLeft = 6,
				OnlyRight = 7
			}

			[DefaultValue(ThreeDeeModes.Anaglyph)]
			[Description("How to display the 3d image.  Use whichever method works with your VR hardware.")]
			public ThreeDeeModes ThreeDeeMode { get; set; }

			[DefaultValue(false)]
			[Description("Swap the left and right views.")]
			public bool SwapViews { get; set; }

			public enum AnaglyphPresets : int
			{
				Custom,
				RedBlue,
				RedCyan,
				RedElectricCyan,
				RedGreen,
				GreenMagneto,
				YellowBlue
			}

			[DefaultValue(AnaglyphPresets.RedBlue)]
			[Description("Color preset for Anaglyph mode.")]
			public AnaglyphPresets AnaglyphPreset { get; set; }

			[DefaultValue(typeof(Color), "Green")]
			[Description("Left anaglyph color.  Ignored unless Preset is Custom.")]
			public Color AnaglyphCustomLeftColor { get; set; }
			[DefaultValue(typeof(Color), "Purple")]
			[Description("Right anaglyph color.  Ignored unless Preset is Custom.")]
			public Color AnaglyphCustomRightColor { get; set; }

			[DefaultValue(typeof(Color), "White")]
			[Description("Display color for all of the non-anaglyph modes.  Real hardware was red, but other colors may be easier on your eyes.")]
			public Color NonAnaglyphColor { get; set; }

			[DefaultValue(1750)]
			[Range(1000, 2000)]
			[Description("LED gamma ramp.  Range of 1000 to 2000")]
			public int LedOnScale { get; set; }

			[DefaultValue(2)]
			[Range(1, 10)]
			public int InterlacePrescale { get; set; }

			[DefaultValue(0)]
			[Range(0, 1024)]
			[Description("How many pixels to put between views in Side By Side mode")]
			public int SideBySideSeparation { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		private Settings _settings;
		private SyncSettings _syncSettings;

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}
	}
}
