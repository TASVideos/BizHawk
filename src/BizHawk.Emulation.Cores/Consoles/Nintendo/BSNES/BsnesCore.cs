using System;
using System.Linq;
using System.Xml;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.W65816;

// http://wiki.superfamicom.org/snes/show/Backgrounds

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	[PortedCore(CoreNames.Bsnes115, "bsnes team", "v115+", "https://bsnes.dev", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public unsafe partial class BsnesCore : IEmulator, IDebuggable, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>
	{
		private BsnesApi.SNES_REGION _region;

		[CoreConstructor("SGB")]
		[CoreConstructor("SNES")]
		public BsnesCore(GameInfo game, byte[] rom, CoreComm comm,
			SnesSettings settings, SnesSyncSettings syncSettings)
			:this(game, rom, null, null, comm, settings, syncSettings)
		{}

		public BsnesCore(GameInfo game, byte[] romData, byte[] xmlData, string baseRomPath, CoreComm comm,
			SnesSettings settings, SnesSyncSettings syncSettings)
		{
			_baseRomPath = baseRomPath;
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			CoreComm = comm;
			byte[] sgbRomData = null;

			if (game.System == "SGB")
			{
				if ((romData[0x143] & 0xc0) == 0xc0)
				{
					throw new CGBNotSupportedException();
				}

				sgbRomData = CoreComm.CoreFileProvider.GetFirmware("SNES", "Rom_SGB", true, "SGB Rom is required for SGB emulation.");
				game.FirmwareHash = sgbRomData.HashSHA1();
			}

			_settings = settings ?? new SnesSettings();
			_syncSettings = syncSettings ?? new SnesSyncSettings();

			BsnesApi.SnesCallbacks callbacks = new()
			{
				inputPollCb = snes_input_poll,
				inputStateCb = snes_input_state,
				noLagCb = snes_no_lag,
				videoFrameCb = snes_video_refresh,
				audioSampleCb = snes_audio_sample,
				pathRequestCb = snes_path_request,
				traceCb = snes_trace,
				readHookCb = ReadHook,
				writeHookCb = WriteHook,
				execHookCb = ExecHook
			};

			Api = new BsnesApi(CoreComm.CoreFileProvider.DllPath(), CoreComm, callbacks.AllDelegatesInMemoryOrder());

			_controllers = new BsnesControllers(_syncSettings);

			generate_palette();
			// TODO: massive random hack till waterboxhost gets fixed to support 5+ args
			ushort mergedBools = (ushort) ((_syncSettings.Hotfixes ? 1 << 8 : 0) | (_syncSettings.FastPPU ? 1 : 0));
			Api.core.snes_init(_syncSettings.Entropy, _syncSettings.LeftPort, _syncSettings.RightPort, mergedBools);
			Api.SetCallbacks(callbacks);

			// start up audio resampler
			InitAudio();
			ser.Register<ISoundProvider>(_resampler);

			if (game.System == "SGB")
			{
				IsSGB = true;
				SystemId = "SNES";
				ser.Register<IBoardInfo>(new SGBBoardInfo());

				_currLoadParams = new LoadParams
				{
					type = LoadParamType.SuperGameBoy,
					baseRomPath = baseRomPath,
					romData = sgbRomData,
					sgbRomData = romData
				};
			}
			else
			{
				// we may need to get some information out of the cart, even during the following bootup/load process
				if (xmlData != null)
				{
					_romxml = new XmlDocument();
					_romxml.Load(new MemoryStream(xmlData));

					// bsnes wont inspect the xml to load the necessary sfc file.
					// so, we have to do that here and pass it in as the romData :/

					// TODO: uhh i have no idea what the xml is or whether this below code is needed
					if (_romxml["cartridge"]?["rom"] != null)
					{
						romData = File.ReadAllBytes(PathSubfile(_romxml["cartridge"]["rom"].Attributes["name"].Value));
					}
					else
					{
						throw new Exception("Could not find rom file specification in xml file. Please check the integrity of your xml file");
					}
				}

				SystemId = "SNES";
				_currLoadParams = new LoadParams
				{
					type = LoadParamType.Normal,
					baseRomPath = baseRomPath,
					romData = romData
				};
			}

			LoadCurrent();

			if (_region == BsnesApi.SNES_REGION.NTSC)
			{
				// taken from bsnes source
				VsyncNumerator = 21477272;
				VsyncDenominator = 357366;
			}
			else
			{
				// http://forums.nesdev.com/viewtopic.php?t=5367&start=19
				VsyncNumerator = 21281370;
				VsyncDenominator = 4 * 341 * 312;
			}

			SetMemoryDomains();

			_tracer = new TraceBuffer
			{
				Header = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, B, flags (NVMXDIZC), V, H)"
			};
			ser.Register<IDisassemblable>(new W65816_DisassemblerService());
			ser.Register(_tracer);

			Api.Seal();
		}

		private CoreComm CoreComm { get; }

		private readonly string _baseRomPath;

		private string PathSubfile(string fname) => Path.Combine(_baseRomPath, fname);

		private readonly BsnesControllers _controllers;
		private readonly ITraceable _tracer;
		private readonly XmlDocument _romxml;

		private IController _controller;
		private readonly LoadParams _currLoadParams;
		private SpeexResampler _resampler;
		private bool _disposed;

		public bool IsSGB { get; }

		private class SGBBoardInfo : IBoardInfo
		{
			public string BoardName => "SGB";
		}

		private BsnesApi Api { get; }

		private string snes_path_request(int slot, string hint, bool required)
		{
			// TODO: this msu1 handling code is outdated and needs to be remade from someone with knowledge.
			// every rom requests msu1.rom... why? who knows.
			// also handle msu-1 pcm files here
			bool isMsu1Rom = hint == "msu1/data.rom";
			bool isMsu1Pcm = Path.GetExtension(hint).ToLower() == ".pcm";
			if (isMsu1Rom || isMsu1Pcm)
			{
				// well, check if we have an msu-1 xml
				if (_romxml?["cartridge"]?["msu1"] != null)
				{
					var msu1 = _romxml["cartridge"]["msu1"];
					if (isMsu1Rom && msu1["rom"]?.Attributes["name"] != null)
					{
						return PathSubfile(msu1["rom"].Attributes["name"].Value);
					}

					if (isMsu1Pcm)
					{
						// return @"D:\roms\snes\SuperRoadBlaster\SuperRoadBlaster-1.pcm";
						// return "";
						int wantsTrackNumber = int.Parse(hint.Replace("track-", "").Replace(".pcm", ""));
						wantsTrackNumber++;
						string wantsTrackString = wantsTrackNumber.ToString();
						foreach (var child in msu1.ChildNodes.Cast<XmlNode>())
						{
							if (child.Name == "track" && child.Attributes["number"].Value == wantsTrackString)
							{
								return PathSubfile(child.Attributes["name"].Value);
							}
						}
					}
				}

				// not found.. what to do? (every rom will get here when msu1.rom is requested)
				return "";
			}

			// not MSU-1.  ok.
			if (hint == "save.ram")
			{
				// core asked for saveram, but the interface isn't designed to be able to handle this.
				// so, we'll just return nothing and the frontend will set the saveram itself later
				return null;
			}

			string firmwareId;

			switch (hint)
			{
				case "cx4": firmwareId = "CX4"; break;
				case "dsp1": firmwareId = "DSP1"; break;
				case "dsp1b": firmwareId = "DSP1b"; break;
				case "dsp2": firmwareId = "DSP2"; break;
				case "dsp3": firmwareId = "DSP3"; break;
				case "dsp4": firmwareId = "DSP4"; break;
				case "st010": firmwareId = "ST010"; break;
				case "st011": firmwareId = "ST011"; break;
				case "st018": firmwareId = "ST018"; break;
				default:
					CoreComm.ShowMessage($"Unrecognized SNES firmware request \"{hint}\".");
					return "";
			}

			string ret = "";
			var data = CoreComm.CoreFileProvider.GetFirmware("SNES", firmwareId, required, "Game may function incorrectly without the requested firmware.");
			if (data != null)
			{
				ret = hint;
				Api.AddReadonlyFile(data, hint);
			}

			Console.WriteLine("Served bsnescore request for firmware \"{0}\"", hint);

			// return the path we built
			return ret;
		}

		private enum LoadParamType
		{
			Normal, SuperGameBoy
		}

		private struct LoadParams
		{
			public LoadParamType type;
			public string baseRomPath;
			public byte[] romData;
			public byte[] sgbRomData;
		}

		private void LoadCurrent()
		{
			if (_currLoadParams.type == LoadParamType.Normal)
				Api.core.snes_load_cartridge_normal(_currLoadParams.baseRomPath, _currLoadParams.romData, _currLoadParams.romData.Length);
			else
				Api.core.snes_load_cartridge_super_gameboy(_currLoadParams.baseRomPath, _currLoadParams.romData,
					_currLoadParams.sgbRomData, (ulong) _currLoadParams.romData.Length << 32 |  (uint)_currLoadParams.sgbRomData.Length);

			_region = Api.core.snes_get_region();
		}

		// poll which updates the controller state
		private void snes_input_poll()
		{
			_controllers.CoreInputPoll(_controller);
		}

		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		private short snes_input_state(int port, int index, int id)
		{
			return _controllers.CoreInputState(port, index, id);
		}

		private void snes_no_lag()
		{
			// gets called whenever there was input polled, aka no lag
			IsLagFrame = false;
		}

		private readonly int[] palette = new int[32768];

		private void generate_palette()
		{
			for (int color = 0; color < 32768; color++) {
				int r = (color >> 10) & 31;
				int g = (color >>  5) & 31;
				int b = (color >>  0) & 31;

				r = r << 3 | r >> 2; r = r << 8 | r << 0;
				g = g << 3 | g >> 2; g = g << 8 | g << 0;
				b = b << 3 | b >> 2; b = b << 8 | b << 0;

				palette[color] = r >> 8 << 16 | g >> 8 <<  8 | b >> 8 << 0;
			}
		}

		private void snes_video_refresh(ushort* data, int width, int height, int pitch)
		{
			int widthMultiplier = 1;
			int heightMultiplier = 1;
			if (_settings.CropSGBFrame && IsSGB)
			{
				BufferWidth = 160;
				BufferHeight = 144;
			}
			else
			{
				if (_settings.AlwaysDoubleSize)
				{
					if (width == 256) widthMultiplier = 2;
					if (height == 224) heightMultiplier = 2;
				}
				BufferWidth = width * widthMultiplier;
				BufferHeight = height * heightMultiplier;
			}

			int size = BufferWidth * BufferHeight;
			if (_videoBuffer.Length != size)
			{
				_videoBuffer = new int[size];
			}

			int di = 0;
			if (_settings.CropSGBFrame && IsSGB)
			{
				for (int y = 39; y < 39 + 144; y++)
				{
					ushort* sp = data + y * pitch + 48;
					for (int x = 0; x < 160; x++)
					{
						_videoBuffer[di++] = palette[*sp++];
					}
				}
				return;
			}

			for (int y = 0; y < height * heightMultiplier; y++)
			{
				int si = y / heightMultiplier * pitch;
				for (int x = 0; x < width * widthMultiplier; x++)
				{
					_videoBuffer[di++] = palette[data[si + x / widthMultiplier]];
				}
			}
		}

		private void InitAudio()
		{
			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DESKTOP, 64080, 88200, 32040, 44100);
		}

		private void snes_audio_sample(short left, short right)
		{
			_resampler.EnqueueSample(left, right);
		}

		private void snes_trace(string disassembly, string registerInfo)
		{
			_tracer.Put(new TraceInfo
			{
				Disassembly = disassembly,
				RegisterInfo = registerInfo
			});
		}

		private void ReadHook(uint addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, (uint) MemoryCallbackFlags.AccessRead, "System Bus");
			}
		}

		private void WriteHook(uint addr, byte value)
		{
			if (MemoryCallbacks.HasWrites)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, value, (uint) MemoryCallbackFlags.AccessWrite, "System Bus");
			}
		}

		private void ExecHook(uint addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, (uint) MemoryCallbackFlags.AccessExecute, "System Bus");
			}
		}
	}
}
