using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	internal class mupen64plusInputApi
	{
		private IntPtr InpDll;

		/// <summary>
		/// Sets a callback to use when the mupen core wants controller buttons
		/// </summary>
		/// <param name="inputCallback">The delegate to use</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void SetInputCallback(InputCallback inputCallback);

		private readonly SetInputCallback InpSetInputCallback;

		/// <summary>
		/// Callback to use when mupen64plus wants input
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int InputCallback(int i);

		private InputCallback InpInputCallback;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate mupen64plusApi.m64p_error SetRumbleCallback(RumbleCallback ParamPtr);

		private SetRumbleCallback InpSetRumbleCallback;

		/// <summary>
		/// This will be called every time the N64 changes
		/// rumble
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RumbleCallback(int Control, int on);

		private RumbleCallback m64pRumbleCallback;

		/// <summary>
		/// Sets the controller pak type
		/// </summary>
		/// <param name="controller">Controller id</param>
		/// <param name="type">Type id according to (well documented... hurr hurr) mupen api</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetControllerPakType(int controller, int type);

		private readonly SetControllerPakType InpSetControllerPakType;

		/// <summary>
		/// Connects and disconnects controllers
		/// </summary>
		/// <param name="controller">Controller id</param>
		/// <param name="connected">1 if controller should be connected, 0 if controller should be disconnected</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void SetControllerConnected(int controller, int connected);

		private readonly SetControllerConnected InpSetControllerConnected;

		public mupen64plusInputApi(mupen64plusApi core)
		{
			T GetInputDelegate<T>(string proc) where T : Delegate => mupen64plusApi.GetTypedDelegate<T>(InpDll, proc);

			InpDll = core.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_INPUT,
				"mupen64plus-input-bkm.dll");

			InpSetInputCallback = GetInputDelegate<SetInputCallback>("SetInputCallback");
			InpSetRumbleCallback = GetInputDelegate<SetRumbleCallback>("SetRumbleCallback");
			InpSetControllerPakType = GetInputDelegate<SetControllerPakType>("SetControllerPakType");
			InpSetControllerConnected = GetInputDelegate<SetControllerConnected>("SetControllerConnected");
		}

		public void SetM64PInputCallbacks(InputCallback inputCallback, RumbleCallback rumbleCallback)
		{
			InpSetInputCallback(InpInputCallback = inputCallback);
			_ = InpSetRumbleCallback(m64pRumbleCallback = rumbleCallback);
		}

		public void SetM64PControllerPakType(int controller, N64SyncSettings.N64ControllerSettings.N64ControllerPakType type)
		{
			InpSetControllerPakType(controller, (int)type);
		}

		public void SetM64PControllerConnected(int controller, bool connected)
		{
			InpSetControllerConnected(controller, connected?1:0);
		}
	}
}
