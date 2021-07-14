using System;
using System.Drawing;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class EmuClientApi : IEmuClientApi
	{
		private readonly Config _config;

		private readonly DisplayManagerBase _displayManager;

		private readonly IMainFormForApi _mainForm;

		private readonly Action<string> _logCallback;

		private readonly IEmulator Emulator;

		private readonly IGameInfo Game;

		public static readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();

		private readonly IVideoProvider VideoProvider;

		public event BeforeQuickLoadEventHandler BeforeQuickLoad;

		public event BeforeQuickSaveEventHandler BeforeQuickSave;

		public event EventHandler RomLoaded;

		public event StateLoadedEventHandler StateLoaded;

		public event StateSavedEventHandler StateSaved;

		public EmuClientApi(Action<string> logCallback, IMainFormForApi mainForm, DisplayManagerBase displayManager, Config config, IEmulator emulator, IGameInfo game)
		{
			_config = config;
			_displayManager = displayManager;
			Emulator = emulator;
			Game = game;
			_logCallback = logCallback;
			_mainForm = mainForm;
			VideoProvider = Emulator.AsVideoProviderOrDefault();
		}

		public int BorderHeight() => _displayManager.TransformPoint(new Point(0, 0)).Y;

		public int BorderWidth() => _displayManager.TransformPoint(new Point(0, 0)).X;

		public int BufferHeight() => VideoProvider.BufferHeight;

		public int BufferWidth() => VideoProvider.BufferWidth;

		public void ClearAutohold() => _mainForm.ClearHolds();

		public void CloseEmulator(int? exitCode = null) => _mainForm.CloseEmulator(exitCode);

		public void CloseRom() => _mainForm.CloseRom();

		public void DisplayMessages(bool value) => _config.DisplayMessages = value;

		public void DoFrameAdvance()
		{
			_mainForm.FrameAdvance();
			_mainForm.StepRunLoop_Throttle();
			_mainForm.Render();
		}

		public void DoFrameAdvanceAndUnpause()
		{
			DoFrameAdvance();
			Unpause();
		}

		public void EnableRewind(bool enabled) => _mainForm.EnableRewind(enabled);

		public void FrameSkip(int numFrames)
		{
			if (numFrames < 0)
			{
				_logCallback("Invalid frame skip value");
				return;
			}

			_config.FrameSkip = numFrames;
			_mainForm.FrameSkipMessage();
		}

		public int GetApproxFramerate() => _mainForm.GetApproxFramerate();

		public bool GetSoundOn() => _config.SoundEnabled;

		public int GetTargetScanlineIntensity() => _config.TargetScanlineFilterIntensity;

		public int GetWindowSize() => _config.TargetZoomFactors[Emulator.SystemId];

		public void InvisibleEmulation(bool invisible) => _mainForm.InvisibleEmulation = invisible;

		public bool IsPaused() => _mainForm.EmulatorPaused;

		public bool IsSeeking() => _mainForm.IsSeeking;

		public bool IsTurbo() => _mainForm.IsTurboing;

		public void LoadState(string name) => _mainForm.LoadState(Path.Combine(_config.PathEntries.SaveStateAbsolutePath(Game.System), $"{name}.State"), name, suppressOSD: false);

		public void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			if (BeforeQuickLoad == null)
			{
				eventHandled = false;
				return;
			}
			var e = new BeforeQuickLoadEventArgs(quickSaveSlotName);
			BeforeQuickLoad(sender, e);
			eventHandled = e.Handled;
		}

		public void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			if (BeforeQuickSave == null)
			{
				eventHandled = false;
				return;
			}
			var e = new BeforeQuickSaveEventArgs(quickSaveSlotName);
			BeforeQuickSave(sender, e);
			eventHandled = e.Handled;
		}

		public void OnRomLoaded()
		{
			RomLoaded?.Invoke(null, EventArgs.Empty);
		}

		public void OnStateLoaded(object sender, string stateName) => StateLoaded?.Invoke(sender, new StateLoadedEventArgs(stateName));

		public void OnStateSaved(object sender, string stateName) => StateSaved?.Invoke(sender, new StateSavedEventArgs(stateName));

		public void OpenRom(string path) => _mainForm.LoadRom(path, new LoadRomArgs { OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path) });

		public void Pause() => _mainForm.PauseEmulator();

		public void PauseAv() => _mainForm.PauseAvi = true;

		public void RebootCore() => _mainForm.RebootCore();

		public void SaveRam() => _mainForm.FlushSaveRAM();

		public void SaveState(string name) => _mainForm.SaveState(Path.Combine(_config.PathEntries.SaveStateAbsolutePath(Game.System), $"{name}.State"), name, fromLua: false);

		public int ScreenHeight() => _displayManager.GetPanelNativeSize().Height;

		public void Screenshot(string path)
		{
			if (path == null) _mainForm.TakeScreenshot();
			else _mainForm.TakeScreenshot(path);
		}

		public void ScreenshotToClipboard() => _mainForm.TakeScreenshotToClipboard();

		public int ScreenWidth() => _displayManager.GetPanelNativeSize().Width;

		public void SeekFrame(int frame)
		{
			var wasPaused = _mainForm.EmulatorPaused;
			while (Emulator.Frame != frame) _mainForm.SeekFrameAdvance();
			if (!wasPaused) _mainForm.UnpauseEmulator();
		}

		public void SetClientExtraPadding(int left, int top, int right, int bottom)
		{
			_displayManager.ClientExtraPadding = (left, top, right, bottom);
			_mainForm.FrameBufferResized();
		}

		public void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			_displayManager.GameExtraPadding = (left, top, right, bottom);
			_mainForm.FrameBufferResized();
		}

		public void SetScreenshotOSD(bool value) => _config.ScreenshotCaptureOsd = value;

		public void SetSoundOn(bool enable)
		{
			if (enable != _config.SoundEnabled) _mainForm.ToggleSound();
		}

		public void SetTargetScanlineIntensity(int val) => _config.TargetScanlineFilterIntensity = val;

		public void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				_config.TargetZoomFactors[Emulator.SystemId] = size;
				_mainForm.FrameBufferResized();
				_mainForm.AddOnScreenMessage($"Window size set to {size}x");
			}
			else
			{
				_logCallback("Invalid window size");
			}
		}

		public void SpeedMode(int percent)
		{
			if (percent.StrictlyBoundedBy(0.RangeTo(6400))) _mainForm.ClickSpeedItem(percent);
			else _logCallback("Invalid speed value");
		}

		public void TogglePause() => _mainForm.TogglePause();

		public Point TransformPoint(Point point) => _displayManager.TransformPoint(point);

		public void Unpause() => _mainForm.UnpauseEmulator();

		public void UnpauseAv() => _mainForm.PauseAvi = false;

		public int Xpos() => _mainForm.DesktopLocation.X;

		public int Ypos() => _mainForm.DesktopLocation.Y;
	}
}
