using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			int[] data = new int[10];
			LibGambatte.gambatte_getregs(GambatteState, data);

			return new Dictionary<string, RegisterValue>
			{
				["PC"] = (ushort)(data[(int)LibGambatte.RegIndices.PC] & 0xffff),
				["SP"] = (ushort)(data[(int)LibGambatte.RegIndices.SP] & 0xffff),
				["A"] = (byte)(data[(int)LibGambatte.RegIndices.A] & 0xff),
				["B"] = (byte)(data[(int)LibGambatte.RegIndices.B] & 0xff),
				["C"] = (byte)(data[(int)LibGambatte.RegIndices.C] & 0xff),
				["D"] = (byte)(data[(int)LibGambatte.RegIndices.D] & 0xff),
				["E"] = (byte)(data[(int)LibGambatte.RegIndices.E] & 0xff),
				["F"] = (byte)(data[(int)LibGambatte.RegIndices.F] & 0xff),
				["H"] = (byte)(data[(int)LibGambatte.RegIndices.H] & 0xff),
				["L"] = (byte)(data[(int)LibGambatte.RegIndices.L] & 0xff)
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			int[] data = new int[10];
			LibGambatte.gambatte_getregs(GambatteState, data);
			LibGambatte.RegIndices index = (LibGambatte.RegIndices)Enum.Parse(typeof(LibGambatte.RegIndices), register);
			data[(int)index] = value & (index <= LibGambatte.RegIndices.SP ? 0xffff : 0xff);
			LibGambatte.gambatte_setregs(GambatteState, data);
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => Math.Max((long)_cycleCount, (long)callbackCycleCount);

		private const string systemBusScope = "System Bus";

		private MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { systemBusScope });
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		private LibGambatte.MemoryCallback _readcb;
		private LibGambatte.MemoryCallback _writecb;
		private LibGambatte.MemoryCallback _execcb;

		/// <summary>
		/// for use in dual core
		/// </summary>
		internal void ConnectMemoryCallbackSystem(MemoryCallbackSystem mcs)
		{
			_memorycallbacks = mcs;
		}

		private void InitMemoryCallbacks()
		{
			LibGambatte.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType)
			{
				var rawFlags = (uint)flags;
				return (address, cycleOffset) =>
				{
					callbackCycleCount = _cycleCount + cycleOffset;
					if (getHasCBOfType()) MemoryCallbacks.CallMemoryCallbacks(address, 0, rawFlags, systemBusScope);
				};
			}

			_readcb = CreateCallback(MemoryCallbackFlags.AccessRead, () => MemoryCallbacks.HasReads);
			_writecb = CreateCallback(MemoryCallbackFlags.AccessWrite, () => MemoryCallbacks.HasWrites);
			_execcb = CreateCallback(MemoryCallbackFlags.AccessExecute, () => MemoryCallbacks.HasExecutes);

			_memorycallbacks.ActiveChanged += () =>
			{
				LibGambatte.gambatte_setreadcallback(GambatteState, MemoryCallbacks.HasReads ? _readcb : null);
				LibGambatte.gambatte_setwritecallback(GambatteState, MemoryCallbacks.HasWrites ? _writecb : null);
				LibGambatte.gambatte_setexeccallback(GambatteState, MemoryCallbacks.HasExecutes ? _execcb : null);
			};
		}
	}
}
