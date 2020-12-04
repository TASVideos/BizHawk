using System.Collections.Generic;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class NLuaTableHelper : ILuaTableHelper<LuaTable>
	{
		private readonly Lua _lua;

		public NLuaTableHelper(Lua lua) => _lua = lua;

		public LuaTable CreateTable() => _lua.NewTable();

		public LuaTable DictToTable<T>(IDictionary<string, T> dictionary)
		{
			var table = _lua.NewTable();

			foreach (var kvp in dictionary)
			{
				table[kvp.Key] = kvp.Value;
			}

			return table;
		}

		public IEnumerable<(TKey Key, TValue Value)> EnumerateEntries<TKey, TValue>(LuaTable table)
			=> table.Keys.Cast<TKey>().Select(k => (k, (TValue) table[k]));

		public IEnumerable<T> EnumerateValues<T>(LuaTable table) => table.Values.Cast<T>();

		public LuaTable ListToTable<T>(IList<T> list, int indexFrom = 0)
		{
			var table = _lua.NewTable();
			for (int i = 0, l = list.Count; i != l; i++) table[indexFrom + i] = list[i];
			return table;
		}

		public LuaTable ObjectToTable(object obj)
		{
			var table = _lua.NewTable();
			foreach (var method in obj.GetType().GetMethods())
			{
				if (!method.IsPublic) continue;
				var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
				table[method.Name] = _lua.RegisterFunction(
					foundAttrs.Length == 0 ? string.Empty : ((LuaMethodAttribute) foundAttrs[0]).Name, // empty string will default to the actual method name
					obj,
					method
				);
			}
			return table;
		}
	}
}
