using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// TODO: Switch to dotnet core and remove this junkus
	/// </summary>
	public interface ISpanStream
	{
		void Write (ReadOnlySpan<byte> buffer);
		int Read (Span<byte> buffer);
	}
	public class SpanStream
	{
		/// <summary>
		/// Returns a stream in spanstream mode, or creates a wrapper that provides that functionality
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static ISpanStream GetOrBuild(Stream s)
		{
			return s as ISpanStream
				?? new SpanStreamAdapter(s);
		}
		private class SpanStreamAdapter : ISpanStream
		{
			public SpanStreamAdapter(Stream stream)
			{
				_stream = stream;
			}
			private byte[] _buffer = new byte[0];
			private readonly Stream _stream;
			public unsafe int Read(Span<byte> buffer)
			{
				if (buffer.Length > _buffer.Length)
				{
					_buffer = new byte[buffer.Length];
				}
				var n = _stream.Read(_buffer, 0, buffer.Length);
				fixed(byte* p = buffer)
				{
					Marshal.Copy(_buffer, 0, (IntPtr)p, n);
				}
				return n;
			}

			public unsafe void Write(ReadOnlySpan<byte> buffer)
			{
				if (buffer.Length > _buffer.Length)
				{
					_buffer = new byte[buffer.Length];
				}
				fixed(byte* p = buffer)
				{
					Marshal.Copy((IntPtr)p, _buffer, 0, buffer.Length);
				}
				_stream.Write(_buffer, 0, buffer.Length);
			}
		}
	}
}
