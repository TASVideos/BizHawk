using System;

using NLua;

namespace BizHawk.Client.Common
{
	/// <summary>Extends <see cref="LuaLibraryBase"/> by including an <see cref="ApiContainer"/> for the library to delegate its calls through.</summary>
	public abstract class DelegatingLuaLibrary : LuaLibraryBase
	{
		protected DelegatingLuaLibrary(LuaLibraries luaLibsImpl, Lua lua, Action<string> logOutputCallback)
			: base(luaLibsImpl, lua, logOutputCallback) {}

		public ApiContainer APIs { protected get; set; }
	}
}
