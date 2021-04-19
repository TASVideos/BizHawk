using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public class N64Input
	{
		private readonly mupen64plusInputApi _api;
		public IController Controller { get; set; }

		public bool LastFrameInputPolled { get; set; }
		public bool ThisFrameInputPolled { get; set; }

		private readonly IInputPollable _emuCore;

		internal N64Input(IInputPollable emuCore, mupen64plusApi core, N64SyncSettings.N64ControllerSettings[] controllerSettings)
		{
			_emuCore = emuCore;
			_api = new mupen64plusInputApi(core);

			_api.SetM64PInputCallbacks(GetControllerInput, SetRumble);

			core.VInterrupt += ShiftInputPolledBools;
			for (int i = 0; i < controllerSettings.Length; ++i)
			{
				SetControllerConnected(i, controllerSettings[i].IsConnected);
				SetControllerPakType(i, controllerSettings[i].PakType);
			}
		}

		public void ShiftInputPolledBools()
		{
			LastFrameInputPolled = ThisFrameInputPolled;
			ThisFrameInputPolled = false;
		}

		private const sbyte _maxAnalogX = 127;
		private const sbyte _minAnalogX = -128;
		private const sbyte _maxAnalogY = 127;
		private const sbyte _minAnalogY = -128;

		/// <summary>
		/// Translates controller input from EmuHawk into
		/// N64 controller data
		/// </summary>
		/// <param name="i">Id of controller to update and shove</param>
		public int GetControllerInput(int i)
		{
			_emuCore.InputCallbacks.Call();
			ThisFrameInputPolled = true;

			// Analog stick right = +X
			// Analog stick up = +Y
			string p = "P" + (i + 1);
			sbyte x;
			if (Controller.IsPressed(p + " A Left"))
			{
				x = _minAnalogX;
			}
			else if (Controller.IsPressed(p + " A Right"))
			{
				x = _maxAnalogX;
			}
			else
			{
				x = (sbyte)Controller.AxisValue(p + " X Axis");
			}

			sbyte y;
			if (Controller.IsPressed(p + " A Up"))
			{
				y = _maxAnalogY;
			}
			else if (Controller.IsPressed(p + " A Down"))
			{
				y = _minAnalogY;
			}
			else
			{
				y = (sbyte)Controller.AxisValue(p + " Y Axis");
			}

			int value = ReadController(i + 1);
			value |= (x & 0xFF) << 16;
			value |= (y & 0xFF) << 24;
			return value;
		}

		private void SetRumble(int player, int bIsActive)
			=> Controller.SetHapticChannelStrength($"P{player} Rumble Pak", bIsActive == 0 ? 0 : int.MaxValue);

		/// <summary>
		/// Read all buttons from a controller and translate them
		/// into a form the N64 understands
		/// </summary>
		/// <param name="num">Number of controller to translate</param>
		/// <returns>Bit list of pressed buttons</returns>
		public int ReadController(int num)
		{
			int buttons = 0;

			if (Controller.IsPressed("P" + num + " DPad R")) buttons |= (1 << 0);
			if (Controller.IsPressed("P" + num + " DPad L")) buttons |= (1 << 1);
			if (Controller.IsPressed("P" + num + " DPad D")) buttons |= (1 << 2);
			if (Controller.IsPressed("P" + num + " DPad U")) buttons |= (1 << 3);
			if (Controller.IsPressed("P" + num + " Start")) buttons |= (1 << 4);
			if (Controller.IsPressed("P" + num + " Z")) buttons |= (1 << 5);
			if (Controller.IsPressed("P" + num + " B")) buttons |= (1 << 6);
			if (Controller.IsPressed("P" + num + " A")) buttons |= (1 << 7);
			if (Controller.IsPressed("P" + num + " C Right")) buttons |= (1 << 8);
			if (Controller.IsPressed("P" + num + " C Left")) buttons |= (1 << 9);
			if (Controller.IsPressed("P" + num + " C Down")) buttons |= (1 << 10);
			if (Controller.IsPressed("P" + num + " C Up")) buttons |= (1 << 11);
			if (Controller.IsPressed("P" + num + " R")) buttons |= (1 << 12);
			if (Controller.IsPressed("P" + num + " L")) buttons |= (1 << 13);

			return buttons;
		}

		/// <summary>
		/// Sets the controller pak of the controller to the specified type
		/// </summary>
		/// <param name="controller">Id of the controller</param>
		/// <param name="type">Type to which the controller pak is set. Currently only NO_PAK and MEMORY_CARD are supported</param>
		public void SetControllerPakType(int controller, N64SyncSettings.N64ControllerSettings.N64ControllerPakType type)
		{
			_api.SetM64PControllerPakType(controller, type);
		}

		/// <summary>
		/// Sets the connection status of the controller
		/// </summary>
		/// <param name="controller">Id of the controller to connect or disconnect</param>
		/// <param name="connectionStatus">New status of the controller connection</param>
		public void SetControllerConnected(int controller, bool connectionStatus)
		{
			_api.SetM64PControllerConnected(controller, connectionStatus);
		}
	}
}
