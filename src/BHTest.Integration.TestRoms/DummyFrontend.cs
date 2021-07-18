using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BHTest.Integration.TestRoms
{
	public sealed class DummyFrontend : IDisposable
	{
		public sealed class EmbeddedFirmwareProvider : ICoreFileProvider
		{
			public readonly IDictionary<FirmwareID, string> EmbedPathMap;

			public EmbeddedFirmwareProvider(IDictionary<FirmwareID, string>? embedPathMap = null)
				=> EmbedPathMap = embedPathMap ?? new Dictionary<FirmwareID, string>();

			/// <returns><see langword="true"/> iff succeeded</returns>
			public bool AddIfExists(FirmwareID id, string embedPath)
			{
				var exists = ReflectionCache.EmbeddedResourceList().Contains(embedPath);
				if (exists) EmbedPathMap[id] = embedPath;
				return exists;
			}

			public string DllPath()
				=> throw new NotImplementedException();

			public byte[] GetFirmware(FirmwareID id, bool required, string? msg)
			{
				var embedPath = EmbedPathMap[id];
				Stream embeddedResourceStream;
				try
				{
					embeddedResourceStream = ReflectionCache.EmbeddedResourceStream(embedPath);
				}
				catch (Exception)
				{
					throw new Exception($"failed to open resource at {embedPath}, is it present in $(ProjectDir)/res?");
				}
				return embeddedResourceStream.ReadAllBytes();
			}

			public byte[] GetFirmwareWithGameInfo(FirmwareID id, bool required, out GameInfo gi, string? msg)
				=> throw new NotImplementedException();

			public string GetRetroSaveRAMDirectory(IGameInfo game)
				=> throw new NotImplementedException();

			public string GetRetroSystemPath(IGameInfo game)
				=> throw new NotImplementedException();
		}

		private sealed class FakeGraphicsControl : IGraphicsControl
		{
			private readonly IGL_GdiPlus _gdi;

			private readonly Func<(int, int)> _getVirtualSize;

			public Rectangle ClientRectangle
			{
				get
				{
					var (w, h) = _getVirtualSize();
					return new(0, 0, w, h);
				}
			}

			public RenderTargetWrapper? RenderTargetWrapper { get; set; }

			public FakeGraphicsControl(IGL_GdiPlus glImpl, Func<(int Width, int Height)> getVirtualSize)
			{
				_gdi = glImpl;
				_getVirtualSize = getVirtualSize;
			}

			public void Begin()
			{
				_gdi.BeginControl(this);
				RenderTargetWrapper!.CreateGraphics();
			}

			public Graphics CreateGraphics()
			{
				var (w, h) = _getVirtualSize();
				return Graphics.FromImage(new Bitmap(w, h));
			}

			public void End() => _gdi.EndControl(this);

			public void SetVsync(bool state) {}

			public void SwapBuffers()
			{
				_gdi.SwapControl(this);
				if (RenderTargetWrapper!.MyBufferedGraphics is null) return;
				RenderTargetWrapper.CreateGraphics();
			}

			public void Dispose() {}
		}

		public sealed class SimpleGDIPDisplayManager : DisplayManagerBase
		{
			private readonly FakeGraphicsControl _gc;

			private SimpleGDIPDisplayManager(Config config, IEmulator emuCore, IGL_GdiPlus glImpl)
				: base(config, emuCore, inputManager: null, movieSession: null, EDispMethod.GdiPlus, glImpl, new GDIPlusGuiRenderer(glImpl))
			{
				_gc = (FakeGraphicsControl) glImpl.Internal_CreateGraphicsControl();
				Blank();
			}

			public SimpleGDIPDisplayManager(Config config, IEmulator emuCore, Func<(int Width, int Height)> getVirtualSize)
				: this(config, emuCore, new IGL_GdiPlus(self => new FakeGraphicsControl(self, getVirtualSize))) {}

			protected override void ActivateGLContext() => _gc.Begin();

			protected override void SwapBuffersOfGraphicsControl() => _gc.SwapBuffers();
		}

		/// <summary>
		/// set-up firmwares on <paramref name="efp"/>, optionally setting <paramref name="config"/>, then
		/// initialise and return a core instance (<paramref name="coreComm"/> is provided),
		/// and optionally specify a frame number to seek to (e.g. to skip BIOS screens)
		/// </summary>
		public delegate (IEmulator newCore, int biosWaitDuration) ClassInitCallbackDelegate(
			EmbeddedFirmwareProvider efp,
			Config config,
			CoreComm coreComm);

		public static Bitmap RunAndScreenshot(ClassInitCallbackDelegate init, Action<DummyFrontend> run)
		{
			using DummyFrontend fe = new(init);
			run(fe);
			return fe.Screenshot();
		}

		private readonly Config _config = new();

		private readonly SimpleController _controller;

		private readonly IVideoProvider _coreAsVP;

		private readonly SimpleGDIPDisplayManager _dispMan;

		public readonly IEmulator Core;

		public readonly IDebuggable? CoreAsDebuggable;

		public readonly IMemoryDomains? CoreAsMemDomains;

		public int FrameCount => Core.Frame;

		/// <seealso cref="ClassInitCallbackDelegate"/>
		public DummyFrontend(ClassInitCallbackDelegate init)
		{
			EmbeddedFirmwareProvider efp = new();
			var (core, biosWaitDuration) = init(
				efp,
				_config,
				new(Console.WriteLine, Console.WriteLine, efp, CoreComm.CorePreferencesFlags.None));
			Core = core;
			_controller = new() { Definition = Core.ControllerDefinition };
			while (Core.Frame < biosWaitDuration) Core.FrameAdvance(_controller, render: false, renderSound: false);
			CoreAsDebuggable = Core.CanDebug() ? Core.AsDebuggable() : null;
			CoreAsMemDomains = Core.HasMemoryDomains() ? Core.AsMemoryDomains() : null;
			_coreAsVP = core.AsVideoProvider();
			_dispMan = new(_config, core, () => (_coreAsVP!.VirtualWidth, _coreAsVP!.VirtualHeight));
		}

		public void Dispose() => _dispMan.Dispose();

		public void FrameAdvance() => Core.FrameAdvance(_controller, render: false, renderSound: false);

		public void FrameAdvanceBy(int numFrames) => FrameAdvanceTo(FrameCount + numFrames);

		/// <returns>last return of <paramref name="pred"/> (will be <see langword="false"/> iff timed out)</returns>
		/// <remarks><paramref name="timeoutAtFrame"/> is NOT relative to current frame count</remarks>
		public bool FrameAdvanceUntil(Func<bool> pred, int timeoutAtFrame = 500)
		{
			while (!pred() && FrameCount < timeoutAtFrame) FrameAdvance();
			return FrameCount < timeoutAtFrame;
		}

		public void FrameAdvanceTo(int frame)
		{
			while (FrameCount < frame) FrameAdvance();
		}

		public Bitmap Screenshot() => _dispMan.RenderVideoProvider(_coreAsVP).ToSysdrawingBitmap();

		public void SetButton(string buttonName) => _controller[buttonName] = true;
	}
}
