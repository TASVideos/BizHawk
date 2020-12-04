using System;
using System.Collections.Generic;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for performing SQLite operations.")]
	public sealed class SQLiteLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public SQLiteLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "SQL";

		[LuaMethodExample("local stSQLcre = SQL.createdatabase( \"eg_db\" );")]
		[LuaMethod("createdatabase", "Creates a SQLite Database. Name should end with .db")]
		public string CreateDatabase(string name) => APIs.SQLite.CreateDatabase(name);

		[LuaMethodExample("local stSQLope = SQL.opendatabase( \"eg_db\" );")]
		[LuaMethod("opendatabase", "Opens a SQLite database. Name should end with .db")]
		public string OpenDatabase(string name) => APIs.SQLite.OpenDatabase(name);

		[LuaMethodExample("local stSQLwri = SQL.writecommand( \"CREATE TABLE eg_tab ( eg_tab_id integer PRIMARY KEY, eg_tab_row_name text NOT NULL ); INSERT INTO eg_tab ( eg_tab_id, eg_tab_row_name ) VALUES ( 1, 'Example table row' );\" );")]
		[LuaMethod("writecommand", "Runs a SQLite write command which includes CREATE,INSERT, UPDATE. " +
			"Ex: create TABLE rewards (ID integer  PRIMARY KEY, action VARCHAR(20)) ")]
		public string WriteCommand(string query = "") => APIs.SQLite.WriteCommand(query);

		[LuaMethodExample("local obSQLrea = SQL.readcommand( \"SELECT * FROM eg_tab WHERE eg_tab_id = 1;\" );")]
		[LuaMethod("readcommand", "Run a SQLite read command which includes Select. Returns all rows into a LuaTable." +
			"Ex: select * from rewards")]
		public object ReadCommand(string query = "")
		{
			var result = APIs.SQLite.ReadCommand(query);
			return result is Dictionary<string, object> dict ? _th.DictToTable(dict) : result;
		}
	}
}
