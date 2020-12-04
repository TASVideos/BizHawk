using System;
using System.ComponentModel;

using BizHawk.Emulation.Cores.Nintendo.SNES;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("Functions specific to SNESHawk (functions may not run when an SNES game is not loaded)")]
	public sealed class SNESLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public SNESLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "snes";

		private LibsnesCore.SnesSettings Settings
		{
			get => APIs.Emulation.GetSettings() as LibsnesCore.SnesSettings ?? new LibsnesCore.SnesSettings();
			set => APIs.Emulation.PutSettings(value);
		}

		[LuaMethodExample("if ( snes.getlayer_bg_1( ) ) then\r\n\tconsole.log( \"Returns whether the bg 1 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bg_1", "Returns whether the bg 1 layer is displayed")]
		public bool GetLayerBg1()
		{
			return Settings.ShowBG1_1;
		}

		[LuaMethodExample("if ( snes.getlayer_bg_2( ) ) then\r\n\tconsole.log( \"Returns whether the bg 2 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bg_2", "Returns whether the bg 2 layer is displayed")]
		public bool GetLayerBg2()
		{
			return Settings.ShowBG2_1;
		}

		[LuaMethodExample("if ( snes.getlayer_bg_3( ) ) then\r\n\tconsole.log( \"Returns whether the bg 3 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bg_3", "Returns whether the bg 3 layer is displayed")]
		public bool GetLayerBg3()
		{
			return Settings.ShowBG3_1;
		}

		[LuaMethodExample("if ( snes.getlayer_bg_4( ) ) then\r\n\tconsole.log( \"Returns whether the bg 4 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bg_4", "Returns whether the bg 4 layer is displayed")]
		public bool GetLayerBg4()
		{
			return Settings.ShowBG4_1;
		}

		[LuaMethodExample("if ( snes.getlayer_obj_1( ) ) then\r\n\tconsole.log( \"Returns whether the obj 1 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_obj_1", "Returns whether the obj 1 layer is displayed")]
		public bool GetLayerObj1()
		{
			return Settings.ShowOBJ_0;
		}

		[LuaMethodExample("if ( snes.getlayer_obj_2( ) ) then\r\n\tconsole.log( \"Returns whether the obj 2 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_obj_2", "Returns whether the obj 2 layer is displayed")]
		public bool GetLayerObj2()
		{
			return Settings.ShowOBJ_1;
		}

		[LuaMethodExample("if ( snes.getlayer_obj_3( ) ) then\r\n\tconsole.log( \"Returns whether the obj 3 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_obj_3", "Returns whether the obj 3 layer is displayed")]
		public bool GetLayerObj3()
		{
			return Settings.ShowOBJ_2;
		}

		[LuaMethodExample("if ( snes.getlayer_obj_4( ) ) then\r\n\tconsole.log( \"Returns whether the obj 4 layer is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_obj_4", "Returns whether the obj 4 layer is displayed")]
		public bool GetLayerObj4()
		{
			return Settings.ShowOBJ_3;
		}

		[LuaMethodExample("snes.setlayer_bg_1( true );")]
		[LuaMethod("setlayer_bg_1", "Sets whether the bg 1 layer is displayed")]
		public void SetLayerBg1(bool value)
		{
			var s = Settings;
			s.ShowBG1_1 = s.ShowBG1_0 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_bg_2( true );")]
		[LuaMethod("setlayer_bg_2", "Sets whether the bg 2 layer is displayed")]
		public void SetLayerBg2(bool value)
		{
			var s = Settings;
			s.ShowBG2_1 = s.ShowBG2_0 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_bg_3( true );")]
		[LuaMethod("setlayer_bg_3", "Sets whether the bg 3 layer is displayed")]
		public void SetLayerBg3(bool value)
		{
			var s = Settings;
			s.ShowBG3_1 = s.ShowBG3_0 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_bg_4( true );")]
		[LuaMethod("setlayer_bg_4", "Sets whether the bg 4 layer is displayed")]
		public void SetLayerBg4(bool value)
		{
			var s = Settings;
			s.ShowBG4_1 = s.ShowBG4_0 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_obj_1( true );")]
		[LuaMethod("setlayer_obj_1", "Sets whether the obj 1 layer is displayed")]
		public void SetLayerObj1(bool value)
		{
			var s = Settings;
			s.ShowOBJ_0 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_obj_2( true );")]
		[LuaMethod("setlayer_obj_2", "Sets whether the obj 2 layer is displayed")]
		public void SetLayerObj2(bool value)
		{
			var s = Settings;
			s.ShowOBJ_1 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_obj_3( true );")]
		[LuaMethod("setlayer_obj_3", "Sets whether the obj 3 layer is displayed")]
		public void SetLayerObj3(bool value)
		{
			var s = Settings;
			s.ShowOBJ_2 = value;
			Settings = s;
		}

		[LuaMethodExample("snes.setlayer_obj_4( true );")]
		[LuaMethod("setlayer_obj_4", "Sets whether the obj 4 layer is displayed")]
		public void SetLayerObj4(bool value)
		{
			var s = Settings;
			s.ShowOBJ_3 = value;
			Settings = s;
		}
	}
}
