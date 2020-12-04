using System;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	public sealed class MemorySavestateLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public MemorySavestateLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "memorysavestate";

		[LuaMethodExample("local mmsvstsvcst = memorysavestate.savecorestate( );")]
		[LuaMethod("savecorestate", "creates a core savestate and stores it in memory.  Note: a core savestate is only the raw data from the core, and not extras such as movie input logs, or framebuffers. Returns a unique identifer for the savestate")]
		public string SaveCoreStateToMemory() => APIs.MemorySaveState.SaveCoreStateToMemory();

		[LuaMethodExample("memorysavestate.loadcorestate( \"3fcf120f-0778-43fd-b2c5-460fb7d34184\" );")]
		[LuaMethod("loadcorestate", "loads an in memory state with the given identifier")]
		public void LoadCoreStateFromMemory(string identifier) => APIs.MemorySaveState.LoadCoreStateFromMemory(identifier);

		[LuaMethodExample("memorysavestate.removestate( \"3fcf120f-0778-43fd-b2c5-460fb7d34184\" );")]
		[LuaMethod("removestate", "removes the savestate with the given identifier from memory")]
		public void DeleteState(string identifier) => APIs.MemorySaveState.DeleteState(identifier);

		[LuaMethodExample("memorysavestate.clearstatesfrommemory( );")]
		[LuaMethod("clearstatesfrommemory", "clears all savestates stored in memory")]
		public void ClearInMemoryStates() => APIs.MemorySaveState.ClearInMemoryStates();
	}
}
