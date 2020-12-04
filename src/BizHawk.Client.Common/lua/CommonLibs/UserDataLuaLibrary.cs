using System;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for setting and retrieving dynamic data that will be saved and loaded with savestates")]
	public sealed class UserDataLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public UserDataLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "userdata";

		[LuaMethodExample("userdata.set(\"Unique key\", \"Current key data\");")]
		[LuaMethod("set", "adds or updates the data with the given key with the given value")]
		public void Set(string name, object value) => APIs.UserData.Set(name, value);

		[LuaMethodExample("local obuseget = userdata.get( \"Unique key\" );")]
		[LuaMethod("get", "gets the data with the given key, if the key does not exist it will return nil")]
		public object Get(string key) => APIs.UserData.Get(key);

		[LuaMethodExample("userdata.clear( );")]
		[LuaMethod("clear", "clears all user data")]
		public void Clear() => APIs.UserData.Clear();

		[LuaMethodExample("if ( userdata.remove( \"Unique key\" ) ) then\r\n\tconsole.log( \"remove the data with the given key.Returns true if the element is successfully found and removed; otherwise, false.\" );\r\nend;")]
		[LuaMethod("remove", "remove the data with the given key. Returns true if the element is successfully found and removed; otherwise, false.")]
		public bool Remove(string key) => APIs.UserData.Remove(key);

		[LuaMethodExample("if ( userdata.containskey( \"Unique key\" ) ) then\r\n\tconsole.log( \"returns whether or not there is an entry for the given key\" );\r\nend;")]
		[LuaMethod("containskey", "returns whether or not there is an entry for the given key")]
		public bool ContainsKey(string key) => APIs.UserData.ContainsKey(key);
	}
}
