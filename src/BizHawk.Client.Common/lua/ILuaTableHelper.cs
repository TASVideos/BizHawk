using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface ILuaTableHelper<TTable>
	{
		TTable CreateTable();

		TTable DictToTable<T>(IDictionary<string, T> dictionary);

		IEnumerable<(TKey Key, TValue Value)> EnumerateEntries<TKey, TValue>(TTable table);

		IEnumerable<T> EnumerateValues<T>(TTable table);

		TTable ListToTable<T>(IList<T> list, int indexFrom = 0);

		TTable ObjectToTable(object obj);
	}
}
