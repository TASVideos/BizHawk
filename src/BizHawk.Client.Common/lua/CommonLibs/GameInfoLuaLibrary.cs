using System;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	public sealed class GameInfoLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public GameInfoLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "gameinfo";

		[LuaMethodExample("local stgamget = gameinfo.getromname( );")]
		[LuaMethod("getromname", "returns the name of the currently loaded rom, if a rom is loaded")]
		public string GetRomName() => APIs.GameInfo.GetRomName();

		[LuaMethodExample("local stgamget = gameinfo.getromhash( );")]
		[LuaMethod("getromhash", "returns the hash of the currently loaded rom, if a rom is loaded")]
		public string GetRomHash() => APIs.GameInfo.GetRomHash();

		[LuaMethodExample("if ( gameinfo.indatabase( ) ) then\r\n\tconsole.log( \"returns whether or not the currently loaded rom is in the game database\" );\r\nend;")]
		[LuaMethod("indatabase", "returns whether or not the currently loaded rom is in the game database")]
		public bool InDatabase() => APIs.GameInfo.InDatabase();

		[LuaMethodExample("local stgamget = gameinfo.getstatus( );")]
		[LuaMethod("getstatus", "returns the game database status of the currently loaded rom. Statuses are for example: GoodDump, BadDump, Hack, Unknown, NotInDatabase")]
		public string GetStatus() => APIs.GameInfo.GetStatus();

		[LuaMethodExample("if ( gameinfo.isstatusbad( ) ) then\r\n\tconsole.log( \"returns the currently loaded rom's game database status is considered 'bad'\" );\r\nend;")]
		[LuaMethod("isstatusbad", "returns the currently loaded rom's game database status is considered 'bad'")]
		public bool IsStatusBad() => APIs.GameInfo.IsStatusBad();

		[LuaMethodExample("local stgamget = gameinfo.getboardtype( );")]
		[LuaMethod("getboardtype", "returns identifying information about the 'mapper' or similar capability used for this game.  empty if no such useful distinction can be drawn")]
		public string GetBoardType() => APIs.GameInfo.GetBoardType();

		[LuaMethodExample("local nlgamget = gameinfo.getoptions( );")]
		[LuaMethod("getoptions", "returns the game options for the currently loaded rom. Options vary per platform")]
		public TTable GetOptions() => _th.DictToTable(APIs.GameInfo.GetOptions());
	}
}
