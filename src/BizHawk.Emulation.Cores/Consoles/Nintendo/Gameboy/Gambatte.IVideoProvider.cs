namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		/// <summary>
		/// stored image of most recent frame
		/// </summary>
		private readonly int[] VideoBuffer = CreateVideoBuffer();
		
		private static int[] CreateVideoBuffer()
		{
			var b = new int[160 * 144];
			for (int i = 0; i < (160 * 144); i++)
			{
				b[i] = -1; // GB/C screen is disabled on bootup, so it always starts as white, not black
			}
			return b;
		}

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		public int VirtualWidth => 160; // only sgb changes this, which we don't emulate here

		public int VirtualHeight => 144;

		public int BufferWidth => 160;

		public int BufferHeight => 144;

		public int BackgroundColor => 0;

		public int VsyncNumerator => 262144;

		public int VsyncDenominator => 4389;
	}
}
