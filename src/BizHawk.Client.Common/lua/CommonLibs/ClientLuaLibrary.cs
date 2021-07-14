using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

using BizHawk.Client.Common.cheats;
using BizHawk.Common;
using BizHawk.Emulation.Common;

using NLua;

// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for manipulating the EmuHawk client UI")]
	public sealed class ClientLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IVideoProvider VideoProvider { get; set; }

		public IMainFormForApi MainForm { get; set; }

		public ClientLuaLibrary(IPlatformLuaLibEnv luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "client";

		[LuaMethodExample("client.exit( );")]
		[LuaMethod("exit", "Closes the emulator")]
		public void CloseEmulator() => APIs.EmuClient.CloseEmulator();

		[LuaMethodExample("client.exitCode( 0 );")]
		[LuaMethod("exitCode", "Closes the emulator and returns the provided code")]
		public void CloseEmulatorWithCode(int exitCode) => APIs.EmuClient.CloseEmulator(exitCode);

		[LuaMethodExample("local inclibor = client.borderheight( );")]
		[LuaMethod("borderheight", "Gets the current height in pixels of the letter/pillarbox area (top side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")]
		public int BorderHeight() => APIs.EmuClient.BorderHeight();

		[LuaMethodExample("local inclibor = client.borderwidth( );")]
		[LuaMethod("borderwidth", "Gets the current width in pixels of the letter/pillarbox area (left side only) around the emu display surface, excluding the gameExtraPadding you've set. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")]
		public int BorderWidth() => APIs.EmuClient.BorderWidth();

		[LuaMethodExample("local inclibuf = client.bufferheight( );")]
		[LuaMethod("bufferheight", "Gets the visible height of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.")]
		public int BufferHeight()
		{
			return VideoProvider?.BufferHeight ?? NullVideo.Instance.BufferHeight; // TODO: consider exposing the video provider from mainform, so it can decide NullVideo is the correct substitute
		}

		[LuaMethodExample("local inclibuf = client.bufferwidth( );")]
		[LuaMethod("bufferwidth", "Gets the visible width of the emu display surface (the core video output). This excludes the gameExtraPadding you've set.")]
		public int BufferWidth()
		{
			return VideoProvider?.BufferWidth ?? NullVideo.Instance.BufferWidth;
		}

		[LuaMethodExample("client.clearautohold( );")]
		[LuaMethod("clearautohold", "Clears all autohold keys")]
		public void ClearAutohold() => APIs.EmuClient.ClearAutohold();

		[LuaMethodExample("client.closerom( );")]
		[LuaMethod("closerom", "Closes the loaded Rom")]
		public void CloseRom() => APIs.EmuClient.CloseRom();

		[LuaMethodExample("client.enablerewind( true );")]
		[LuaMethod("enablerewind", "Sets whether or not the rewind feature is enabled")]
		public void EnableRewind(bool enabled) => APIs.EmuClient.EnableRewind(enabled);

		[LuaMethodExample("client.frameskip( 8 );")]
		[LuaMethod("frameskip", "Sets the frame skip value of the client UI (use 0 to disable)")]
		public void FrameSkip(int numFrames) => APIs.EmuClient.FrameSkip(numFrames);

		[LuaMethod("get_lua_engine", "returns the name of the Lua engine currently in use")]
		public string GetLuaEngine() => _luaLibsImpl.EngineName;

		[LuaMethodExample("client.invisibleemulation( true );")]
		[LuaMethod("invisibleemulation", "Disables and enables emulator updates")]
		public void InvisibleEmulation(bool invisible) => APIs.EmuClient.InvisibleEmulation(invisible);

		[LuaMethodExample("client.seekframe( 100 );")]
		[LuaMethod("seekframe", "Makes the emulator seek to the frame specified")]
		public void SeekFrame(int frame)
		{
			if (frame < Emulator.Frame)
			{
				Log("client.seekframe: cannot seek backwards");
				return;
			}
			if (frame == Emulator.Frame) return;

			bool wasPaused = MainForm.EmulatorPaused;

			// can't re-enter lua while doing this
			_luaLibsImpl.IsUpdateSupressed = true;
			while (Emulator.Frame != frame)
			{
				MainForm.SeekFrameAdvance();
			}

			_luaLibsImpl.IsUpdateSupressed = false;

			if (!wasPaused)
			{
				MainForm.UnpauseEmulator();
			}
		}

		[LuaMethodExample("local chug = client.get_approx_framerate() < 60;")]
		[LuaMethod("get_approx_framerate", "Gets the (host) framerate, approximated from frame durations.")]
		public int GetApproxFramerate() => APIs.EmuClient.GetApproxFramerate();

		[LuaMethodExample("local incliget = client.gettargetscanlineintensity( );")]
		[LuaMethod("gettargetscanlineintensity", "Gets the current scanline intensity setting, used for the scanline display filter")]
		public int GetTargetScanlineIntensity() => APIs.EmuClient.GetTargetScanlineIntensity();

		[LuaMethodExample("local incliget = client.getwindowsize( );")]
		[LuaMethod("getwindowsize", "Gets the main window's size Possible values are 1, 2, 3, 4, 5, and 10")]
		public int GetWindowSize() => APIs.EmuClient.GetWindowSize();

		[LuaMethodExample("client.SetGameExtraPadding( 5, 10, 15, 20 );")]
		[LuaMethod("SetGameExtraPadding", "Sets the extra padding added to the 'emu' surface so that you can draw HUD elements in predictable placements")]
		public void SetGameExtraPadding(int left, int top, int right, int bottom) => APIs.EmuClient.SetGameExtraPadding(left, top, right, bottom);

		[LuaMethodExample("client.SetSoundOn( true );")]
		[LuaMethod("SetSoundOn", "Sets the state of the Sound On toggle")]
		public void SetSoundOn(bool enable) => APIs.EmuClient.SetSoundOn(enable);

		[LuaMethodExample("if ( client.GetSoundOn( ) ) then\r\n\tconsole.log( \"Gets the state of the Sound On toggle\" );\r\nend;")]
		[LuaMethod("GetSoundOn", "Gets the state of the Sound On toggle")]
		public bool GetSoundOn() => APIs.EmuClient.GetSoundOn();

		[LuaMethodExample("client.SetClientExtraPadding( 5, 10, 15, 20 );")]
		[LuaMethod("SetClientExtraPadding", "Sets the extra padding added to the 'native' surface so that you can draw HUD elements in predictable placements")]
		public void SetClientExtraPadding(int left, int top, int right, int bottom) => APIs.EmuClient.SetClientExtraPadding(left, top, right, bottom);

		[LuaMethodExample("if ( client.ispaused( ) ) then\r\n\tconsole.log( \"Returns true if emulator is paused, otherwise, false\" );\r\nend;")]
		[LuaMethod("ispaused", "Returns true if emulator is paused, otherwise, false")]
		public bool IsPaused() => APIs.EmuClient.IsPaused();

		[LuaMethodExample("if ( client.client.isturbo( ) ) then\r\n\tconsole.log( \"Returns true if emulator is in turbo mode, otherwise, false\" );\r\nend;")]
		[LuaMethod("isturbo", "Returns true if emulator is in turbo mode, otherwise, false")]
		public bool IsTurbo() => APIs.EmuClient.IsTurbo();

		[LuaMethodExample("if ( client.isseeking( ) ) then\r\n\tconsole.log( \"Returns true if emulator is seeking, otherwise, false\" );\r\nend;")]
		[LuaMethod("isseeking", "Returns true if emulator is seeking, otherwise, false")]
		public bool IsSeeking() => APIs.EmuClient.IsSeeking();

		[LuaMethodExample("client.opencheats( );")]
		[LuaMethod("opencheats", "opens the Cheats dialog")]
		public void OpenCheats() => APIs.Tool.OpenCheats();

		[LuaMethodExample("client.openhexeditor( );")]
		[LuaMethod("openhexeditor", "opens the Hex Editor dialog")]
		public void OpenHexEditor() => APIs.Tool.OpenHexEditor();

		[LuaMethodExample("client.openramwatch( );")]
		[LuaMethod("openramwatch", "opens the RAM Watch dialog")]
		public void OpenRamWatch() => APIs.Tool.OpenRamWatch();

		[LuaMethodExample("client.openramsearch( );")]
		[LuaMethod("openramsearch", "opens the RAM Search dialog")]
		public void OpenRamSearch() => APIs.Tool.OpenRamSearch();

		[LuaMethodExample("client.openrom( \"C:\\\" );")]
		[LuaMethod("openrom", "opens the Open ROM dialog")]
		public void OpenRom(string path) => APIs.EmuClient.OpenRom(path);

		[LuaMethodExample("client.opentasstudio( );")]
		[LuaMethod("opentasstudio", "opens the TAStudio dialog")]
		public void OpenTasStudio() => APIs.Tool.OpenTasStudio();

		[LuaMethodExample("client.opentoolbox( );")]
		[LuaMethod("opentoolbox", "opens the Toolbox Dialog")]
		public void OpenToolBox() => APIs.Tool.OpenToolBox();

		[LuaMethodExample("client.opentracelogger( );")]
		[LuaMethod("opentracelogger", "opens the tracelogger if it is available for the given core")]
		public void OpenTraceLogger() => APIs.Tool.OpenTraceLogger();

		[LuaMethodExample("client.pause( );")]
		[LuaMethod("pause", "Pauses the emulator")]
		public void Pause() => APIs.EmuClient.Pause();

		[LuaMethodExample("client.pause_av( );")]
		[LuaMethod("pause_av", "If currently capturing Audio/Video, this will suspend the record. Frames will not be captured into the AV until client.unpause_av() is called")]
		public void PauseAv() => APIs.EmuClient.PauseAv();

		[LuaMethodExample("client.reboot_core( );")]
		[LuaMethod("reboot_core", "Reboots the currently loaded core")]
		public void RebootCore()
		{
			_luaLibsImpl.IsRebootingCore = true;
			APIs.EmuClient.RebootCore();
			_luaLibsImpl.IsRebootingCore = false;
		}

		[LuaMethodExample("local incliscr = client.screenheight( );")]
		[LuaMethod("screenheight", "Gets the current height in pixels of the emulator's drawing area")]
		public int ScreenHeight() => APIs.EmuClient.ScreenHeight();

		[LuaMethodExample("client.screenshot( \"C:\\\" );")]
		[LuaMethod("screenshot", "if a parameter is passed it will function as the Screenshot As menu item of EmuHawk, else it will function as the Screenshot menu item")]
		public void Screenshot(string path = null) => APIs.EmuClient.Screenshot(path);

		[LuaMethodExample("client.screenshottoclipboard( );")]
		[LuaMethod("screenshottoclipboard", "Performs the same function as EmuHawk's Screenshot To Clipboard menu item")]
		public void ScreenshotToClipboard() => APIs.EmuClient.ScreenshotToClipboard();

		[LuaMethodExample("client.settargetscanlineintensity( -1000 );")]
		[LuaMethod("settargetscanlineintensity", "Sets the current scanline intensity setting, used for the scanline display filter")]
		public void SetTargetScanlineIntensity(int val) => APIs.EmuClient.SetTargetScanlineIntensity(val);

		[LuaMethodExample("client.setscreenshotosd( true );")]
		[LuaMethod("setscreenshotosd", "Sets the screenshot Capture OSD property of the client")]
		public void SetScreenshotOSD(bool value) => APIs.EmuClient.SetScreenshotOSD(value);

		[LuaMethodExample("local incliscr = client.screenwidth( );")]
		[LuaMethod("screenwidth", "Gets the current width in pixels of the emulator's drawing area")]
		public int ScreenWidth() => APIs.EmuClient.ScreenWidth();

		[LuaMethodExample("client.setwindowsize( 100 );")]
		[LuaMethod("setwindowsize", "Sets the main window's size to the give value. Accepted values are 1, 2, 3, 4, 5, and 10")]
		public void SetWindowSize(int size) => APIs.EmuClient.SetWindowSize(size);

		[LuaMethodExample("client.speedmode( 75 );")]
		[LuaMethod("speedmode", "Sets the speed of the emulator (in terms of percent)")]
		public void SpeedMode(int percent) => APIs.EmuClient.SpeedMode(percent);

		[LuaMethodExample("local curSpeed = client.getconfig().SpeedPercent")]
		[LuaMethod("getconfig", "gets the current config settings object")]
		public object GetConfig() => ((EmulationApi) APIs.Emulation).ForbiddenConfigReference;

		[LuaMethodExample("client.togglepause( );")]
		[LuaMethod("togglepause", "Toggles the current pause state")]
		public void TogglePause() => APIs.EmuClient.TogglePause();

		[LuaMethodExample("local newY = client.transform_point( 32, 100 ).y;")]
		[LuaMethod("transformPoint", "Transforms a point (x, y) in emulator space to a point in client space")]
		public LuaTable TransformPoint(int x, int y) {
			var transformed = APIs.EmuClient.TransformPoint(new Point(x, y));
			var table = _th.CreateTable();
			table["x"] = transformed.X;
			table["y"] = transformed.Y;
			return table;
		}

		[LuaMethodExample("client.unpause( );")]
		[LuaMethod("unpause", "Unpauses the emulator")]
		public void Unpause() => APIs.EmuClient.Unpause();

		[LuaMethodExample("client.unpause_av( );")]
		[LuaMethod("unpause_av", "If currently capturing Audio/Video this resumes capturing")]
		public void UnpauseAv() => APIs.EmuClient.UnpauseAv();

		[LuaMethodExample("local inclixpo = client.xpos( );")]
		[LuaMethod("xpos", "Returns the x value of the screen position where the client currently sits")]
		public int Xpos() => APIs.EmuClient.Xpos();

		[LuaMethodExample("local incliypo = client.ypos( );")]
		[LuaMethod("ypos", "Returns the y value of the screen position where the client currently sits")]
		public int Ypos() => APIs.EmuClient.Ypos();

		[LuaMethodExample("local incbhver = client.getversion( );")]
		[LuaMethod("getversion", "Returns the current stable BizHawk version")]
		public static string GetVersion()
		{
			return VersionInfo.MainVersion;
		}

		[LuaMethodExample("local nlcliget = client.getavailabletools( );")]
		[LuaMethod("getavailabletools", "Returns a list of the tools currently open")]
		public LuaTable GetAvailableTools() => _th.EnumerateToLuaTable(APIs.Tool.AvailableTools.Select(tool => tool.Name.ToLower()));

		[LuaMethodExample("local nlcliget = client.gettool( \"Tool name\" );")]
		[LuaMethod("gettool", "Returns an object that represents a tool of the given name (not case sensitive). If the tool is not open, it will be loaded if available. Use gettools to get a list of names")]
		public LuaTable GetTool(string name)
		{
			var selectedTool = APIs.Tool.GetTool(name);
			return selectedTool == null ? null : _th.ObjectToTable(selectedTool);
		}

		[LuaMethodExample("local nlclicre = client.createinstance( \"objectname\" );")]
		[LuaMethod("createinstance", "returns a default instance of the given type of object if it exists (not case sensitive). Note: This will only work on objects which have a parameterless constructor.  If no suitable type is found, or the type does not have a parameterless constructor, then nil is returned")]
		public LuaTable CreateInstance(string name)
		{
			var instance = APIs.Tool.CreateInstance(name);
			return instance == null ? null : _th.ObjectToTable(instance);
		}

		[LuaMethodExample("client.displaymessages( true );")]
		[LuaMethod("displaymessages", "sets whether or not on screen messages will display")]
		public void DisplayMessages(bool value) => APIs.EmuClient.DisplayMessages(value);

		[LuaMethodExample("client.saveram( );")]
		[LuaMethod("saveram", "flushes save ram to disk")]
		public void SaveRam() => APIs.EmuClient.SaveRam();

		[LuaMethodExample("client.sleep( 50 );")]
		[LuaMethod("sleep", "sleeps for n milliseconds")]
		public void Sleep(int millis)
		{
			Thread.Sleep(millis);
		}

		[LuaMethodExample("client.exactsleep( 50 );")]
		[LuaMethod("exactsleep", "sleeps exactly for n milliseconds")]
		public void ExactSleep(int millis)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (millis - stopwatch.ElapsedMilliseconds > 100)
			{
				Thread.Sleep(50);
			}
			while (true)
			{
				if (stopwatch.ElapsedMilliseconds >= millis)
				{
					break;
				}
			}
		}

		[LuaMethodExample("client.addcheat(\"NNNPAK\");")]
		[LuaMethod("addcheat", "adds a cheat code, if supported")]
		public void AddCheat(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return;
			}

			if (!MainForm.Emulator.HasMemoryDomains())
			{
				Log($"cheat codes not supported by the current system: {MainForm.Emulator.SystemId}");
				return;
			}
			
			var decoder = new GameSharkDecoder(MainForm.Emulator.AsMemoryDomains(), MainForm.Emulator.SystemId);
			var result = decoder.Decode(code);
			
			if (result.IsValid(out var valid))
			{
				var domain = decoder.CheatDomain();
				MainForm.CheatList.Add(valid.ToCheat(domain, code));
			}
			else
			{
				Log(result.Error);
			}
		}

		[LuaMethodExample("client.removecheat(\"NNNPAK\");")]
		[LuaMethod("removecheat", "removes a cheat, if it already exists")]
		public void RemoveCheat(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return;
			}

			if (!MainForm.Emulator.HasMemoryDomains())
			{
				Log($"cheat codes not supported by the current system: {MainForm.Emulator.SystemId}");
				return;
			}

			var decoder = new GameSharkDecoder(MainForm.Emulator.AsMemoryDomains(), MainForm.Emulator.SystemId);
			var result = decoder.Decode(code);

			if (result.IsValid(out var valid))
			{
				MainForm.CheatList.RemoveRange(
					MainForm.CheatList.Where(c => c.Address == valid.Address));
			}
			else
			{
				Log(result.Error);
			}
		}
	}
}
