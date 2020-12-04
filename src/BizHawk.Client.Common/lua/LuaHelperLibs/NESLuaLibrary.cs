using System;
using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("Functions related specifically to Nes Cores")]
	public sealed class NESLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		// TODO:  
		// perhaps with the new core config system, one could
		// automatically bring out all of the settings to a lua table, with names.  that
		// would be completely arbitrary and would remove the whole requirement for this mess
		[OptionalService]
		private IMemoryDomains MemoryDomains { get; set; }

		public NESLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "nes";

		private object Settings
		{
			get => APIs.Emulation.GetSettings();
			set => APIs.Emulation.PutSettings(value);
		}

		[LuaDeprecatedMethod]
		[LuaMethod("addgamegenie", "Adds the specified game genie code. If an NES game is not currently loaded or the code is not a valid game genie code, this will have no effect")]
		public void AddGameGenie(string code)
		{
			Log("Method no longer supported, use client.addcheat() instead");
		}

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("if ( nes.getallowmorethaneightsprites( ) ) then\r\n\tconsole.log( \"Gets the NES setting 'Allow more than 8 sprites per scanline' value\" );\r\nend;")]
		[LuaMethod("getallowmorethaneightsprites", "Gets the NES setting 'Allow more than 8 sprites per scanline' value")]
		public bool GetAllowMoreThanEightSprites() => Settings switch
		{
			NES.NESSettings nhs => nhs.AllowMoreThanEightSprites,
			QuickNES.QuickNESSettings qns => qns.NumSprites != 8,
			_ => throw new InvalidOperationException()
		};

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("local innesget = nes.getbottomscanline( false );")]
		[LuaMethod("getbottomscanline", "Gets the current value for the bottom scanline value")]
		public int GetBottomScanline(bool pal = false) => Settings switch
		{
			NES.NESSettings nhs => pal ? nhs.PAL_BottomLine : nhs.NTSC_BottomLine,
			QuickNES.QuickNESSettings qns => qns.ClipTopAndBottom ? 231 : 239,
			_ => throw new InvalidOperationException()
		};

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("if ( nes.getclipleftandright( ) ) then\r\n\tconsole.log( \"Gets the current value for the Clip Left and Right sides option\" );\r\nend;")]
		[LuaMethod("getclipleftandright", "Gets the current value for the Clip Left and Right sides option")]
		public bool GetClipLeftAndRight() => Settings switch
		{
			NES.NESSettings nhs => nhs.ClipLeftAndRight,
			QuickNES.QuickNESSettings qns => qns.ClipLeftAndRight,
			_ => throw new InvalidOperationException()
		};

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("if ( nes.getdispbackground( ) ) then\r\n\tconsole.log( \"Indicates whether or not the bg layer is being displayed\" );\r\nend;")]
		[LuaMethod("getdispbackground", "Indicates whether or not the bg layer is being displayed")]
		public bool GetDisplayBackground() => Settings switch
		{
			NES.NESSettings nhs => nhs.DispBackground,
			QuickNES.QuickNESSettings _ => true,
			_ => throw new InvalidOperationException()
		};

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("if ( nes.getdispsprites( ) ) then\r\n\tconsole.log( \"Indicates whether or not sprites are being displayed\" );\r\nend;")]
		[LuaMethod("getdispsprites", "Indicates whether or not sprites are being displayed")]
		public bool GetDisplaySprites() => Settings switch
		{
			NES.NESSettings nhs => nhs.DispSprites,
			QuickNES.QuickNESSettings qns => qns.NumSprites > 0,
			_ => throw new InvalidOperationException()
		};

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("local innesget = nes.gettopscanline(false);")]
		[LuaMethod("gettopscanline", "Gets the current value for the top scanline value")]
		public int GetTopScanline(bool pal = false) => Settings switch
		{
			NES.NESSettings nhs => pal ? nhs.PAL_TopLine : nhs.NTSC_TopLine,
			QuickNES.QuickNESSettings qns => qns.ClipTopAndBottom ? 8 : 0,
			_ => throw new InvalidOperationException()
		};

		[LuaDeprecatedMethod]
		[LuaMethod("removegamegenie", "Removes the specified game genie code. If an NES game is not currently loaded or the code is not a valid game genie code, this will have no effect")]
		public void RemoveGameGenie(string code)
		{
			Log("Method no longer supported, use client.removecheat() instead");
		}

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("nes.setallowmorethaneightsprites( true );")]
		[LuaMethod("setallowmorethaneightsprites", "Sets the NES setting 'Allow more than 8 sprites per scanline'")]
		public void SetAllowMoreThanEightSprites(bool allow)
		{
			switch (Settings)
			{
				case NES.NESSettings nhs:
					nhs.AllowMoreThanEightSprites = allow;
					Settings = nhs;
					break;
				case QuickNES.QuickNESSettings qns:
					qns.NumSprites = allow ? 64 : 8;
					Settings = qns;
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("nes.setclipleftandright( true );")]
		[LuaMethod("setclipleftandright", "Sets the Clip Left and Right sides option")]
		public void SetClipLeftAndRight(bool leftandright)
		{
			switch (Settings)
			{
				case NES.NESSettings nhs:
					nhs.ClipLeftAndRight = leftandright;
					Settings = nhs;
					break;
				case QuickNES.QuickNESSettings qns:
					qns.ClipLeftAndRight = leftandright;
					Settings = qns;
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("nes.setdispbackground( true );")]
		[LuaMethod("setdispbackground", "Sets whether or not the background layer will be displayed")]
		public void SetDisplayBackground(bool show)
		{
			switch (Settings)
			{
				case NES.NESSettings nhs:
					nhs.DispBackground = show;
					Settings = nhs;
					break;
				case QuickNES.QuickNESSettings _:
					return;
				default:
					throw new InvalidOperationException();
			}
		}

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("nes.setdispsprites( true );")]
		[LuaMethod("setdispsprites", "Sets whether or not sprites will be displayed")]
		public void SetDisplaySprites(bool show)
		{
			switch (Settings)
			{
				case NES.NESSettings nhs:
					nhs.DispSprites = show;
					Settings = nhs;
					break;
				case QuickNES.QuickNESSettings qns:
					qns.NumSprites = show ? 8 : 0;
					Settings = qns;
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		/// <exception cref="InvalidOperationException">loaded core is not NESHawk or QuickNes</exception>
		[LuaMethodExample("nes.setscanlines( 10, 20, false );")]
		[LuaMethod("setscanlines", "sets the top and bottom scanlines to be drawn (same values as in the graphics options dialog). Top must be in the range of 0 to 127, bottom must be between 128 and 239. Not supported in the Quick Nes core")]
		public void SetScanlines(int top, int bottom, bool pal = false)
		{
			switch (Settings)
			{
				case NES.NESSettings nhs:
					if (top > 127)
					{
						top = 127;
					}
					else if (top < 0)
					{
						top = 0;
					}

					if (bottom > 239)
					{
						bottom = 239;
					}
					else if (bottom < 128)
					{
						bottom = 128;
					}

					if (pal)
					{
						nhs.PAL_TopLine = top;
						nhs.PAL_BottomLine = bottom;
					}
					else
					{
						nhs.NTSC_TopLine = top;
						nhs.NTSC_BottomLine = bottom;
					}

					Settings = nhs;
					break;
				case QuickNES.QuickNESSettings _:
					return;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
