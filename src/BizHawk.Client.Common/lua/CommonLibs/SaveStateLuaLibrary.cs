using System;

namespace BizHawk.Client.Common
{
	public sealed class SaveStateLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public SaveStateLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "savestate";

		[LuaMethodExample("savestate.load( \"C:\\state.bin\" );")]
		[LuaMethod("load", "Loads a savestate with the given path. If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes (and the path is ignored).")]
		public void Load(string path, bool suppressOSD = false)
		{
			_luaLibsImpl.IsUpdateSupressed = true;

			APIs.SaveState.Load(path, suppressOSD);
			
			_luaLibsImpl.IsUpdateSupressed = false;
		}

		[LuaMethodExample("savestate.loadslot( 7 );")]
		[LuaMethod("loadslot", "Loads the savestate at the given slot number (must be an integer between 0 and 9). If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes with the slot number.")]
		public void LoadSlot(int slotNum, bool suppressOSD = false)
		{
			_luaLibsImpl.IsUpdateSupressed = true;

			APIs.SaveState.LoadSlot(slotNum, suppressOSD);
			
			_luaLibsImpl.IsUpdateSupressed = false;
		}

		[LuaMethodExample("savestate.save( \"C:\\state.bin\" );")]
		[LuaMethod("save", "Saves a state at the given path. If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes (and the path is ignored).")]
		public void Save(string path, bool suppressOSD = false) => APIs.SaveState.Save(path, suppressOSD);

		[LuaMethodExample("savestate.saveslot( 7 );")]
		[LuaMethod("saveslot", "Saves a state at the given save slot (must be an integer between 0 and 9). If EmuHawk is deferring quicksaves, to TAStudio for example, that form will do what it likes with the slot number.")]
		public void SaveSlot(int slotNum, bool suppressOSD = false) => APIs.SaveState.SaveSlot(slotNum, suppressOSD);
	}
}
