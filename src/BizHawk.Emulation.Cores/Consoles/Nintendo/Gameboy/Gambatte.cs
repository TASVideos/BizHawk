using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	[PortedCore(CoreNames.Gambatte, "", "Gambatte-Speedrun r717+", "https://github.com/pokemon-speedrunning/gambatte-speedrun")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class Gameboy : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable, ICodeDataLogger,
		IBoardInfo, IRomInfo, IDebuggable, ISettable<Gameboy.GambatteSettings, Gameboy.GambatteSyncSettings>,
		IGameboyCommon, ICycleTiming, ILinkable
	{
		[CoreConstructor("GB")]
		[CoreConstructor("GBC")]
		public Gameboy(CoreComm comm, GameInfo game, byte[] file, Gameboy.GambatteSettings settings, Gameboy.GambatteSyncSettings syncSettings, bool deterministic)
		{
			var ser = new BasicServiceProvider(this);
			ser.Register<IDisassemblable>(_disassembler);
			ServiceProvider = ser;
			Tracer = new TraceBuffer
			{
				Header = "LR35902: PC, opcode, registers (A, F, B, C, D, E, H, L, LY, SP, CY)"
			};
			ser.Register<ITraceable>(Tracer);
			InitMemoryCallbacks();

			ThrowExceptionForBadRom(file);
			BoardName = MapperName(file);

			DeterministicEmulation = deterministic;

			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_create)}() returned null???");
			}

			Console.WriteLine(game.System);

			try
			{
				_syncSettings = syncSettings ?? new GambatteSyncSettings();

				LibGambatte.LoadFlags flags = 0;

				switch (_syncSettings.ConsoleMode)
				{
					case GambatteSyncSettings.ConsoleModeType.GB:
						break;
					case GambatteSyncSettings.ConsoleModeType.GBC:
						flags |= LibGambatte.LoadFlags.CGB_MODE;
						break;
					case GambatteSyncSettings.ConsoleModeType.GBA:
						flags |= LibGambatte.LoadFlags.CGB_MODE | LibGambatte.LoadFlags.GBA_FLAG;
						break;
					default:
						if (game.System == "GBC")
							flags |= LibGambatte.LoadFlags.CGB_MODE;
						break;
				}

				if (_syncSettings.MulticartCompat)
				{
					flags |= LibGambatte.LoadFlags.MULTICART_COMPAT;
				}

				byte[] bios;
				string biosSystemId;
				string biosId;
				if ((flags & LibGambatte.LoadFlags.CGB_MODE) == LibGambatte.LoadFlags.CGB_MODE)
				{
					biosSystemId = "GBC";
					biosId = _syncSettings.ConsoleMode == GambatteSyncSettings.ConsoleModeType.GBA ? "AGB" : "World";
					IsCgb = true;
				}
				else
				{
					biosSystemId = "GB";
					biosId = "World";
					IsCgb = false;
				}

				if (_syncSettings.EnableBIOS)
				{
					bios = comm.CoreFileProvider.GetFirmware(biosSystemId, biosId, true, "BIOS Not Found, Cannot Load.  Change SyncSettings to run without BIOS.");
					if (LibGambatte.gambatte_loadbios(GambatteState, bios, (uint)bios.Length) != 0)
					{
						throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbios)}() returned non-zero (bios error)");
					}
				}
				else
				{
					if (DeterministicEmulation) // throw a warning if a movie is being recorded with the bios disabled
					{
						comm.ShowMessage("Detected disabled BIOS during movie recording. It is recommended to use a BIOS for movie recording. Change Sync Settings to run with a BIOS.");
					}
					flags |= LibGambatte.LoadFlags.NO_BIOS;
				}

				if (LibGambatte.gambatte_load(GambatteState, file, (uint)file.Length, flags) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_load)}() returned non-zero (is this not a gb or gbc rom?)");
				}

				// set real default colors (before anyone mucks with them at all)
				PutSettings((GambatteSettings)settings ?? new GambatteSettings());

				InitSound();

				Frame = 0;
				LagCount = 0;
				IsLagFrame = false;

				InputCallback = new LibGambatte.InputGetter(ControllerCallback);

				LibGambatte.gambatte_setinputgetter(GambatteState, InputCallback);

				InitMemoryDomains();

				RomDetails = $"{game.Name}\r\nSHA1:{file.HashSHA1()}\r\nMD5:{file.HashMD5()}\r\n";

				byte[] buff = new byte[32];
				LibGambatte.gambatte_romtitle(GambatteState, buff);
				string romname = System.Text.Encoding.ASCII.GetString(buff);
				Console.WriteLine("Core reported rom name: {0}", romname);

				if (!_syncSettings.EnableBIOS && IsCgb && IsCGBDMGMode()) // without a bios, we need to set the palette for cgbdmg ourselves
				{
					int[] cgbDmgColors = new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					if (file[0x14B] == 0x01 || (file[0x14B] == 0x33 && file[0x144] == '0' && file[0x145] == '1')) // Nintendo licencees get special palettes
					{
						cgbDmgColors = ColorsFromTitleHash(file);
					}
					ChangeDMGColors(cgbDmgColors);
				}

				if (!DeterministicEmulation && _syncSettings.RealTimeRTC)
				{
					LibGambatte.gambatte_settimemode(GambatteState, false);
				}

				if (DeterministicEmulation)
				{
					int[] rtcRegs = new int[11];
					rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh] = 0;
					if (_syncSettings.InternalRTCOverflow)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh] |= 0x80;
					}
					if (_syncSettings.InternalRTCHalt)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh] |= 0x40;
					}
					rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh] |= _syncSettings.InternalRTCDays >> 8;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.Dl] = _syncSettings.InternalRTCDays & 0xFF;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.H] = (_syncSettings.InternalRTCHours < 0) ? (_syncSettings.InternalRTCHours + 0x20) : _syncSettings.InternalRTCHours;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.M] = (_syncSettings.InternalRTCMinutes < 0) ? (_syncSettings.InternalRTCMinutes + 0x40) : _syncSettings.InternalRTCMinutes;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.S] = (_syncSettings.InternalRTCSeconds < 0) ? (_syncSettings.InternalRTCSeconds + 0x40) : _syncSettings.InternalRTCSeconds;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.C] = _syncSettings.InternalRTCCycles;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh_L] = 0;
					if (_syncSettings.LatchedRTCOverflow)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh_L] |= 0x80;
					}
					if (_syncSettings.LatchedRTCHalt)
					{
						rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh_L] |= 0x40;
					}
					rtcRegs[(int)LibGambatte.RtcRegIndicies.Dh_L] |= _syncSettings.LatchedRTCDays >> 8;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.Dl_L] = _syncSettings.LatchedRTCDays & 0xFF;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.H_L] = _syncSettings.LatchedRTCHours;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.M_L] = _syncSettings.LatchedRTCMinutes;
					rtcRegs[(int)LibGambatte.RtcRegIndicies.S_L] = _syncSettings.LatchedRTCSeconds;
					LibGambatte.gambatte_setrtcregs(GambatteState, rtcRegs);
				}

				LibGambatte.gambatte_setrtcdivisoroffset(GambatteState, _syncSettings.RTCDivisorOffset);

				LibGambatte.gambatte_setcartbuspulluptime(GambatteState, _syncSettings.CartBusPullUpTime);

				_cdCallback = new LibGambatte.CDCallback(CDCallbackProc);

				NewSaveCoreSetBuff();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private readonly GBDisassembler _disassembler = new();

		public string RomDetails { get; }

		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const uint TICKSINFRAME = 35112;

		/// <summary>
		/// number of ticks per second
		/// </summary>
		private const uint TICKSPERSECOND = 2097152;

		/// <summary>
		/// keep a copy of the input callback delegate so it doesn't get GCed
		/// </summary>
		private LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		private LibGambatte.Buttons CurrentButtons = 0;

		/// <summary>
		/// internal gambatte state
		/// </summary>
		internal IntPtr GambatteState { get; private set; } = IntPtr.Zero;

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public bool IsCgb { get; set; }

		// all cycle counts are relative to a 2*1024*1024 mhz refclock

		/// <summary>
		/// total cycles actually executed
		/// </summary>
		private ulong _cycleCount = 0;
		private ulong callbackCycleCount = 0;

		/// <summary>
		/// number of extra cycles we overran in the last frame
		/// </summary>
		private uint frameOverflow = 0;
		public long CycleCount => (long)_cycleCount;
		public double ClockRate => TICKSPERSECOND;

		public static readonly ControllerDefinition GbController = new ControllerDefinition
		{
			Name = "Gameboy Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power"
			}
		};
		
		public static readonly ControllerDefinition SubGbController = new ControllerDefinition
		{
			Name = "Subframe Gameboy Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power"
			}
		}.AddAxis("Input Length", 0.RangeTo(35112), 35112);

		private LibGambatte.Buttons ControllerCallback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
			return CurrentButtons;
		}

		/// <summary>
		/// true if the emulator is currently emulating CGB
		/// </summary>
		public bool IsCGBMode()
		{
			//return LibGambatte.gambatte_iscgb(GambatteState);
			return IsCgb;
		}
		
		/// <summary>
		/// true if the emulator is currently emulating CGB in DMG compatibility mode (NOTE: this mode does not take affect until the bootrom unmaps itself)
		/// </summary>
		public bool IsCGBDMGMode()
		{
			return LibGambatte.gambatte_iscgbdmg(GambatteState);
		}

		private InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		// low priority TODO: due to certain aspects of the core implementation,
		// we don't smartly use the ActiveChanged event here.
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		/// <summary>
		/// for use in dual core
		/// </summary>
		public void ConnectInputCallbackSystem(InputCallbackSystem ics)
		{
			_inputCallbacks = ics;
		}

		// needs to match the reverse order of Libgambatte's button enum
		static readonly IReadOnlyList<String> BUTTON_ORDER_IN_BITMASK = new string[] { "Down", "Up", "Left", "Right", "Start", "Select", "B", "A" };

		internal void FrameAdvancePrep(IController controller)
		{
			// update our local copy of the controller data
			byte b = 0;
			for (var i = 0; i < 8; i++)
			{
				b <<= 1;
				if (controller.IsPressed(BUTTON_ORDER_IN_BITMASK[i])) b |= 1;
			}
			CurrentButtons = (LibGambatte.Buttons)b;

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			if (controller.IsPressed("Power"))
			{
				LibGambatte.gambatte_reset(GambatteState);
			}

			if (Tracer.Enabled)
			{
				_tracecb = MakeTrace;
			}
			else
			{
				_tracecb = null;
			}

			LibGambatte.gambatte_settracecallback(GambatteState, _tracecb);

			LibGambatte.gambatte_setlayers(GambatteState, (_syncSettings.DisplayBG ? 1 : 0) | (_syncSettings.DisplayOBJ ? 2 : 0) | (_syncSettings.DisplayWindow ? 4 : 0));
		}

		internal void FrameAdvancePost()
		{
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			endofframecallback?.Invoke(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
		}


		private static string MapperName(byte[] romdata)
		{
			switch (romdata[0x147])
			{
				case 0x00: return "Plain ROM"; // = PLAIN; break;
				case 0x01: return "MBC1 ROM"; // = MBC1; break;
				case 0x02: return "MBC1 ROM+RAM"; // = MBC1; break;
				case 0x03: return "MBC1 ROM+RAM+BATTERY"; // = MBC1; break;
				case 0x05: return "MBC2 ROM"; // = MBC2; break;
				case 0x06: return "MBC2 ROM+BATTERY"; // = MBC2; break;
				case 0x08: return "Plain ROM+RAM"; // = PLAIN; break;
				case 0x09: return "Plain ROM+RAM+BATTERY"; // = PLAIN; break;
				case 0x0F: return "MBC3 ROM+TIMER+BATTERY"; // = MBC3; break;
				case 0x10: return "MBC3 ROM+TIMER+RAM+BATTERY"; // = MBC3; break;
				case 0x11: return "MBC3 ROM"; // = MBC3; break;
				case 0x12: return "MBC3 ROM+RAM"; // = MBC3; break;
				case 0x13: return "MBC3 ROM+RAM+BATTERY"; // = MBC3; break;
				case 0x19: return "MBC5 ROM"; // = MBC5; break;
				case 0x1A: return "MBC5 ROM+RAM"; // = MBC5; break;
				case 0x1B: return "MBC5 ROM+RAM+BATTERY"; // = MBC5; break;
				case 0x1C: return "MBC5 ROM+RUMBLE"; // = MBC5; break;
				case 0x1D: return "MBC5 ROM+RUMBLE+RAM"; // = MBC5; break;
				case 0x1E: return "MBC5 ROM+RUMBLE+RAM+BATTERY"; // = MBC5; break;
				case 0xFF: return "HuC1 ROM+RAM+BATTERY"; // = HUC1; break;
				case 0xFE: return "HuC3 ROM+RAM+BATTERY";
				default: return "UNKNOWN";
			}
		}

		/// <summary>
		/// throw exception with intelligible message on some kinds of bad rom
		/// </summary>
		private static void ThrowExceptionForBadRom(byte[] romdata)
		{
			if (romdata.Length < 0x148)
			{
				throw new ArgumentException("ROM is far too small to be a valid GB\\GBC rom!");
			}

			switch (romdata[0x147])
			{
				case 0x00: break;
				case 0x01: break;
				case 0x02: break;
				case 0x03: break;
				case 0x05: break;
				case 0x06: break;
				case 0x08: break;
				case 0x09: break;

				case 0x0b: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");
				case 0x0c: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");
				case 0x0d: throw new UnsupportedGameException("\"MM01\" Mapper not supported!");

				case 0x0f: break;
				case 0x10: break;
				case 0x11: break;
				case 0x12: break;
				case 0x13: break;

				case 0x19: break;
				case 0x1a: break;
				case 0x1b: break;
				case 0x1c: break; // rumble
				case 0x1d: break; // rumble
				case 0x1e: break; // rumble

				case 0x20: throw new UnsupportedGameException("\"MBC6\" Mapper not supported!");
				case 0x22: throw new UnsupportedGameException("\"MBC7\" Mapper not supported!");

				case 0xfc: throw new UnsupportedGameException("\"Pocket Camera\" Mapper not supported!");
				case 0xfd: throw new UnsupportedGameException("\"Bandai TAMA5\" Mapper not supported!");
				case 0xfe: break;
				case 0xff: break;
				default: throw new UnsupportedGameException($"Unknown mapper: {romdata[0x147]:x2}");
			}
		}

		private static int[] ColorsFromTitleHash(byte[] romdata)
		{
			int titleHash = 0;
			for (int i = 0; i < 16; i++)
			{
				titleHash += romdata[0x134 + i];
			}

			switch (titleHash & 0xFF)
			{
				case 0x01:
				case 0x10:
				case 0x29:
				case 0x52:
				case 0x5D:
				case 0x68:
				case 0x6D:
				case 0xF6:
					return new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 };
				case 0x0C:
				case 0x16:
				case 0x35:
				case 0x67:
				case 0x75:
				case 0x92:
				case 0x99:
				case 0xB7:
					return new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 };
				case 0x14:
					return new int[] { 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x15:
				case 0xDB:
					return new int[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000 };
				case 0x17:
				case 0x8B:
					return new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
				case 0x19:
					return new int[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x1D:
					return new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000 };
				case 0x34:
					return new int[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x36:
					return new int[] { 0x52DE00, 0xFF8400, 0xFFFF00, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x39:
				case 0x43:
				case 0x97:
					return new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
				case 0x3C:
					return new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x3D:
					return new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x3E:
				case 0xE0:
					return new int[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
				case 0x49:
				case 0x5C:
					return new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF };
				case 0x4B:
				case 0x90:
				case 0x9A:
				case 0xBD:
					return new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x4E:
					return new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFFFF7B, 0x0084FF, 0xFF0000 };
				case 0x58:
					return new int[] { 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000, 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000, 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000 };
				case 0x59:
					return new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
				case 0x69:
				case 0xF2:
					return new int[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
				case 0x6B:
					return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
				case 0x6F:
					return new int[] { 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000, 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000, 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000 };
				case 0x70:
					return new int[] { 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x00FF00, 0x318400, 0x004A00, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
				case 0x71:
				case 0xFF:
					return new int[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000 };
				case 0x86:
				case 0xA8:
					return new int[] { 0xFFFF9C, 0x94B5FF, 0x639473, 0x003A3A, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
				case 0x88:
					return new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xA59CFF, 0xFFFF00, 0x006300, 0x000000 };
				case 0x8C:
					return new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000 };
				case 0x95:
					return new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
				case 0x9C:
					return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000 };
				case 0x9D:
					return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 };
				case 0xA2:
				case 0xF7:
					return new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
				case 0xAA:
					return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000 };
				case 0xC9:
					return new int[] { 0xFFFFCE, 0x63EFEF, 0x9C8431, 0x5A5A5A, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
				case 0xCE:
				case 0xD1:
				case 0xF0:
					return new int[] { 0x6BFF00, 0xFFFFFF, 0xFF524A, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 };
				case 0xE8:
					return new int[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF };
				case 0x0D:
					switch (romdata[0x137])
					{
						case 0x45:
							return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000 };
						case 0x52:
							return new int[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x18:
					switch (romdata[0x137])
					{
						case 0x4B:
							return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x27:
					switch (romdata[0x137])
					{
						case 0x42:
							return new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF };
						case 0x4E:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x28:
					switch (romdata[0x137])
					{
						case 0x41:
							return new int[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF };
						case 0x46:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x46:
					switch (romdata[0x137])
					{
						case 0x45:
							return new int[] { 0xB5B5FF, 0xFFFF94, 0xAD5A42, 0x000000, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A };
						case 0x52:
							return new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFF00, 0xFF0000, 0x630000, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x61:
					switch (romdata[0x137])
					{
						case 0x41:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
						case 0x45:
							return new int[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x66:
					switch (romdata[0x137])
					{
						case 0x45:
							return new int[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0x6A:
					switch (romdata[0x137])
					{
						case 0x49:
							return new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
						case 0x4B:
							return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0xA5:
					switch (romdata[0x137])
					{
						case 0x41:
							return new int[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF };
						case 0x52:
							return new int[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0xB3:
					switch (romdata[0x137])
					{
						case 0x42:
							return new int[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF };
						case 0x52:
							return new int[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
						case 0x55:
							return new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0xBF:
					switch (romdata[0x137])
					{
						case 0x20:
							return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
						case 0x43:
							return new int[] { 0x6BFF00, 0xFFFFFF, 0xFF524A, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0xC6:
					switch (romdata[0x137])
					{
						case 0x41:
							return new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0xD3:
					switch (romdata[0x137])
					{
						case 0x49:
							return new int[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
						case 0x52:
							return new int[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				case 0xF4:
					switch (romdata[0x137])
					{
						case 0x20:
							return new int[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
						case 0x2D:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 };
						default:
							return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
					}
				default:
					return new int[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
			}
		}

		public IGPUMemoryAreas LockGPU()
		{
			var _vram = IntPtr.Zero;
			var _bgpal = IntPtr.Zero;
			var _sppal = IntPtr.Zero;
			var _oam = IntPtr.Zero;
			int unused = 0;
			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.vram, ref _vram, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.bgpal, ref _bgpal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.sppal, ref _sppal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.oam, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in gambatte_getmemoryarea");
			}
			return new GPUMemoryAreas
			{
				Vram = _vram,
				Oam = _oam,
				Sppal = _sppal,
				Bgpal = _bgpal,	
			};
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; init; }

			public IntPtr Oam { get; init; }

			public IntPtr Sppal { get; init; }

			public IntPtr Bgpal { get; init; }

			public void Dispose() {}
		}

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			if (GambatteState == IntPtr.Zero)
			{
				return; // not sure how this is being reached.  tried the debugger...
			}

			endofframecallback = null;
			if (callback == null || line == -1 || line == -2)
			{
				scanlinecb = null;
				LibGambatte.gambatte_setscanlinecallback(GambatteState, null, 0);
				if (line == -1)
				{
					endofframecallback = callback;
				}
				else if (line == -2)
				{
					callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
				}
			}
			else if (line >= 0 && line <= 153)
			{
				scanlinecb = () => callback(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
				LibGambatte.gambatte_setscanlinecallback(GambatteState, scanlinecb, line);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be in [0, 153]");
			}
		}

		private GambattePrinter printer;

		/// <summary>
		/// set up Printer callback
		/// </summary>
		public void SetPrinterCallback(PrinterCallback callback)
		{
			// Copying SetScanlineCallback for this check, I assume this is still a bug somewhere
			if (GambatteState == IntPtr.Zero)
			{
				return; // not sure how this is being reached.  tried the debugger...
			}

			if (callback != null)
			{
				printer = new GambattePrinter(this, callback);
				LinkConnected = true;
			}
			else
			{
				LinkConnected = false;
				printer.Disconnect();
				printer = null;
			}
		}

		private LibGambatte.ScanlineCallback scanlinecb;
		private ScanlineCallback endofframecallback;

		/// <summary>
		/// update gambatte core's internal colors
		/// </summary>
		public void ChangeDMGColors(int[] colors)
		{
			for (int i = 0; i < 12; i++)
			{
				LibGambatte.gambatte_setdmgpalettecolor(GambatteState, (LibGambatte.PalType)(i / 4), (uint)i % 4, (uint)colors[i]);
			}
		}

		public void SetCGBColors(GBColors.ColorType type)
		{
			int[] lut = GBColors.GetLut(type);
			LibGambatte.gambatte_setcgbpalette(GambatteState, lut);
		}
	}
}
