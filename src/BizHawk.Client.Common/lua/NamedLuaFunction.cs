using System;
using System.Collections.Generic;

using NLua;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class NamedLuaFunction : INamedLuaFunction
	{
		private readonly Func<IReadOnlyList<object>, IReadOnlyList<object>> _function;

		public NamedLuaFunction(Func<IReadOnlyList<object>, IReadOnlyList<object>> function, string theEvent, Action<string> logCallback, LuaFile luaFile, string name = null)
		{
			_function = function;
			Name = name ?? "Anonymous";
			Event = theEvent;

			// When would a file be null?
			// When a script is loaded with a callback, but no infinite loop so it closes
			// Then that callback proceeds to register more callbacks
			// In these situations, we will generate a thread for this new callback on the fly here
			// Scenarios like this suggest that a thread being managed by a LuaFile is a bad idea,
			// and we should refactor
			if (luaFile == null)
			{
				DetachFromScript();
			}
			else
			{
				LuaFile = luaFile;
			}

			Guid = Guid.NewGuid();

			Callback = () =>
			{
				try
				{
					_function(Array.Empty<object>());
				}
				catch (Exception ex)
				{
					logCallback($"error running function attached by the event {Event}\nError message: {ex.Message}");
				}
			};

			MemCallback = (address, value, flags) => Callback();
		}

		public void DetachFromScript()
		{
			var thread = new Lua();
				
			// Current dir will have to do for now, but this will inevitably not be desired
			// Users will expect it to be the same directly as the thread that spawned this callback
			// But how do we know what that directory was?
			LuaSandbox.CreateSandbox(thread, ".");
			LuaFile = new LuaFile(".") { Thread = thread };
		}

		public Guid Guid { get; }

		public string Name { get; }

		public LuaFile LuaFile { get; private set; }

		public string Event { get; }

		public Action Callback { get; }

		public MemoryCallbackDelegate MemCallback { get; }

		public void Call(string name = null)
		{
			LuaSandbox.Sandbox(LuaFile.Thread, () =>
			{
				_function(new object[] { name });
			});
		}
	}
}
