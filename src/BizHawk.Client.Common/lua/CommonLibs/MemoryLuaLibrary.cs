using System;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("These functions behavior identically to the mainmemory functions but the user can set the memory domain to read and write from. The default domain is the system bus. Use getcurrentmemorydomain(), and usememorydomain() to control which domain is used. Each core has its own set of valid memory domains. Use getmemorydomainlist() to get a list of memory domains for the current core loaded.")]
	public sealed class MemoryLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public MemoryLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "memory";

		[LuaMethodExample("local nlmemget = memory.getmemorydomainlist();")]
		[LuaMethod("getmemorydomainlist", "Returns a string of the memory domains for the loaded platform core. List will be a single string delimited by line feeds")]
		public TTable GetMemoryDomainList() => _th.ListToTable(APIs.Memory.GetMemoryDomainList());

		[LuaMethodExample("local uimemget = memory.getmemorydomainsize( mainmemory.getname( ) );")]
		[LuaMethod("getmemorydomainsize", "Returns the number of bytes of the specified memory domain. If no domain is specified, or the specified domain doesn't exist, returns the current domain size")]
		public uint GetMemoryDomainSize(string name = "") => APIs.Memory.GetMemoryDomainSize(name);

		[LuaMethodExample("local stmemget = memory.getcurrentmemorydomain( );")]
		[LuaMethod("getcurrentmemorydomain", "Returns a string name of the current memory domain selected by Lua. The default is Main memory")]
		public string GetCurrentMemoryDomain() => APIs.Memory.GetCurrentMemoryDomain();

		[LuaMethodExample("local uimemget = memory.getcurrentmemorydomainsize( );")]
		[LuaMethod("getcurrentmemorydomainsize", "Returns the number of bytes of the current memory domain selected by Lua. The default is Main memory")]
		public uint GetCurrentMemoryDomainSize() => APIs.Memory.GetCurrentMemoryDomainSize();

		[LuaMethodExample("if ( memory.usememorydomain( mainmemory.getname( ) ) ) then\r\n\tconsole.log( \"Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true\" );\r\nend;")]
		[LuaMethod("usememorydomain", "Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true")]
		public bool UseMemoryDomain(string domain) => APIs.Memory.UseMemoryDomain(domain);

		[LuaMethodExample("local stmemhas = memory.hash_region( 0x100, 50, mainmemory.getname( ) );")]
		[LuaMethod("hash_region", "Returns a hash as a string of a region of memory, starting from addr, through count bytes. If the domain is unspecified, it uses the current region.")]
		public string HashRegion(int addr, int count, string domain = null) => APIs.Memory.HashRegion(addr, count, domain);

		[LuaMethodExample("local uimemrea = memory.readbyte( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("readbyte", "gets the value from the given address as an unsigned byte")]
		public uint ReadByte(int addr, string domain = null) => APIs.Memory.ReadByte(addr, domain);

		[LuaMethodExample("memory.writebyte( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("writebyte", "Writes the given value to the given address as an unsigned byte")]
		public void WriteByte(int addr, uint value, string domain = null) => APIs.Memory.WriteByte(addr, value, domain);

		[LuaMethodExample("local nlmemrea = memory.readbyterange( 0x100, 30, mainmemory.getname( ) );")]
		[LuaMethod("readbyterange", "Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key).")]
		public TTable ReadByteRange(int addr, int length, string domain = null) => _th.ListToTable(APIs.Memory.ReadByteRange(addr, length, domain));

		/// <remarks>TODO C# version requires a contiguous address range</remarks>
		[LuaMethodExample("")]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public void WriteByteRange(TTable memoryblock, string domain = null)
		{
#if true
			foreach (var (addr, v) in _th.EnumerateEntries<double, double>(memoryblock))
			{
				APIs.Memory.WriteByte(LuaInt(addr), (uint) v, domain);
			}
#else
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
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

		[LuaMethodExample("local simemrea = memory.readfloat( 0x100, false, mainmemory.getname( ) );")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public float ReadFloat(int addr, bool bigendian, string domain = null)
		{
			APIs.Memory.SetBigEndian(bigendian);
			return APIs.Memory.ReadFloat(addr, domain);
		}

		[LuaMethodExample("memory.writefloat( 0x100, 10.0, false, mainmemory.getname( ) );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public void WriteFloat(int addr, double value, bool bigendian, string domain = null)
		{
			APIs.Memory.SetBigEndian(bigendian);
			APIs.Memory.WriteFloat(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s8( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s8", "read signed byte")]
		public int ReadS8(int addr, string domain = null) => APIs.Memory.ReadS8(addr, domain);

		[LuaMethodExample("memory.write_s8( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s8", "write signed byte")]
		public void WriteS8(int addr, uint value, string domain = null) => APIs.Memory.WriteS8(addr, unchecked((int) value), domain);

		[LuaMethodExample("local uimemrea = memory.read_u8( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u8", "read unsigned byte")]
		public uint ReadU8(int addr, string domain = null) => APIs.Memory.ReadU8(addr, domain);

		[LuaMethodExample("memory.write_u8( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u8", "write unsigned byte")]
		public void WriteU8(int addr, uint value, string domain = null) => APIs.Memory.WriteU8(addr, value, domain);

		[LuaMethodExample("local inmemrea = memory.read_s16_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS16(addr, domain);
		}

		[LuaMethodExample("memory.write_s16_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(int addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS16(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s16_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS16(addr, domain);
		}

		[LuaMethodExample("memory.write_s16_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(int addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS16(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u16_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU16(addr, domain);
		}

		[LuaMethodExample("memory.write_u16_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(int addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU16(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u16_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU16(addr, domain);
		}

		[LuaMethodExample("memory.write_u16_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(int addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU16(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s24_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS24(addr, domain);
		}

		[LuaMethodExample("memory.write_s24_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(int addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS24(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s24_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS24(addr, domain);
		}

		[LuaMethodExample("memory.write_s24_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(int addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS24(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u24_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU24(addr, domain);
		}

		[LuaMethodExample("memory.write_u24_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(int addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU24(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u24_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU24(addr, domain);
		}

		[LuaMethodExample("memory.write_u24_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(int addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU24(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s32_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS32(addr, domain);
		}

		[LuaMethodExample("memory.write_s32_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(int addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS32(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s32_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS32(addr, domain);
		}

		[LuaMethodExample("memory.write_s32_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(int addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS32(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u32_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU32(addr, domain);
		}

		[LuaMethodExample("memory.write_u32_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(int addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU32(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u32_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(int addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU32(addr, domain);
		}

		[LuaMethodExample("memory.write_u32_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(int addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU32(addr, value, domain);
		}
	}
}
