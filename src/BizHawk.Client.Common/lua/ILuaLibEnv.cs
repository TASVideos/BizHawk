using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface ILuaLibEnv<TTable>
	{
		LuaDocumentation Docs { get; }

		string EngineName { get; }

		/// <remarks>pretty hacky... we don't want a lua script to be able to restart itself by rebooting the core</remarks>
		bool IsRebootingCore { get; set; }

		bool IsUpdateSupressed { get; set; }

		INamedLuaFunction CreateAndRegisterNamedFunction(Func<IReadOnlyList<object>, IReadOnlyList<object>> function, string theEvent, Action<string> logCallback, LuaFile luaFile, string name = null);

		ILuaTableHelper<TTable> GetTableHelper();

		bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate);

		Func<IReadOnlyList<object>, IReadOnlyList<object>> WrapFunction(object luaFunction);
	}
}
