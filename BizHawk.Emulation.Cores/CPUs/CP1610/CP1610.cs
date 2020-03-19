﻿using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.CP1610
{
	public sealed partial class CP1610
	{
		private const ushort RESET = 0x1000;
		private const ushort INTERRUPT = 0x1004;

		internal bool FlagS, FlagC, FlagZ, FlagO, FlagI, FlagD, IntRM, BusRq, BusAk, Interruptible, Interrupted;
		//private bool MSync;
		internal ushort[] Register = new ushort[8];

		private ushort RegisterSP
		{
			get => Register[6];
			set => Register[6] = value;
		}

		private ushort RegisterPC
		{
			get => Register[7];
			set => Register[7] = value;
		}

		public string TraceHeader => "CP1610: PC, machine code, mnemonic, operands, flags (SCZOID)";

		public Action<TraceInfo> TraceCallback;
		public IMemoryCallbackSystem MemoryCallbacks { get; set; }

		public ushort ReadMemoryWrapper(ushort addr, bool peek)
		{
			if (MemoryCallbacks != null && !peek)
			{
				uint flags = (uint)(MemoryCallbackFlags.AccessRead);
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}

			return ReadMemory(addr, peek);
		}

		public void WriteMemoryWrapper(ushort addr, ushort value, bool poke)
		{
			if (MemoryCallbacks != null && !poke)
			{
				uint flags = (uint)(MemoryCallbackFlags.AccessWrite);
				MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			}

			WriteMemory(addr, value, poke);
		}

		public int TotalExecutedCycles;
		public int PendingCycles;

		public Func<ushort, bool, ushort> ReadMemory;
		public Func<ushort, ushort, bool, bool> WriteMemory;

		private static bool Logging = false;
		private static readonly StreamWriter Log;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(CP1610));

			ser.Sync(nameof(Register), ref Register, false);
			ser.Sync(nameof(FlagS), ref FlagS);
			ser.Sync(nameof(FlagC), ref FlagC);
			ser.Sync(nameof(FlagZ), ref FlagZ);
			ser.Sync(nameof(FlagO), ref FlagO);
			ser.Sync(nameof(FlagI), ref FlagI);
			ser.Sync(nameof(FlagD), ref FlagD);
			ser.Sync(nameof(IntRM), ref IntRM);
			ser.Sync(nameof(BusRq), ref BusRq);
			ser.Sync(nameof(BusAk), ref BusAk);
			ser.Sync("Duplicate_Bus_Rq", ref BusRq); // Can't remove this or it will break backward compatibility with binary states
			ser.Sync(nameof(Interruptible), ref Interruptible);
			ser.Sync(nameof(Interrupted), ref Interrupted);
			ser.Sync("Toal_executed_cycles", ref TotalExecutedCycles);
			ser.Sync("Pending_Cycles", ref PendingCycles);


			ser.EndSection();
		}

	static CP1610()
		{
			if (Logging)
			{
				Log = new StreamWriter("log_CP1610.txt");
			}
		}

		public void Reset()
		{
			BusAk = true;
			Interruptible = false;
			FlagS = FlagC = FlagZ = FlagO = FlagI = FlagD = false;
			for (int register = 0; register <= 6; register++)
			{
				Register[register] = 0;
			}
			RegisterPC = RESET;
			PendingCycles = 0;
		}

		public bool GetBusAk()
		{
			return BusAk;
		}

		public void SetIntRM(bool value)
		{
			IntRM = value;
			if (IntRM)
			{
				Interrupted = false;
			}
		}

		public void SetBusRq(bool value)
		{
			BusRq = !value;
		}

		public int GetPendingCycles()
		{
			return PendingCycles;
		}

		public void AddPendingCycles(int cycles)
		{
			PendingCycles += cycles;
		}

		public void LogData()
		{
			if (!Logging)
			{
				return;
			}
			Log.WriteLine("Total Executed Cycles = {0}", TotalExecutedCycles);
			for (int register = 0; register <= 5; register++)
			{
				Log.WriteLine("R{0:d} = {1:X4}", register, Register[register]);
			}
			Log.WriteLine("SP = {0:X4}", RegisterSP);
			Log.WriteLine("PC = {0:X4}", RegisterPC);
			Log.WriteLine("S = {0}", FlagS);
			Log.WriteLine("C = {0}", FlagC);
			Log.WriteLine("Z = {0}", FlagZ);
			Log.WriteLine("O = {0}", FlagO);
			Log.WriteLine("I = {0}", FlagI);
			Log.WriteLine("D = {0}", FlagD);
			Log.WriteLine("INTRM = {0}", IntRM);
			Log.WriteLine("BUSRQ = {0}", BusRq);
			Log.WriteLine("BUSAK = {0}", BusAk);
			// Log.WriteLine("MSYNC = {0}", MSync);
			Log.Flush();
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["R0"] = Register[0],
				["R1"] = Register[1],
				["R2"] = Register[2],
				["R3"] = Register[3],
				["R4"] = Register[4],
				["R5"] = Register[5],
				["R6"] = Register[6],
				["PC"] = Register[7],

				["FlagS"] = FlagS,
				["FlagC"] = FlagC,
				["FlagZ"] = FlagZ,
				["FlagO"] = FlagO,
				["FlagI"] = FlagI,
				["FlagD"] = FlagD
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();

				case "R0":
					Register[0] = (ushort)value;
					break;
				case "R1":
					Register[1] = (ushort)value;
					break;
				case "R2":
					Register[2] = (ushort)value;
					break;
				case "R3":
					Register[3] = (ushort)value;
					break;
				case "R4":
					Register[4] = (ushort)value;
					break;
				case "R5":
					Register[5] = (ushort)value;
					break;
				case "R6":
					Register[6] = (ushort)value;
					break;
				case "PC":
					Register[7] = (ushort)value;
					break;

				case "FlagS":
					FlagS = value > 0;
					break;
				case "FlagC":
					FlagC = value > 0;
					break;
				case "FlagZ":
					FlagZ = value > 0;
					break;
				case "FlagO":
					FlagO = value > 0;
					break;
				case "FlagI":
					FlagI = value > 0;
					break;
				case "FlagD":
					FlagD = value > 0;
					break;
			}
		}
	}
}
