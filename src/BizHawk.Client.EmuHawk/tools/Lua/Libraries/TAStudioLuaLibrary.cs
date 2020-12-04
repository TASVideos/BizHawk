using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;

// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo
namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the Tastudio dialog of the EmuHawk client")]
	[LuaLibrary(released: true)]
	public sealed class TAStudioLuaLibrary<TTable> : LuaLibraryBase<TTable>
	{
		public ToolManager Tools { get; set; }

		public TAStudioLuaLibrary(ILuaLibEnv<TTable> luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "tastudio";

		private TAStudio Tastudio => Tools.Get<TAStudio>() as TAStudio;

		private struct PendingChanges
		{
			public LuaChangeTypes Type;
			public InputChangeTypes InputType;
			public int Frame;
			public int Number;
			public string Button;
			public bool ValueBool;
			public int ValueAxis;
		}

		public enum LuaChangeTypes
		{
			InputChange,
			InsertFrames,
			DeleteFrames,
			ClearFrames
		}

		private enum InputChangeTypes
		{
			Bool,
			Axis
		}

		public class TastudioBranchInfo
		{
			public string Id { get; set; }
			public int Frame { get; set; }
			public string Text { get; set; }
		}

		private readonly List<PendingChanges> _changeList = new List<PendingChanges>(); //TODO: Initialize it to empty list on a script reload, and have each script have it's own list

		[LuaMethodExample("if ( tastudio.engaged( ) ) then\r\n\tconsole.log( \"returns whether or not tastudio is currently engaged ( active )\" );\r\nend;")]
		[LuaMethod("engaged", "returns whether or not tastudio is currently engaged (active)")]
		public bool Engaged()
		{
			return Tools.Has<TAStudio>(); // TODO: eventually tastudio should have an engaged flag
		}

		[LuaMethodExample("if ( tastudio.getrecording( ) ) then\r\n\tconsole.log( \"returns whether or not TAStudio is in recording mode\" );\r\nend;")]
		[LuaMethod("getrecording", "returns whether or not TAStudio is in recording mode")]
		public bool GetRecording()
		{
			return Tastudio.TasPlaybackBox.RecordingMode;
		}

		[LuaMethodExample("tastudio.setrecording( true );")]
		[LuaMethod("setrecording", "sets the recording mode on/off depending on the parameter")]
		public void SetRecording(bool val)
		{
			if (Tastudio.TasPlaybackBox.RecordingMode != val)
			{
				Tastudio.ToggleReadOnly();
			}
		}

		[LuaMethodExample("tastudio.togglerecording( );")]
		[LuaMethod("togglerecording", "toggles tastudio recording mode on/off depending on its current state")]
		public void SetRecording()
		{
			Tastudio.ToggleReadOnly();
		}

		[LuaMethodExample("local botasisl = tastudio.islag( 500 );")]
		[LuaMethod("islag", "Returns whether or not the given frame was a lag frame, null if unknown")]
		public bool? IsLag(int frame)
		{
			if (Engaged())
			{
				if (frame < Tastudio.CurrentTasMovie.InputLogLength)
				{
					return Tastudio.CurrentTasMovie[frame].Lagged;
				}
			}

			return null;
		}

		[LuaMethodExample("tastudio.setlag( 500, true );")]
		[LuaMethod("setlag", "Sets the lag information for the given frame, if the frame does not exist in the lag log, it will be added. If the value is null, the lag information for that frame will be removed")]
		public void SetLag(int frame, bool? value)
		{
			if (Engaged())
			{
				Tastudio.CurrentTasMovie.LagLog[frame] = value;
			}
		}

		[LuaMethodExample("if ( tastudio.hasstate( 500 ) ) then\r\n\tconsole.log( \"Returns whether or not the given frame has a savestate associated with it\" );\r\nend;")]
		[LuaMethod("hasstate", "Returns whether or not the given frame has a savestate associated with it")]
		public bool HasState(int frame)
		{
			if (Engaged())
			{
				if (frame < Tastudio.CurrentTasMovie.InputLogLength)
				{
					return Tastudio.CurrentTasMovie[frame].HasState;
				}
			}

			return false;
		}

		[LuaMethodExample("tastudio.setplayback( 1500 );")]
		[LuaMethod("setplayback", "Seeks the given frame (a number) or marker (a string)")]
		public void SetPlayback(object frame)
		{
			if (Engaged())
			{
				_luaLibsImpl.IsUpdateSupressed = true;

				int f;
				if (frame is double frameNumber)
				{
					f = (int)frameNumber;
				}
				else
				{
					f = Tastudio.CurrentTasMovie.Markers.FindIndex((string)frame);
					if (f == -1)
					{
						return;
					}

					f = Tastudio.CurrentTasMovie.Markers[f].Frame;
				}

				if (0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(f))
				{
					Tastudio.GoToFrame(f, true);
				}

				_luaLibsImpl.IsUpdateSupressed = false;
			}
		}

		[LuaMethodExample("local nltasget = tastudio.getselection( );")]
		[LuaMethod("getselection", "gets the currently selected frames")]
		public TTable GetSelection() => Engaged() ? _th.EnumerateToLuaTable(Tastudio.GetSelection()) : _th.CreateTable();

		[LuaMethodExample("")]
		[LuaMethod("submitinputchange", "")]
		public void SubmitInputChange(int frame, string button, bool value)
		{
			if (Engaged())
			{
				if (frame >= 0)
				{
					PendingChanges newChange = new PendingChanges();

					if (frame < Tastudio.CurrentTasMovie.InputLogLength)
					{
						if (Tastudio.CurrentTasMovie.BoolIsPressed(frame, button) != value) //Check if the button state is not already in the state the user set in the lua script
						{
							newChange.Type = LuaChangeTypes.InputChange;
							newChange.InputType = InputChangeTypes.Bool;
							newChange.Frame = frame;
							newChange.Button = button;
							newChange.ValueBool = value;

							_changeList.Add(newChange);
						}
					}
					else
					{
						newChange.Type = LuaChangeTypes.InputChange;
						newChange.InputType = InputChangeTypes.Bool;
						newChange.Frame = frame;
						newChange.Button = button;
						newChange.ValueBool = value;

						_changeList.Add(newChange);
					}
				}
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitanalogchange", "")]
		public void SubmitAnalogChange(int frame, string button, float value)
		{
			if (Engaged())
			{
				if (frame >= 0)
				{
					PendingChanges newChange = new PendingChanges();

					if (frame < Tastudio.CurrentTasMovie.InputLogLength)
					{
						if (Tastudio.CurrentTasMovie.GetAxisState(frame, button) != (int) value) // Check if the button state is not already in the state the user set in the lua script
						{
							newChange.Type = LuaChangeTypes.InputChange;
							newChange.InputType = InputChangeTypes.Axis;
							newChange.Frame = frame;
							newChange.Button = button;
							newChange.ValueAxis = (int) value;

							_changeList.Add(newChange);
						}
					}
					else
					{
						newChange.Type = LuaChangeTypes.InputChange;
						newChange.InputType = InputChangeTypes.Axis;
						newChange.Frame = frame;
						newChange.Button = button;
						newChange.ValueAxis = (int) value;

						_changeList.Add(newChange);
					}
				}
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitinsertframes", "")]
		public void SubmitInsertFrames(int frame, int number)
		{
			if (Engaged() && 0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(frame) && number > 0)
			{
				_changeList.Add(new PendingChanges
				{
					Type = LuaChangeTypes.InsertFrames,
					Frame = frame,
					Number = number
				});
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitdeleteframes", "")]
		public void SubmitDeleteFrames(int frame, int number)
		{
			if (Engaged() && 0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(frame) && number > 0)
			{
				_changeList.Add(new PendingChanges
				{
					Type = LuaChangeTypes.DeleteFrames,
					Frame = frame,
					Number = number
				});
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("submitclearframes", "")]
		public void SubmitClearFrames(int frame, int number)
		{
			if (Engaged() && 0.RangeToExclusive(Tastudio.CurrentTasMovie.InputLogLength).Contains(frame) && number > 0)
			{
				_changeList.Add(new PendingChanges
				{
					Type = LuaChangeTypes.ClearFrames,
					Frame = frame,
					Number = number
				});
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("applyinputchanges", "")]
		public void ApplyInputChanges()
		{
			if (Engaged())
			{
				_luaLibsImpl.IsUpdateSupressed = true;

				if (_changeList.Count > 0)
				{
					int size = _changeList.Count;

					for (int i = 0; i < size; i++)
					{
						switch (_changeList[i].Type)
						{
							case LuaChangeTypes.InputChange:
								switch (_changeList[i].InputType)
								{
									case InputChangeTypes.Bool:
										Tastudio.CurrentTasMovie.SetBoolState(_changeList[i].Frame, _changeList[i].Button, _changeList[i].ValueBool);
										break;
									case InputChangeTypes.Axis:
										Tastudio.CurrentTasMovie.SetAxisState(_changeList[i].Frame, _changeList[i].Button, _changeList[i].ValueAxis);
										break;
								}
								break;
							case LuaChangeTypes.InsertFrames:
								Tastudio.InsertNumFrames(_changeList[i].Frame, _changeList[i].Number);
								break;
							case LuaChangeTypes.DeleteFrames:
								Tastudio.DeleteFrames(_changeList[i].Frame, _changeList[i].Number);
								break;
							case LuaChangeTypes.ClearFrames:
								Tastudio.ClearFrames(_changeList[i].Frame, _changeList[i].Number);
								break;
						}
					}
					_changeList.Clear();
					Tastudio.Refresh();
					Tastudio.JumpToGreenzone();
					Tastudio.DoAutoRestore();
				}

				_luaLibsImpl.IsUpdateSupressed = false;
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("clearinputchanges", "")]
		public void ClearInputChanges()
		{
			if (Engaged())
			{
				_changeList.Clear();
			}
		}

		[LuaMethod("addcolumn", "")]
		public void AddColumn(string name, string text, int width)
		{
			if (Engaged())
			{
				Tastudio.AddColumn(name, text, width, ColumnType.Text);
			}
		}

		[LuaMethodExample("tastudio.setbranchtext( \"Some text\", 1 );")]
		[LuaMethod("setbranchtext", "adds the given message to the existing branch, or to the branch that will be created next if branch index is not specified")]
		public void SetBranchText(string text, int? index = null)
		{
			if (index != null)
			{
				var branch = Tastudio.CurrentTasMovie.Branches[index.Value];
				if (branch != null)
				{
					branch.UserText = text;
				}
			}
			else
			{
				Tastudio.CurrentTasMovie.Branches.NewBranchText = text;
			}
		}

		[LuaMethodExample("local nltasget = tastudio.getbranches( );")]
		[LuaMethod("getbranches", "Returns a list of the current tastudio branches.  Each entry will have the Id, Frame, and Text properties of the branch")]
		public TTable GetBranches()
		{
			if (!Engaged()) return _th.CreateTable();
			return _th.EnumerateToLuaTable(Tastudio.CurrentTasMovie.Branches.Select(b => new
			{
				Id = b.Uuid.ToString(),
				b.Frame,
				Text = b.UserText
			}));
		}

		[LuaMethodExample("local nltasget = tastudio.getbranchinput( \"97021544-2454-4483-824f-47f75e7fcb6a\", 500 );")]
		[LuaMethod("getbranchinput", "Gets the controller state of the given frame with the given branch identifier")]
		public TTable GetBranchInput(string branchId, int frame)
		{
			if (!Engaged()) return _th.CreateTable();

			var controller = Tastudio.GetBranchInput(branchId, frame);
			if (controller == null) return _th.CreateTable();

			var dict = new Dictionary<string, object>();
			foreach (var button in controller.Definition.BoolButtons)
			{
				dict[button] = controller.IsPressed(button);
			}
			foreach (var button in controller.Definition.Axes.Keys)
			{
				dict[button] = controller.AxisValue(button);
			}
			return _th.DictToTable(dict);
		}

		[LuaMethodExample("tastudio.loadbranch(0)")]
		[LuaMethod("loadbranch", "Loads a branch at the given index, if a branch at that index exists.")]
		public void LoadBranch(int index)
		{
			if (Engaged())
			{
				_luaLibsImpl.IsUpdateSupressed = true;

				Tastudio.LoadBranchByIndex(index);

				_luaLibsImpl.IsUpdateSupressed = false;
			}
		}

		[LuaMethodExample("local sttasget = tastudio.getmarker( 500 );")]
		[LuaMethod("getmarker", "returns the marker text at the given frame, or an empty string if there is no marker for the given frame")]
		public string GetMarker(int frame)
		{
			if (Engaged())
			{
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					return marker.Message;
				}
			}

			return "";
		}

		[LuaMethodExample("tastudio.removemarker( 500 );")]
		[LuaMethod("removemarker", "if there is a marker for the given frame, it will be removed")]
		public void RemoveMarker(int frame)
		{
			if (Engaged())
			{
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					Tastudio.CurrentTasMovie.Markers.Remove(marker);
					Tastudio.RefreshDialog();
				}
			}
		}

		[LuaMethodExample("tastudio.setmarker( 500, \"Some message\" );")]
		[LuaMethod("setmarker", "Adds or sets a marker at the given frame, with an optional message")]
		public void SetMarker(int frame, string message = null)
		{
			if (Engaged())
			{
				var marker = Tastudio.CurrentTasMovie.Markers.Get(frame);
				if (marker != null)
				{
					marker.Message = message;
				}
				else
				{
					Tastudio.CurrentTasMovie.Markers.Add(frame, message);
					Tastudio.RefreshDialog();
				}
			}
		}

		[LuaMethodExample("tastudio.onqueryitembg( function( currentindex, itemname )\r\n\tconsole.log( \"called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitembg", "called during the background draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemBg(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.QueryItemBgColorCallback = (index, name) =>
				{
					var result = wrappedf(new object[] { index, name });

					if (result != null)
					{
						var color = ToColor(result[0]);
						return color;
					}

					return null;
				};
			}
		}

		[LuaMethodExample("tastudio.onqueryitemtext( function( currentindex, itemname )\r\n\tconsole.log( \"called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitemtext", "called during the text draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemText(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.QueryItemTextCallback = (index, name) =>
				{
					var result = wrappedf(new object[] { index, name });
					return result?[0]?.ToString();
				};
			}
		}

		[LuaMethodExample("tastudio.onqueryitemicon( function( currentindex, itemname )\r\n\tconsole.log( \"called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)\" );\r\nend );")]
		[LuaMethod("onqueryitemicon", "called during the icon draw event of the tastudio listview. luaf must be a function that takes 2 params: index, column.  The first is the integer row index of the listview, and the 2nd is the string column name. luaf should return a value that can be parsed into a .NET Color object (string color name, or integer value)")]
		public void OnQueryItemIcon(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.QueryItemIconCallback = (index, name) =>
				{
					var result = wrappedf(new object[] { index, name });
					if (result?[0] != null)
					{
						string path = result[0].ToString();
						Icon icon = new Icon(path);
						return icon.ToBitmap();
					}

					return null;
				};
			}
		}

		[LuaMethodExample("tastudio.ongreenzoneinvalidated( function( currentindex, itemname )\r\n\tconsole.log( \"called whenever the greenzone is invalidated and returns the first frame that was invalidated\" );\r\nend );")]
		[LuaMethod("ongreenzoneinvalidated", "called whenever the greenzone is invalidated and returns the first frame that was invalidated")]
		public void OnGreenzoneInvalidated(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.GreenzoneInvalidatedCallback = index =>
				{
					wrappedf(new object[] { index });
				};
			}
		}

		[LuaMethodExample("tastudio.ongreenzoneinvalidated( function( currentindex, itemname )\r\n\tconsole.log( \"called whenever the greenzone is invalidated and returns the first frame that was invalidated\" );\r\nend );")]
		[LuaMethod("onbranchload", "called whenever a branch is loaded. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchLoad(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.BranchLoadedCallback = index =>
				{
					wrappedf(new object[] { index });
				};
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("onbranchsave", "called whenever a branch is created or updated. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchSave(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.BranchSavedCallback = index =>
				{
					wrappedf(new object[] { index });
				};
			}
		}

		[LuaMethodExample("")]
		[LuaMethod("onbranchremove", "called whenever a branch is removed. luaf must be a function that takes the integer branch index as a parameter")]
		public void OnBranchRemove(object luaf)
		{
			if (Engaged())
			{
				var wrappedf = _luaLibsImpl.WrapFunction(luaf);
				Tastudio.BranchRemovedCallback = index =>
				{
					wrappedf(new object[] { index });
				};
			}
		}
	}
}
