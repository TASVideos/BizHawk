using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		/// <remarks><c>-0x1FFFFFFFFFFFFF..0x1FFFFFFFFFFFFF</c></remarks>
		private static readonly Range<double> PrecisionLimits = (-9007199254740991.0).RangeTo(9007199254740991.0);

		/// <remarks>
		/// Lua numbers are always double-length floats, so integers whose magnitude is at least 2^53 may not fit in the 53-bit mantissa (the sign is stored separately).
		/// These extremely large values aren't that useful, so we'll just assume they're erroneous and give the script author an error.
		/// </remarks>
		/// <exception cref="ArithmeticException"><paramref name="d"/> ≥ 2^53 or <paramref name="d"/> ≤ -2^53</exception>
		public static long AsInteger(this double d) => PrecisionLimits.Contains(d) ? (long) d : throw new ArithmeticException("integer value exceeds the precision of Lua's integer-as-double");

		public static TTable EnumerateToLuaTable<T, TTable>(this ILuaTableHelper<TTable> tableHelper, IEnumerable<T> list) => tableHelper.ListToTable(list.ToList());
	}
}
