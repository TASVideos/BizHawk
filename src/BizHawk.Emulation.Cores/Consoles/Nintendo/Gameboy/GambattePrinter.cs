using System;

using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// Emulate the gameboy printer in managed code
	/// </summary>
	public class GambattePrinter
	{
		// A loose c->c# port of SameBoy's printer code

		private enum CommandState : byte
		{
			GB_PRINTER_COMMAND_MAGIC1,
			GB_PRINTER_COMMAND_MAGIC2,
			GB_PRINTER_COMMAND_ID,
			GB_PRINTER_COMMAND_COMPRESSION,
			GB_PRINTER_COMMAND_LENGTH_LOW,
			GB_PRINTER_COMMAND_LENGTH_HIGH,
			GB_PRINTER_COMMAND_DATA,
			GB_PRINTER_COMMAND_CHECKSUM_LOW,
			GB_PRINTER_COMMAND_CHECKSUM_HIGH,
			GB_PRINTER_COMMAND_ACTIVE,
			GB_PRINTER_COMMAND_STATUS,
		}

		private enum CommandID : byte
		{
			GB_PRINTER_INIT_COMMAND = 1,
			GB_PRINTER_START_COMMAND = 2,
			GB_PRINTER_DATA_COMMAND = 4,
			GB_PRINTER_NOP_COMMAND = 0xF,
		}

		private const int GB_PRINTER_MAX_COMMAND_LENGTH = 0x280;
		private const int GB_PRINTER_DATA_SIZE = 0x280;

		private const ushort SerialIRQAddress = 0x58;

		private readonly Gameboy gb;
		private readonly PrinterCallback callback;
		private LibGambatte.LinkCallback linkCallback;

		private CommandState command_state;
		private CommandID command_id;

		private bool compression;
		private ushort length_left;
		private readonly byte[] command_data = new byte[GB_PRINTER_MAX_COMMAND_LENGTH];
		private ushort command_length;
		private ushort checksum;
		private byte status;

		private readonly byte[] image = new byte[160 * 200];
		private ushort image_offset;

		private byte compression_run_length;
		private bool compression_run_is_compressed;

		public GambattePrinter(Gameboy gb, PrinterCallback callback)
		{
			this.gb = gb;
			this.callback = callback;

			linkCallback = OnSerial;
			LibGambatte.gambatte_setlinkcallback(gb.GambatteState, linkCallback);

			// connect the cable
			LibGambatte.gambatte_linkstatus(gb.GambatteState, 259);
		}

		public void Disconnect()
		{
			if (gb.GambatteState != IntPtr.Zero)
				LibGambatte.gambatte_setlinkcallback(gb.GambatteState, null);
		}

		private void OnSerial()
		{
			if (LibGambatte.gambatte_linkstatus(gb.GambatteState, 256) != 0) // ClockTrigger
			{
				LibGambatte.gambatte_linkstatus(gb.GambatteState, 257); // ack

				byte output = HandleSerial((byte)LibGambatte.gambatte_linkstatus(gb.GambatteState, 258)); // GetOut
				LibGambatte.gambatte_linkstatus(gb.GambatteState, output); // ShiftIn
			}
		}

		private byte HandleSerial(byte byte_received)
		{
			byte byte_to_send = 0;

			switch (command_state)
			{
				case CommandState.GB_PRINTER_COMMAND_MAGIC1:
					if (byte_received != 0x88)
					{
						return byte_to_send;
					}
					status &= 254;
					command_length = 0;
					checksum = 0;
					break;

				case CommandState.GB_PRINTER_COMMAND_MAGIC2:
					if (byte_received != 0x33)
					{
						if (byte_received != 0x88)
						{
							command_state = CommandState.GB_PRINTER_COMMAND_MAGIC1;
						}
						return byte_to_send;
					}
					break;

				case CommandState.GB_PRINTER_COMMAND_ID:
					command_id = (CommandID)(byte_received & 0xF);
					break;

				case CommandState.GB_PRINTER_COMMAND_COMPRESSION:
					compression = (byte_received & 1) != 0;
					break;

				case CommandState.GB_PRINTER_COMMAND_LENGTH_LOW:
					length_left = byte_received;
					break;

				case CommandState.GB_PRINTER_COMMAND_LENGTH_HIGH:
					length_left |= (ushort)((byte_received & 3) << 8);
					break;

				case CommandState.GB_PRINTER_COMMAND_DATA:
					if (command_length != GB_PRINTER_MAX_COMMAND_LENGTH)
					{
						if (compression)
						{
							if (compression_run_length == 0)
							{
								compression_run_is_compressed = (byte_received & 0x80) != 0;
								compression_run_length = (byte)((byte_received & 0x7F) + 1 + (compression_run_is_compressed ? 1 : 0));
							}
							else if (compression_run_is_compressed)
							{
								while (compression_run_length > 0)
								{
									command_data[command_length++] = byte_received;
									compression_run_length--;
									if (command_length == GB_PRINTER_MAX_COMMAND_LENGTH)
									{
										compression_run_length = 0;
									}
								}
							}
							else
							{
								command_data[command_length++] = byte_received;
								compression_run_length--;
							}
						}
						else
						{
							command_data[command_length++] = byte_received;
						}
					}
					length_left--;
					break;

				case CommandState.GB_PRINTER_COMMAND_CHECKSUM_LOW:
					checksum ^= byte_received;
					break;

				case CommandState.GB_PRINTER_COMMAND_CHECKSUM_HIGH:
					checksum ^= (ushort)(byte_received << 8);
					if (checksum != 0)
					{
						status |= 1; /* Checksum error*/
						command_state = CommandState.GB_PRINTER_COMMAND_MAGIC1;
						return byte_to_send;
					}
					break;

				case CommandState.GB_PRINTER_COMMAND_ACTIVE:
					byte_to_send = 0x81;
					break;

				case CommandState.GB_PRINTER_COMMAND_STATUS:

					if (((int)command_id & 0xF) == (byte)CommandID.GB_PRINTER_INIT_COMMAND)
					{
						/* Games expect INIT commands to return 0? */
						byte_to_send = 0;
					}
					else
					{
						byte_to_send = status;
					}

					/* Printing is done instantly, but let the game recieve a 6 (Printing) status at least once, for compatibility */
					if (status == 6)
					{
						status = 4; /* Done */
					}

					command_state = CommandState.GB_PRINTER_COMMAND_MAGIC1;
					HandleCommand();
					return byte_to_send;
			}

			if (command_state >= CommandState.GB_PRINTER_COMMAND_ID && command_state < CommandState.GB_PRINTER_COMMAND_CHECKSUM_LOW)
			{
				checksum += byte_received;
			}

			if (command_state != CommandState.GB_PRINTER_COMMAND_DATA)
			{
				command_state++;
			}

			if (command_state == CommandState.GB_PRINTER_COMMAND_DATA)
			{
				if (length_left == 0)
				{
					command_state++;
				}
			}

			return byte_to_send;
		}

		private void HandleCommand()
		{
			switch (command_id)
			{
				case CommandID.GB_PRINTER_INIT_COMMAND:
					status = 0;
					image_offset = 0;
					break;

				case CommandID.GB_PRINTER_START_COMMAND:
					if (command_length == 4)
					{
						status = 6; /* Printing */
						uint[] outputImage = new uint[image_offset];

						int palette = command_data[2];
						uint[] colors = {
							0xFFFFFFFFU,
							0xFFAAAAAAU,
							0xFF555555U,
							0xFF000000U
						};
						for (int i = 0; i < image_offset; i++)
						{
							outputImage[i] = colors[(palette >> (image[i] * 2)) & 3];
						}

						if (callback != null)
						{
							// The native-friendly callback almost seems silly now :P
							unsafe
							{
								fixed (uint* imagePtr = outputImage)
								{
									callback((IntPtr)imagePtr, (byte)(image_offset / 160),
										(byte)(command_data[1] >> 4), (byte)(command_data[1] & 7),
										(byte)(command_data[3] & 0x7F));
								}
							}
						}

						image_offset = 0;
					}
					break;

				case CommandID.GB_PRINTER_DATA_COMMAND:
					if (command_length == GB_PRINTER_DATA_SIZE)
					{
						image_offset %= (ushort)image.Length;
						status = 8; /* Received 0x280 bytes */

						int data_index = 0;

						for (int row = 2; row > 0; row--)
						{
							for (int tile_x = 0; tile_x < 160 / 8; tile_x++)
							{
								for (int y = 0; y < 8; y++, data_index += 2)
								{
									for (int x_pixel = 0; x_pixel < 8; x_pixel++)
									{
										image[image_offset + tile_x * 8 + x_pixel + y * 160] =
											(byte)((command_data[data_index] >> 7) | ((command_data[data_index + 1] >> 7) << 1));
										command_data[data_index] <<= 1;
										command_data[data_index + 1] <<= 1;
									}
								}
							}

							image_offset += 8 * 160;
						}
					}
					break;

				default:
					break;
			}
		}
	}
}
