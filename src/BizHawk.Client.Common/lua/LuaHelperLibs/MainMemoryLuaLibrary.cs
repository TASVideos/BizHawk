using System;
using System.ComponentModel;

using BizHawk.Emulation.Common;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("Main memory library reads and writes from the Main memory domain (the default memory domain set by any given core)")]
	public sealed class MainMemoryLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomainCore { get; set; }

		public MainMemoryLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "mainmemory";

		private MemoryDomain Domain
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					return MemoryDomainCore.MainMemory;
				}

				var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
				Log(error);
				throw new NotImplementedException(error);
			}
		}

		[LuaMethodExample("local stmaiget = mainmemory.getname( );")]
		[LuaMethod("getname", "returns the name of the domain defined as main memory for the given core")]
		public string GetName()
		{
			return Domain.Name;
		}

		[LuaMethodExample("local uimaiget = mainmemory.getcurrentmemorydomainsize( );")]
		[LuaMethod("getcurrentmemorydomainsize", "Returns the number of bytes of the domain defined as main memory")]
		public uint GetSize()
		{
			return (uint)Domain.Size;
		}

		[LuaMethodExample("local uimairea = mainmemory.readbyte( 0x100 );")]
		[LuaMethod("readbyte", "gets the value from the given address as an unsigned byte")]
		public uint ReadByte(int addr) => APIs.Memory.ReadByte(addr, Domain.Name);

		[LuaMethodExample("mainmemory.writebyte( 0x100, 1000 );")]
		[LuaMethod("writebyte", "Writes the given value to the given address as an unsigned byte")]
		public void WriteByte(int addr, uint value) => APIs.Memory.WriteByte(addr, value, Domain.Name);

		[LuaMethodExample("local nlmairea = mainmemory.readbyterange( 0x100, 64 );")]
		[LuaMethod("readbyterange", "Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key).")]
		public TTable ReadByteRange(int addr, int length)
		{
			return _th.ListToTable(APIs.Memory.ReadByteRange(addr, length, Domain.Name));
		}

		/// <remarks>TODO C# version requires a contiguous address range</remarks>
		[LuaMethodExample("")]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public void WriteByteRange(TTable memoryblock)
		{
#if true
			foreach (var (addr, v) in _th.EnumerateEntries<double, double>(memoryblock))
			{
				APIs.Memory.WriteByte(LuaInt(addr), (uint) v, Domain.Name);
			}
#else
			var d = Domain;
			if (d.CanPoke())
			{
				foreach (var (address, v) in _th.EnumerateEntries<double, double>(memoryblock))
				{
					var addr = LuaInt(address);
					if (addr < d.Size)
					{
						d.PokeByte(addr, (byte) LuaInt(v));
					}
					else
					{
						Log($"Warning: Attempted write {addr} outside memory domain size of {d.Size} in writebyterange()");
					}
				}
			}
			else
			{
				Log($"Error: the domain {d.Name} is not writable");
			}
#endif
		}

		[LuaMethodExample("local simairea = mainmemory.readfloat(0x100, false);")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public float ReadFloat(int addr, bool bigendian)
		{
			APIs.Memory.SetBigEndian(bigendian);
			return APIs.Memory.ReadFloat(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.writefloat( 0x100, 10.0, false );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public void WriteFloat(int addr, double value, bool bigendian)
		{
			APIs.Memory.SetBigEndian(bigendian);
			APIs.Memory.WriteFloat(addr, value, Domain.Name);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s8( 0x100 );")]
		[LuaMethod("read_s8", "read signed byte")]
		public int ReadS8(int addr) => APIs.Memory.ReadS8(addr, Domain.Name);

		[LuaMethodExample("mainmemory.write_s8( 0x100, 1000 );")]
		[LuaMethod("write_s8", "write signed byte")]
		public void WriteS8(int addr, uint value) => APIs.Memory.WriteS8(addr, unchecked((int) value), Domain.Name);

		[LuaMethodExample("local uimairea = mainmemory.read_u8( 0x100 );")]
		[LuaMethod("read_u8", "read unsigned byte")]
		public uint ReadU8(int addr) => APIs.Memory.ReadU8(addr, Domain.Name);

		[LuaMethodExample("mainmemory.write_u8( 0x100, 1000 );")]
		[LuaMethod("write_u8", "write unsigned byte")]
		public void WriteU8(int addr, uint value) => APIs.Memory.WriteU8(addr, value, Domain.Name);

		[LuaMethodExample("local inmairea = mainmemory.read_s16_le( 0x100 );")]
		[LuaMethod("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(int addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS16(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_s16_le( 0x100, -1000 );")]
		[LuaMethod("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(int addr, int value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS16(addr, value, Domain.Name);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s16_be( 0x100 );")]
		[LuaMethod("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(int addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS16(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_s16_be( 0x100, -1000 );")]
		[LuaMethod("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(int addr, int value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS16(addr, value, Domain.Name);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u16_le( 0x100 );")]
		[LuaMethod("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(int addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU16(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_u16_le( 0x100, 1000 );")]
		[LuaMethod("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(int addr, uint value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU16(addr, value, Domain.Name);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u16_be( 0x100 );")]
		[LuaMethod("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(int addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU16(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_u16_be( 0x100, 1000 );")]
		[LuaMethod("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(int addr, uint value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU16(addr, value, Domain.Name);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s24_le( 0x100 );")]
		[LuaMethod("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(int addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS24(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_s24_le( 0x100, -1000 );")]
		[LuaMethod("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(int addr, int value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS24(addr, value, Domain.Name);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s24_be( 0x100 );")]
		[LuaMethod("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(int addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS24(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_s24_be( 0x100, -1000 );")]
		[LuaMethod("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(int addr, int value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS24(addr, value, Domain.Name);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u24_le( 0x100 );")]
		[LuaMethod("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(int addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU24(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_u24_le( 0x100, 1000 );")]
		[LuaMethod("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(int addr, uint value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU24(addr, value, Domain.Name);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u24_be( 0x100 );")]
		[LuaMethod("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(int addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU24(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_u24_be( 0x100, 1000 );")]
		[LuaMethod("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(int addr, uint value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU24(addr, value, Domain.Name);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s32_le( 0x100 );")]
		[LuaMethod("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(int addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS32(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_s32_le( 0x100, -1000 );")]
		[LuaMethod("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(int addr, int value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS32(addr, value, Domain.Name);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s32_be( 0x100 );")]
		[LuaMethod("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(int addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS32(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_s32_be( 0x100, -1000 );")]
		[LuaMethod("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(int addr, int value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS32(addr, value, Domain.Name);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u32_le( 0x100 );")]
		[LuaMethod("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(int addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU32(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_u32_le( 0x100, 1000 );")]
		[LuaMethod("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(int addr, uint value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU32(addr, value, Domain.Name);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u32_be( 0x100 );")]
		[LuaMethod("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(int addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU32(addr, Domain.Name);
		}

		[LuaMethodExample("mainmemory.write_u32_be( 0x100, 1000 );")]
		[LuaMethod("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(int addr, uint value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU32(addr, value, Domain.Name);
		}
	}
}
