using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

using NLua;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class Win32LuaLibraries : IPlatformLuaLibEnv
	{
		public Win32LuaLibraries(
			IEmulatorServiceProvider serviceProvider,
			MainForm mainForm,
			DisplayManager displayManager,
			InputManager inputManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			void EnumerateLuaFunctions(string name, Type type, LuaLibraryBase<LuaTable> instance)
			{
				if (instance != null) _lua.NewTable(name);
				foreach (var method in type.GetMethods())
				{
					var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
					if (foundAttrs.Length == 0) continue;
					if (instance != null) _lua.RegisterFunction($"{name}.{((LuaMethodAttribute) foundAttrs[0]).Name}", instance, method);
					Docs.Add(new LibraryFunction(
						name,
						type.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>()
							.Select(descAttr => descAttr.Description).FirstOrDefault() ?? string.Empty,
						method
					));
				}
			}

			if (true /*NLua.Lua.WhichLua == "NLua"*/) _lua["keepalives"] = _lua.NewTable();
			_th = new NLuaTableHelper(_lua);
			_mainForm = mainForm;
			LuaWait = new AutoResetEvent(false);
			Docs.Clear();
			var apiContainer = ApiManager.RestartLua(serviceProvider, LogToLuaConsole, _mainForm, displayManager, inputManager, _mainForm.MovieSession, _mainForm.Tools, config, emulator, game);

			// Register lua libraries
			foreach (var lib in Client.Common.ReflectionCache.Types.Concat(EmuHawk.ReflectionCache.Types)
				.Where(t => typeof(LuaLibraryBase<>).IsAssignableFrom(t) && t.IsSealed && ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				bool addLibrary = true;
				var attributes = lib.GetCustomAttributes(typeof(LuaLibraryAttribute), false);
				if (attributes.Any())
				{
					addLibrary = VersionInfo.DeveloperBuild || ((LuaLibraryAttribute)attributes.First()).Released;
				}

				if (addLibrary)
				{
					var instance = (LuaLibraryBase<LuaTable>) Activator.CreateInstance(lib, this, apiContainer, (Action<string>) LogToLuaConsole);
					ServiceInjector.UpdateServices(serviceProvider, instance);

					// TODO: make EmuHawk libraries have a base class with common properties such as this
					// and inject them here
					if (instance is ClientLuaLibrary<LuaTable> clientLib)
					{
						clientLib.MainForm = _mainForm;
					}
					else if (instance is ConsoleLuaLibrary consoleLib)
					{
						consoleLib.Tools = _mainForm.Tools;
						_logToLuaConsoleCallback = consoleLib.Log;
					}
					else if (instance is GuiLuaLibrary<LuaTable> guiLib)
					{
						guiLib.CreateLuaCanvasCallback = (width, height, x, y) =>
						{
							var canvas = new LuaCanvas<LuaTable>(width, height, x, y, _th, LogToLuaConsole);
							canvas.Show();
							return _th.ObjectToTable(canvas);
						};
					}
					else if (instance is TAStudioLuaLibrary<LuaTable> tastudioLib)
					{
						tastudioLib.Tools = _mainForm.Tools;
					}

					EnumerateLuaFunctions(instance.Name, lib, instance);
					Libraries.Add(lib, instance);
				}
			}

			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			EmulationLuaLibrary.FrameAdvanceCallback = Frameadvance;
			EmulationLuaLibrary.YieldCallback = EmuYield;

			EnumerateLuaFunctions("LuaCanvas", typeof(LuaCanvas<LuaTable>), null); // add LuaCanvas to Lua function reference table
		}

		private readonly MainForm _mainForm;

		private Lua _lua = new Lua();
		private Lua _currThread;

		private readonly NLuaTableHelper _th;

		private static Action<object[]> _logToLuaConsoleCallback = a => Console.WriteLine("a Lua lib is logging during init and the console lib hasn't been initialised yet");

		private FormsLuaLibrary<LuaTable> FormsLibrary => (FormsLuaLibrary<LuaTable>) Libraries[typeof(FormsLuaLibrary<LuaTable>)];

		private EventsLuaLibrary<LuaTable> EventsLibrary => (EventsLuaLibrary<LuaTable>) Libraries[typeof(EventsLuaLibrary<LuaTable>)];

		public LuaDocumentation Docs { get; } = new LuaDocumentation();

		private EmulationLuaLibrary<LuaTable> EmulationLuaLibrary => (EmulationLuaLibrary<LuaTable>) Libraries[typeof(EmulationLuaLibrary<LuaTable>)];

		public string EngineName => Lua.WhichLua;

		public GuiLuaLibrary<LuaTable> GuiLibrary => (GuiLuaLibrary<LuaTable>) Libraries[typeof(GuiLuaLibrary<LuaTable>)];

		public bool IsRebootingCore { get; set; }

		public bool IsUpdateSupressed { get; set; }

		private readonly IDictionary<Type, LuaLibraryBase<LuaTable>> Libraries = new Dictionary<Type, LuaLibraryBase<LuaTable>>();

		private EventWaitHandle LuaWait;

		public LuaFileList ScriptList { get; } = new LuaFileList();

		private static void LogToLuaConsole(object outputs) => _logToLuaConsoleCallback(new[] { outputs });

		public ILuaTableHelper<LuaTable> GetTableHelper() => _th;

		public void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			foreach (var lib in Libraries)
			{
				ServiceInjector.UpdateServices(newServiceProvider, lib.Value);
			}
		}

		public void StartLuaDrawing()
		{
			if (ScriptList.Count != 0 && GuiLibrary.SurfaceIsNull && !IsUpdateSupressed)
			{
				GuiLibrary.DrawNew("emu");
			}
		}

		public void EndLuaDrawing()
		{
			if (ScriptList.Count != 0 && !IsUpdateSupressed)
			{
				GuiLibrary.DrawFinish();
			}
		}

		public bool FrameAdvanceRequested { get; private set; }

		public LuaFunctionList RegisteredFunctions { get; } = new LuaFunctionList();

		public void CallSaveStateEvent(string name)
		{
			try
			{
				foreach (var lf in RegisteredFunctions.Where(l => l.Event == "OnSavestateSave"))
				{
					lf.Call(name);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onsavestate\nError message: {e.Message}");
			}
		}

		public void CallLoadStateEvent(string name)
		{
			try
			{
				foreach (var lf in RegisteredFunctions.Where(l => l.Event == "OnSavestateLoad"))
				{
					lf.Call(name);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onloadstate\nError message: {e.Message}");
			}
		}

		public void CallFrameBeforeEvent()
		{
			if (IsUpdateSupressed) return;
			try
			{
				foreach (var lf in RegisteredFunctions.Where(l => l.Event == "OnFrameStart"))
				{
					lf.Call();
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onframestart\nError message: {e.Message}");
			}
		}

		public void CallFrameAfterEvent()
		{
			if (IsUpdateSupressed) return;
			try
			{
				foreach (var lf in RegisteredFunctions.Where(l => l.Event == "OnFrameEnd"))
				{
					lf.Call();
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onframeend\nError message: {e.Message}");
			}
		}

		public void CallExitEvent(LuaFile lf)
		{
			foreach (var exitCallback in RegisteredFunctions.ForFile(lf).ForEvent("OnExit"))
			{
				exitCallback.Call();
			}
		}

		public void Close()
		{
			RegisteredFunctions.Clear(_mainForm.Emulator);
			ScriptList.Clear();
			FormsLibrary.DestroyAll();
			_lua.Close();
			_lua = new Lua();
			GuiLibrary.Dispose();
		}

		public INamedLuaFunction CreateAndRegisterNamedFunction(
			Func<IReadOnlyList<object>, IReadOnlyList<object>> function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			string name = null)
		{
			var nlf = new NamedLuaFunction(function, theEvent, logCallback, luaFile, name);
			RegisteredFunctions.Add(nlf);
			return nlf;
		}

		public bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate)
		{
			var nlf = (NamedLuaFunction) RegisteredFunctions.FirstOrDefault(predicate);
			if (nlf == null) return false;
			RegisteredFunctions.Remove(nlf, _mainForm.Emulator);
			return true;
		}

		public Lua SpawnCoroutine(string file)
		{
			var lua = _lua.NewThread();
			var content = File.ReadAllText(file);
			var main = lua.LoadString(content, "main");
			lua.Push(main); // push main function on to stack for subsequent resuming
			if (true /*NLua.Lua.WhichLua == "NLua"*/)
			{
				_lua.GetTable("keepalives")[lua] = 1;
				//this not being run is the origin of a memory leak if you restart scripts too many times
				_lua.Pop();
			}
			return lua;
		}

		public void SpawnAndSetFileThread(string pathToLoad, LuaFile lf)
		{
			lf.Thread = SpawnCoroutine(pathToLoad);
		}

		public void ExecuteString(string command)
		{
			_currThread = _lua.NewThread();
			_currThread.DoString(command);
			if (true /*NLua.Lua.WhichLua == "NLua"*/) _lua.Pop();
		}

		public void RunScheduledDisposes() => _lua.RunScheduledDisposes();

		public (bool WaitForFrame, bool Terminated) ResumeScript(LuaFile lf)
		{
			_currThread = lf.Thread;

			try
			{
				LuaLibraryBase<LuaTable>.SetCurrentThread(lf);

				var execResult = _currThread.Resume(0);

				_lua.RunScheduledDisposes(); // TODO: I don't think this is needed anymore, we run this regularly anyway

				// not sure how this is going to work out, so do this too
				_currThread.RunScheduledDisposes();

				_currThread = null;
				var result = execResult == 0
					? (WaitForFrame: false, Terminated: true) // terminated
					: (WaitForFrame: FrameAdvanceRequested, Terminated: false); // yielded

				FrameAdvanceRequested = false;
				return result;
			}
			finally
			{
				LuaLibraryBase<LuaTable>.ClearCurrentThread();
			}
		}

		public static void Print(params object[] outputs)
		{
			_logToLuaConsoleCallback(outputs);
		}

		private void Frameadvance()
		{
			FrameAdvanceRequested = true;
			_currThread.Yield(0);
		}

		private void EmuYield()
		{
			_currThread.Yield(0);
		}

		public Func<IReadOnlyList<object>, IReadOnlyList<object>> WrapFunction(object luaFunction)
			=> args => ((LuaFunction) luaFunction).Call(args);
	}
}
