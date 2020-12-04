using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

using NLua;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Methods intentionally blank.
	/// </summary>
	public sealed class UnixLuaLibraries : IPlatformLuaLibEnv
	{
		private static readonly LuaFunctionList EmptyLuaFunList = new LuaFunctionList();

		public LuaDocumentation Docs { get; } = new LuaDocumentation();

		public string EngineName => null;

		public bool IsRebootingCore { get; set; }

		public bool IsUpdateSupressed { get; set; }

		public LuaFunctionList RegisteredFunctions => EmptyLuaFunList;

		public LuaFileList ScriptList { get; } = new LuaFileList();

		public void CallLoadStateEvent(string name) {}

		public void CallSaveStateEvent(string name) {}

		public INamedLuaFunction CreateAndRegisterNamedFunction(
			Func<IReadOnlyList<object>, IReadOnlyList<object>> function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			string name = null)
				=> null;

		public ILuaTableHelper<LuaTable> GetTableHelper() => null;

		public bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate) => false;

		public void SpawnAndSetFileThread(string pathToLoad, LuaFile lf) {}

		public Func<IReadOnlyList<object>, IReadOnlyList<object>> WrapFunction(object luaFunction)
			=> args => Array.Empty<object>();
	}
}