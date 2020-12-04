using System;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaWinform : Form
	{
		public List<LuaEvent> ControlEvents { get; } = new List<LuaEvent>();

		private readonly string _currentDirectory = Environment.CurrentDirectory;
		private readonly LuaFile _ownerFile;

		public LuaWinform(LuaFile ownerFile, Action<IntPtr> formsWindowClosedCallback)
		{
			_ownerFile = ownerFile;
			InitializeComponent();
			Icon = Properties.Resources.TextDocIcon;
			StartPosition = FormStartPosition.CenterParent;
			Closing += (o, e) => formsWindowClosedCallback(Handle);
		}

		public void DoLuaEvent(IntPtr handle)
		{
			// #1957 - ownerFile can be full, if the script that generated the form ended which will happen if the script does not have a while true loop
			LuaSandbox.Sandbox(_ownerFile?.Thread, () =>
			{
				Environment.CurrentDirectory = _currentDirectory;
				foreach (LuaEvent luaEvent in ControlEvents)
				{
					if (luaEvent.Control == handle)
					{
						luaEvent.Event(Array.Empty<object>());
					}
				}
			});
		}

		public class LuaEvent
		{
			public LuaEvent(IntPtr handle, Func<IReadOnlyList<object>, IReadOnlyList<object>> luaFunction)
			{
				Event = luaFunction;
				Control = handle;
			}

			public Func<IReadOnlyList<object>, IReadOnlyList<object>> Event { get; }
			public IntPtr Control { get; }
		}
	}
}
