using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IVideoProvider
	{
		public int VirtualWidth => 320;
		public int VirtualHeight => 144;
		public int BufferWidth => 320;
		public int BufferHeight => 144;

		public int VsyncNumerator => L.VsyncNumerator;

		public int VsyncDenominator => L.VsyncDenominator;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int[] GetVideoBuffer() => VideoBuffer;

		private readonly int[] VideoBuffer = CreateVideoBuffer();
		
		private static int[] CreateVideoBuffer()
		{
			var b = new int[160 * 2 * 144];
			for (int i = 0; i < (160 * 2 * 144); i++)
			{
				b[i] = -1; // GB/C screen is disabled on bootup, so it always starts as white, not black
			}
			return b;
		}

	}
}
