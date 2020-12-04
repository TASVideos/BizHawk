using NLua;

namespace BizHawk.Client.Common
{
	public interface IPlatformLuaLibEnv : ILuaLibEnv<LuaTable>
	{
		LuaFunctionList RegisteredFunctions { get; }

		LuaFileList ScriptList { get; }

		void CallLoadStateEvent(string name);

		void CallSaveStateEvent(string name);

		void SpawnAndSetFileThread(string pathToLoad, LuaFile lf);
	}
}