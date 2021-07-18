using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		// Input Painting
		private string _startBoolDrawColumn = "";
		private string _startAxisDrawColumn = "";
		private bool _boolPaintState;
		private int _axisPaintState;
		private int _axisBackupState;
		private bool _patternPaint;
		private bool _startCursorDrag;
		private bool _startSelectionDrag;
		private bool _selectionDragState;
		private bool _suppressContextMenu;
		private int _startRow;
		private int _paintingMinFrame = -1;
		private bool _playbackInterrupted; // Occurs when the emulator is unpaused and the user click and holds mouse down to begin delivering input

		// Editing analog input
		private string _axisEditColumn = "";
		private int _axisEditRow = -1;
		private string _axisTypedValue;
		private int _axisEditYPos = -1;
		private int AxisEditRow
		{
			set
			{
				_axisEditRow = value;
				TasView.SuspendHotkeys = AxisEditingMode;
			}
		}

		public bool AxisEditingMode => _axisEditRow != -1;

		private readonly List<int> _extraAxisRows = new List<int>();

		// Right-click dragging
		private string[] _rightClickInput;
		private string[] _rightClickOverInput;
		private int _rightClickFrame = -1;
		private int _rightClickLastFrame = -1;
		private bool _rightClickShift, _rightClickControl, _rightClickAlt;
		private bool _leftButtonHeld;

		private bool MouseButtonHeld => _rightClickFrame != -1 || _leftButtonHeld;

		private bool _triggerAutoRestore; // If true, autorestore will be called on mouse up
		private bool? _autoRestorePaused;
		private int? _seekStartFrame;
		private bool _unpauseAfterSeeking;

		private ControllerDefinition ControllerType => MovieSession.MovieController.Definition;

		public bool WasRecording { get; set; }
		public AutoPatternBool[] BoolPatterns;
		public AutoPatternAxis[] AxisPatterns;

		public void JumpToGreenzone()
		{
			if (Emulator.Frame > CurrentTasMovie.LastEditedFrame)
			{
				GoToLastEmulatedFrameIfNecessary(CurrentTasMovie.LastEditedFrame);
			}
		}

		private void StartSeeking(int? frame, bool fromMiddleClick = false)
		{
			if (!frame.HasValue)
			{
				return;
			}

			if (!fromMiddleClick)
			{
				if (MainForm.PauseOnFrame != null)
				{
					StopSeeking(true); // don't restore rec mode just yet, as with heavy editing checkbox updating causes lag
				}
				_seekStartFrame = Emulator.Frame;
			}

			MainForm.PauseOnFrame = frame.Value;
			int? diff = MainForm.PauseOnFrame - _seekStartFrame;

			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;
			TastudioPlayMode(); // suspend rec mode until seek ends, to allow mouse editing
			MainForm.UnpauseEmulator();

			if (diff > TasView.VisibleRows)
			{
				MessageStatusLabel.Text = "Seeking...";
				SavingProgressBar.Visible = true;
			}
		}

		public void StopSeeking(bool skipRecModeCheck = false)
		{
			if (WasRecording && !skipRecModeCheck)
			{
				TastudioRecordMode();
				WasRecording = false;
			}

			MainForm.PauseOnFrame = null;
			if (_unpauseAfterSeeking)
			{
				MainForm.UnpauseEmulator();
				_unpauseAfterSeeking = false;
			}

			if (CurrentTasMovie != null)
			{
				RefreshDialog();
			}
		}

		// public static Color CurrentFrame_FrameCol = Color.FromArgb(0xCF, 0xED, 0xFC); Why?
		public static Color CurrentFrame_InputLog => Color.FromArgb(0xB5, 0xE7, 0xF7);
		public static Color SeekFrame_InputLog => Color.FromArgb(0x70, 0xB5, 0xE7, 0xF7);

		public static Color GreenZone_FrameCol => Color.FromArgb(0xDD, 0xFF, 0xDD);
		public static Color GreenZone_InputLog => Color.FromArgb(0xD2, 0xF9, 0xD3);
		public static Color GreenZone_InputLog_Stated => Color.FromArgb(0xC4, 0xF7, 0xC8);
		public static Color GreenZone_InputLog_Invalidated => Color.FromArgb(0xE0, 0xFB, 0xE0);

		public static Color LagZone_FrameCol => Color.FromArgb(0xFF, 0xDC, 0xDD);
		public static Color LagZone_InputLog => Color.FromArgb(0xF4, 0xDA, 0xDA);
		public static Color LagZone_InputLog_Stated => Color.FromArgb(0xF0, 0xD0, 0xD2);
		public static Color LagZone_InputLog_Invalidated => Color.FromArgb(0xF7, 0xE5, 0xE5);

		public static Color Marker_FrameCol => Color.FromArgb(0xF7, 0xFF, 0xC9);
		public static Color AnalogEdit_Col => Color.FromArgb(0x90, 0x90, 0x70); // SuuperW: When editing an analog value, it will be a gray color.

		private Bitmap ts_v_arrow_green_blue => Properties.Resources.ts_v_arrow_green_blue;
		private Bitmap ts_h_arrow_green_blue => Properties.Resources.ts_h_arrow_green_blue;
		private Bitmap ts_v_arrow_blue => Properties.Resources.ts_v_arrow_blue;
		private Bitmap ts_h_arrow_blue => Properties.Resources.ts_h_arrow_blue;
		private Bitmap ts_v_arrow_green => Properties.Resources.ts_v_arrow_green;
		private Bitmap ts_h_arrow_green => Properties.Resources.ts_h_arrow_green;

		private Bitmap icon_marker => Properties.Resources.icon_marker;
		private Bitmap icon_anchor_lag => Properties.Resources.icon_anchor_lag;
		private Bitmap icon_anchor => Properties.Resources.icon_anchor;

		private void TasView_QueryItemIcon(int index, RollColumn column, ref Bitmap bitmap, ref int offsetX, ref int offsetY)
		{
			if (!_engaged || _initializing)
			{
				return;
			}

			var overrideIcon = QueryItemIconCallback?.Invoke(index, column.Name);

			if (overrideIcon != null)
			{
				bitmap = overrideIcon;
				return;
			}

			var columnName = column.Name;

			if (columnName == CursorColumnName)
			{
				if (TasView.HorizontalOrientation)
				{
					offsetX = 2;
					offsetY = 5;
				}

				if (index == Emulator.Frame && index == MainForm.PauseOnFrame)
				{
					bitmap = TasView.HorizontalOrientation ?
						ts_v_arrow_green_blue :
						ts_h_arrow_green_blue;
				}
				else if (index == Emulator.Frame)
				{
					bitmap = TasView.HorizontalOrientation ?
						ts_v_arrow_blue :
						ts_h_arrow_blue;
				}
				else if (index == LastPositionFrame)
				{
					bitmap = TasView.HorizontalOrientation ?
						ts_v_arrow_green :
						ts_h_arrow_green;
				}
			}
			else if (columnName == FrameColumnName)
			{
				var record = CurrentTasMovie[index];
				offsetX = -3;
				offsetY = 1;

				if (CurrentTasMovie.Markers.IsMarker(index) && Settings.DenoteMarkersWithIcons)
				{
					bitmap = icon_marker;
				}
				else if (record.HasState && Settings.DenoteStatesWithIcons)
				{
					if (record.Lagged.HasValue && record.Lagged.Value)
					{
						bitmap = icon_anchor_lag;
					}
					else
					{
						bitmap = icon_anchor;
					}
				}
			}
		}

		private void TasView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			if (!_engaged || _initializing)
			{
				return;
			}

			Color? overrideColor = QueryItemBgColorCallback?.Invoke(index, column.Name);

			if (overrideColor.HasValue)
			{
				color = overrideColor.Value;
				return;
			}

			string columnName = column.Name;

			if (columnName == CursorColumnName)
			{
				color = Color.FromArgb(0xFE, 0xFF, 0xFF);
			}

			if (columnName == FrameColumnName)
			{
				if (Emulator.Frame != index && CurrentTasMovie.Markers.IsMarker(index) && Settings.DenoteMarkersWithBGColor)
				{
					color = Marker_FrameCol;
				}
				else
				{
					color = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF);
				}
			}
			else if (AxisEditingMode
				&& (index == _axisEditRow || _extraAxisRows.Contains(index))
				&& columnName == _axisEditColumn)
			{
				color = AnalogEdit_Col;
			}

			int player = Emulator.ControllerDefinition.PlayerNumber(columnName);
			if (player != 0 && player % 2 == 0)
			{
				color = Color.FromArgb(0x0D, 0x00, 0x00, 0x00);
			}
		}

		private void TasView_QueryRowBkColor(int index, ref Color color)
		{
			if (!_engaged || _initializing)
			{
				return;
			}

			var record = CurrentTasMovie[index];

			if (MainForm.IsSeeking && MainForm.PauseOnFrame == index)
			{
				color = CurrentFrame_InputLog;
			}
			else if (!MainForm.IsSeeking && Emulator.Frame == index)
			{
				color = CurrentFrame_InputLog;
			}
			else if (record.Lagged.HasValue)
			{
				if (!record.HasState && Settings.DenoteStatesWithBGColor)
				{
					color = record.Lagged.Value
						? LagZone_InputLog
						: GreenZone_InputLog;
				}
				else
				{
					color = record.Lagged.Value
						? LagZone_InputLog_Stated
						: GreenZone_InputLog_Stated;
				}
			}
			else if (record.WasLagged.HasValue)
			{
				color = record.WasLagged.Value ?
					LagZone_InputLog_Invalidated :
					GreenZone_InputLog_Invalidated;
			}
			else
			{
				color = Color.FromArgb(0xFF, 0xFE, 0xEE);
			}
		}

		private void TasView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			if (!_engaged || _initializing)
			{
				text = "";
				return;
			}

			var overrideText = QueryItemTextCallback?.Invoke(index, column.Name);
			if (overrideText != null)
			{
				text = overrideText;
				return;
			}

			try
			{
				text = "";
				var columnName = column.Name;

				if (columnName == CursorColumnName)
				{
					int branchIndex = CurrentTasMovie.Branches.IndexOfFrame(index);
					if (branchIndex != -1)
					{
						text = branchIndex.ToString();
					}
				}
				else if (columnName == FrameColumnName)
				{
					offsetX = TasView.HorizontalOrientation ? 2 : 7;
					text = index.ToString().PadLeft(CurrentTasMovie.InputLogLength.ToString().Length, '0');
				}
				else
				{
					// Display typed float value (string "-" can't be parsed, so CurrentTasMovie.DisplayValue can't return it)
					if (index == _axisEditRow && columnName == _axisEditColumn)
					{
						text = _axisTypedValue;
					}
					else if (index < CurrentTasMovie.InputLogLength)
					{
						text = CurrentTasMovie.DisplayValue(index, columnName);
						if (column.Type == ColumnType.Axis)
						{
							// feos: this could be cached, but I don't notice any slowdown this way either
							if (text == ((float) ControllerType.Axes[columnName].Neutral).ToString())
							{
								text = "";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				text = "";
				DialogController.ShowMessageBox($"oops\n{ex}");
			}
		}

		private bool TasView_QueryFrameLag(int index, bool hideWasLag)
		{
			var lag = CurrentTasMovie[index];
			return (lag.Lagged.HasValue && lag.Lagged.Value) || (hideWasLag && lag.WasLagged.HasValue && lag.WasLagged.Value);
		}

		private void TasView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var columnName = e.Column.Name;

				if (columnName == FrameColumnName)
				{
					CurrentTasMovie.Markers.Add(TasView.LastSelectedIndex.Value, "");
				}
				else if (columnName != CursorColumnName)
				{
					int frame = TasView.SelectedRows.FirstOrDefault();
					string buttonName = TasView.CurrentCell.Column.Name;

					if (ControllerType.BoolButtons.Contains(buttonName))
					{
						if (ModifierKeys != Keys.Alt)
						{
							// nifty taseditor logic
							bool allPressed = true;
							foreach (var index in TasView.SelectedRows)
							{
								if (index == CurrentTasMovie.FrameCount // last movie frame can't have input, but can be selected
									|| !CurrentTasMovie.BoolIsPressed(index, buttonName))
								{
									allPressed = false;
									break;
								}
							}
							CurrentTasMovie.SetBoolStates(frame, TasView.SelectedRows.Count(), buttonName, !allPressed);
						}
						else
						{
							BoolPatterns[ControllerType.BoolButtons.IndexOf(buttonName)].Reset();
							foreach (var index in TasView.SelectedRows)
							{
								CurrentTasMovie.SetBoolState(index, buttonName, BoolPatterns[ControllerType.BoolButtons.IndexOf(buttonName)].GetNextValue());
							}
						}
					}
					else
					{
						// feos: there's no default value other than neutral, and we can't go arbitrary here, so do nothing for now
						// autohold is ignored for axes too for the same reasons: lack of demand + ambiguity
					}

					_triggerAutoRestore = true;
					JumpToGreenzone();
				}

				RefreshDialog();
			}
		}

		private void TasView_ColumnRightClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			e.Column.Emphasis ^= true;
			UpdateAutoFire(e.Column.Name, e.Column.Emphasis);
			TasView.Refresh();
		}

		private void UpdateAutoFire()
		{
			for (int i = 2; i < TasView.AllColumns.Count; i++)
			{
				UpdateAutoFire(TasView.AllColumns[i].Name, TasView.AllColumns[i].Emphasis);
			}
		}

		public void UpdateAutoFire(string button, bool? isOn)
		{
			if (!isOn.HasValue) // No value means don't change whether it's on or off.
			{
				isOn = TasView.AllColumns.Find(c => c.Name == button).Emphasis;
			}

			int index = 0;
			if (autoHoldToolStripMenuItem.Checked)
			{
				index = 1;
			}

			if (autoFireToolStripMenuItem.Checked)
			{
				index = 2;
			}

			if (ControllerType.BoolButtons.Contains(button))
			{
				if (index == 0)
				{
					index = ControllerType.BoolButtons.IndexOf(button);
				}
				else
				{
					index += ControllerType.BoolButtons.Count - 1;
				}

				// Fixes auto-loading, but why is this code like this? The code above suggests we have a BoolPattern for every  bool button? But we don't
				// This is a sign of a deeper problem, but this fixes some basic functionality at least
				if (index < BoolPatterns.Length)
				{
					AutoPatternBool p = BoolPatterns[index];
					InputManager.AutofireStickyXorAdapter.SetSticky(button, isOn.Value, p);
				}
			}
			else
			{
				if (index == 0)
				{
					index = ControllerType.Axes.IndexOf(button);
				}
				else
				{
					index += ControllerType.Axes.Count - 1;
				}

				int? value = null;
				if (isOn.Value)
				{
					value = 0;
				}

				// Fixes auto-loading, but why is this code like this? The code above suggests we have a AxisPattern for every axis button? But we don't
				// This is a sign of a deeper problem, but this fixes some basic functionality at least
				if (index < BoolPatterns.Length)
				{
					AutoPatternAxis p = AxisPatterns[index];
					InputManager.AutofireStickyXorAdapter.SetAxis(button, value, p);
				}
			}
		}

		private void TasView_ColumnReordered(object sender, InputRoll.ColumnReorderedEventArgs e)
		{
			CurrentTasMovie.FlagChanges();
		}

		private void TasView_MouseEnter(object sender, EventArgs e)
		{
			if (ContainsFocus)
			{
				TasView.Focus();
			}
		}

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			// Clicking with left while right is held or vice versa does weird stuff
			if (MouseButtonHeld)
			{
				return;
			}

			if (e.Button == MouseButtons.Middle)
			{
				if (MainForm.EmulatorPaused)
				{
					var record = CurrentTasMovie[LastPositionFrame];
					if (!record.Lagged.HasValue && LastPositionFrame > Emulator.Frame)
					{
						StartSeeking(LastPositionFrame, true);
						return;
					}
				}

				MainForm.TogglePause();
				return;
			}

			if (TasView.CurrentCell?.RowIndex == null || TasView.CurrentCell.Column == null)
			{
				return;
			}

			int frame = TasView.CurrentCell.RowIndex.Value;
			string buttonName = TasView.CurrentCell.Column.Name;
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;

			if (e.Button == MouseButtons.Left)
			{
				_leftButtonHeld = true;
				_paintingMinFrame = frame;

				// SuuperW: Exit axis editing mode, or re-enter mouse editing
				if (AxisEditingMode)
				{
					if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
					{
						_extraAxisRows.Clear();
						_extraAxisRows.AddRange(TasView.SelectedRows);
						_startSelectionDrag = true;
						_selectionDragState = TasView.SelectedRows.Contains(frame);
						return;
					}
					if (_axisEditColumn != buttonName
						|| !(_axisEditRow == frame || _extraAxisRows.Contains(frame)))
					{
						_extraAxisRows.Clear();
						AxisEditRow = -1;
						SetTasViewRowCount();
					}
					else
					{
						if (_extraAxisRows.Contains(frame))
						{
							_extraAxisRows.Clear();
							AxisEditRow = frame;
							SetTasViewRowCount();
						}

						_axisEditYPos = e.Y;
						_axisPaintState = CurrentTasMovie.GetAxisState(frame, buttonName);
						
						_triggerAutoRestore = true;
						return;
					}
				}

				if (TasView.CurrentCell.Column.Name == CursorColumnName)
				{
					_startCursorDrag = true;
					GoToFrame(TasView.CurrentCell.RowIndex.Value);
				}
				else if (TasView.CurrentCell.Column.Name == FrameColumnName)
				{
					if (ModifierKeys == Keys.Alt && CurrentTasMovie.Markers.IsMarker(frame))
					{
						// TODO
						TasView.DragCurrentCell();
					}
					else
					{
						_startSelectionDrag = true;
						_selectionDragState = TasView.SelectedRows.Contains(frame);
					}
				}
				else if (TasView.CurrentCell.Column.Type != ColumnType.Text) // User changed input
				{
					_playbackInterrupted = !MainForm.EmulatorPaused;
					MainForm.PauseEmulator();

					// Pausing the emulator is insufficient to actually stop frame advancing as the frame advance hotkey can
					// still take effect. This can lead to desyncs by simultaneously changing input and frame advancing.
					// So we want to block all frame advance operations while the user is changing input in the piano roll
					MainForm.BlockFrameAdvance = true;

					if (ControllerType.BoolButtons.Contains(buttonName))
					{
						_patternPaint = false;
						_startBoolDrawColumn = buttonName;

						if ((ModifierKeys == Keys.Alt && ModifierKeys != Keys.Shift) || (applyPatternToPaintedInputToolStripMenuItem.Checked && (!onlyOnAutoFireColumnsToolStripMenuItem.Checked
							|| TasView.CurrentCell.Column.Emphasis)))
						{
							BoolPatterns[ControllerType.BoolButtons.IndexOf(buttonName)].Reset();
							_patternPaint = true;
							_startRow = TasView.CurrentCell.RowIndex.Value;
							_boolPaintState = !CurrentTasMovie.BoolIsPressed(frame, buttonName);
						}
						else if (ModifierKeys == Keys.Shift && ModifierKeys != Keys.Alt)
						{
							if (!TasView.AnyRowsSelected) return;
							int firstSel = TasView.SelectedRows.First();

							if (frame <= firstSel)
							{
								firstSel = frame;
								frame = TasView.SelectedRows.First();
							}

							bool allPressed = true;
							for (int i = firstSel; i <= frame; i++)
							{
								if (i == CurrentTasMovie.FrameCount // last movie frame can't have input, but can be selected
									|| !CurrentTasMovie.BoolIsPressed(i, buttonName))
								{
									allPressed = false;
									break;
								}
							}
							CurrentTasMovie.SetBoolStates(firstSel, (frame - firstSel) + 1, buttonName, !allPressed);
							_boolPaintState = CurrentTasMovie.BoolIsPressed(frame, buttonName);
							_triggerAutoRestore = true;
							JumpToGreenzone();
							RefreshDialog();
						}
						else if (ModifierKeys == Keys.Shift && ModifierKeys == Keys.Alt) // Does not work?
						{
							// TODO: Pattern drawing from selection to current cell
						}
						else
						{
							CurrentTasMovie.ChangeLog.BeginNewBatch($"Paint Bool {buttonName} from frame {frame}");

							CurrentTasMovie.ToggleBoolState(TasView.CurrentCell.RowIndex.Value, buttonName);
							_boolPaintState = CurrentTasMovie.BoolIsPressed(frame, buttonName);
							_triggerAutoRestore = true;
							JumpToGreenzone();
							RefreshDialog();
						}
					}
					else
					{
						if (frame >= CurrentTasMovie.InputLogLength)
						{
							CurrentTasMovie.SetAxisState(frame, buttonName, 0);
							RefreshDialog();
						}

						_axisPaintState = CurrentTasMovie.GetAxisState(frame, buttonName);
						if (applyPatternToPaintedInputToolStripMenuItem.Checked && (!onlyOnAutoFireColumnsToolStripMenuItem.Checked
							|| TasView.CurrentCell.Column.Emphasis))
						{
							AxisPatterns[ControllerType.Axes.IndexOf(buttonName)].Reset();
							CurrentTasMovie.SetAxisState(frame, buttonName, AxisPatterns[ControllerType.Axes.IndexOf(buttonName)].GetNextValue());
							_patternPaint = true;
						}
						else
						{
							_patternPaint = false;
						}


						if (e.Clicks != 2 && !Settings.SingleClickAxisEdit)
						{
							CurrentTasMovie.ChangeLog.BeginNewBatch($"Paint Axis {buttonName} from frame {frame}");
							_startAxisDrawColumn = buttonName;
						}
						else // Double-click enters axis editing mode
						{
							if (_axisEditColumn == buttonName && _axisEditRow == frame)
							{
								AxisEditRow = -1;
							}
							else
							{
								CurrentTasMovie.ChangeLog.BeginNewBatch($"Axis Edit: {frame}");
								_axisEditColumn = buttonName;
								AxisEditRow = frame;
								_axisTypedValue = "";
								_axisEditYPos = e.Y;
								_axisBackupState = CurrentTasMovie.GetAxisState(_axisEditRow, _axisEditColumn);
							}

							RefreshDialog();
						}
					}
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				if (TasView.CurrentCell.Column.Name == FrameColumnName && frame < CurrentTasMovie.InputLogLength)
				{
					_rightClickControl = (ModifierKeys | Keys.Control) == ModifierKeys;
					_rightClickShift = (ModifierKeys | Keys.Shift) == ModifierKeys;
					_rightClickAlt = (ModifierKeys | Keys.Alt) == ModifierKeys;
					if (TasView.SelectedRows.Contains(frame))
					{
						_rightClickInput = new string[TasView.SelectedRows.Count()];
						_rightClickFrame = TasView.FirstSelectedIndex.Value;
						try
						{
							CurrentTasMovie.GetLogEntries().CopyTo(_rightClickFrame, _rightClickInput, 0, TasView.SelectedRows.Count());
						}
						catch { }
						if (_rightClickControl && _rightClickShift)
						{
							_rightClickFrame += _rightClickInput.Length;
						}
					}
					else
					{
						_rightClickInput = new string[1];
						_rightClickInput[0] = CurrentTasMovie.GetInputLogEntry(frame);
						_rightClickFrame = frame;
					}

					_rightClickLastFrame = -1;

					if (_rightClickAlt || _rightClickControl || _rightClickShift)
					{
						JumpToGreenzone();

						// TODO: Turn off ChangeLog.IsRecording and handle the GeneralUndo here.
						string undoStepName = "Right-Click Edit:";
						if (_rightClickShift)
						{
							undoStepName += " Extend Input";
							if (_rightClickControl)
							{
								undoStepName += ", Insert";
							}
						}
						else
						{
							if (_rightClickControl)
							{
								undoStepName += " Copy";
							}
							else // _rightClickAlt
							{
								undoStepName += " Move";
							}
						}

						CurrentTasMovie.ChangeLog.BeginNewBatch(undoStepName);
					}
				}
			}
		}

		private void ClearLeftMouseStates()
		{
			_startCursorDrag = false;
			_startSelectionDrag = false;
			_startBoolDrawColumn = "";
			_startAxisDrawColumn = "";
			_paintingMinFrame = -1;
			TasView.ReleaseCurrentCell();

			// Exit axis editing if value was changed with cursor
			if (AxisEditingMode && _axisPaintState != CurrentTasMovie.GetAxisState(_axisEditRow, _axisEditColumn))
			{
				AxisEditRow = -1;
				_triggerAutoRestore = true;
				JumpToGreenzone();
				DoTriggeredAutoRestoreIfNeeded();
				RefreshDialog();
			}
			_axisPaintState = 0;
			_axisEditYPos = -1;
			_leftButtonHeld = false;

			if (!AxisEditingMode)
			{
				CurrentTasMovie.ChangeLog?.EndBatch();
			}

			MainForm.BlockFrameAdvance = false;
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && !TasView.IsPointingAtColumnHeader &&
				!_suppressContextMenu && TasView.SelectedRows.Any() && !_leftButtonHeld)
			{
				if (CurrentTasMovie.FrameCount < TasView.SelectedRows.Max())
				{
					// trying to be smart here
					// if a loaded branch log is shorter than selection, keep selection until you attempt to call context menu
					// you might need it when you load again the branch where this frame exists
					TasView.DeselectAll();
					SetTasViewRowCount();
				}
				else
				{
					RightClickMenu.Show(TasView, e.X, e.Y);
				}
			}
			else if (e.Button == MouseButtons.Left)
			{
				if (AxisEditingMode && (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift))
				{
					_leftButtonHeld = false;
					_startSelectionDrag = false;
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(_startBoolDrawColumn))
					{
						// If painting up, we have altered frames without loading states (for smoothness)
						// So now we have to ensure that all the edited frames are invalidated
						if (_paintingMinFrame < Emulator.Frame)
						{
							GoToFrame(_paintingMinFrame);
						}
					}

					ClearLeftMouseStates();
				}

				DoTriggeredAutoRestoreIfNeeded();
			}

			if (e.Button == MouseButtons.Right)
			{
				if (_rightClickFrame != -1)
				{
					_rightClickInput = null;
					_rightClickOverInput = null;
					_rightClickFrame = -1;
					CurrentTasMovie.ChangeLog.EndBatch();
				}
			}

			_suppressContextMenu = false;
		}

		private void TasView_MouseWheel(object sender, MouseEventArgs e)
		{
			if (TasView.RightButtonHeld && TasView?.CurrentCell.RowIndex.HasValue == true)
			{
				_suppressContextMenu = true;
				int notch = e.Delta / 120;
				if (notch > 1)
				{
					notch *= 2;
				}

				// warning: tastudio rewind hotkey/button logic is copy pasted from here!
				if (MainForm.IsSeeking && !MainForm.EmulatorPaused)
				{
					MainForm.PauseOnFrame -= notch;

					// that's a weird condition here, but for whatever reason it works best
					if (notch > 0 && Emulator.Frame >= MainForm.PauseOnFrame)
					{
						MainForm.PauseEmulator();
						MainForm.PauseOnFrame = null;
						StopSeeking();
						GoToFrame(Emulator.Frame - notch);
					}

					RefreshDialog();
				}
				else
				{
					GoToFrame(Emulator.Frame - notch);
				}
			}
		}

		private void TasView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (TasView.CurrentCell.Column == null)
			{
				return;
			}

			if (e.Button == MouseButtons.Left)
			{
				if (TasView.CurrentCell.RowIndex.HasValue &&
					TasView.CurrentCell.Column.Name == FrameColumnName &&
					!AxisEditingMode)
				{
					var existingMarker = CurrentTasMovie.Markers.FirstOrDefault(m => m.Frame == TasView.CurrentCell.RowIndex.Value);

					if (existingMarker != null)
					{
						MarkerControl.EditMarkerPopUp(existingMarker, true);
					}
					else
					{
						if (Settings.EmptyMarkers)
						{
							CurrentTasMovie.Markers.Add(TasView.CurrentCell.RowIndex.Value, "");
							RefreshDialog();
						}
						else
						{
							ClearLeftMouseStates();
							MarkerControl.AddMarker(TasView.CurrentCell.RowIndex.Value, false);
						}
					}
				}
			}
		}

		private void TasView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			// TODO: If NewCell is null, it indicates that there was a mouse leave scenario, we may want to account for that
			// For now return if a null because this happens OnEnter which doesn't have any of the below behaviors yet
			if (e.OldCell?.Column == null || e.OldCell?.RowIndex == null
				|| e.NewCell?.Column == null || e.NewCell?.RowIndex == null)
			{
				return;
			}

			if (!MouseButtonHeld)
			{
				return;
			}

			if (_paintingMinFrame >= 0)
			{
				_paintingMinFrame = Math.Min(_paintingMinFrame, e.NewCell?.RowIndex ?? 0);
			}

			// skip rerecord counting on drawing entirely, mouse down is enough
			// avoid introducing another global
			bool wasCountingRerecords = CurrentTasMovie.IsCountingRerecords;
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;

			int startVal, endVal;
			int frame = e.NewCell.RowIndex.Value;
			if (e.OldCell.RowIndex.Value < e.NewCell.RowIndex.Value)
			{
				startVal = e.OldCell.RowIndex.Value;
				endVal = e.NewCell.RowIndex.Value;
				if (_patternPaint)
				{
					endVal--;
				}
			}
			else
			{
				startVal = e.NewCell.RowIndex.Value;
				endVal = e.OldCell.RowIndex.Value;
				if(_patternPaint)
				{
					endVal = _startRow;
				}
			}

			if (_startCursorDrag && !MainForm.IsSeeking)
			{
				GoToFrame(e.NewCell.RowIndex.Value);
			}
			else if (_startSelectionDrag)
			{
				for (var i = startVal; i <= endVal; i++)
				{
					TasView.SelectRow(i, _selectionDragState);
					if (AxisEditingMode && (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift))
					{
						if (_selectionDragState)
						{
							_extraAxisRows.Add(i);
						}
						else
						{
							_extraAxisRows.Remove(i);
						}
					}
				}

				SetSplicer();
			}
			else if (_rightClickFrame != -1)
			{
				if (frame > CurrentTasMovie.InputLogLength - _rightClickInput.Length)
				{
					frame = CurrentTasMovie.InputLogLength - _rightClickInput.Length;
				}

				if (_rightClickShift)
				{
					if (_rightClickControl) // Insert
					{
						// If going backwards, delete!
						bool shouldInsert = true;
						if (startVal < _rightClickFrame)
						{
							// Cloning to a previous frame makes no sense.
							startVal = _rightClickFrame - 1;
						}

						if (startVal < _rightClickLastFrame)
						{
							shouldInsert = false;
						}

						if (shouldInsert)
						{
							for (int i = startVal + 1; i <= endVal; i++)
							{
								CurrentTasMovie.InsertInput(i, _rightClickInput[(i - _rightClickFrame).Mod(_rightClickInput.Length)]);
							}
						}
						else
						{
							CurrentTasMovie.RemoveFrames(startVal + 1, endVal + 1);
						}

						_rightClickLastFrame = frame;
					}
					else // Overwrite
					{
						for (int i = startVal; i <= endVal; i++)
						{
							CurrentTasMovie.SetFrame(i, _rightClickInput[(i - _rightClickFrame).Mod(_rightClickInput.Length)]);
						}
					}
				}
				else
				{
					if (_rightClickControl)
					{
						for (int i = 0; i < _rightClickInput.Length; i++) // Re-set initial range, just to verify it's still there.
						{
							CurrentTasMovie.SetFrame(_rightClickFrame + i, _rightClickInput[i]);
						}

						if (_rightClickOverInput != null) // Restore overwritten input from previous movement
						{
							for (int i = 0; i < _rightClickOverInput.Length; i++)
							{
								CurrentTasMovie.SetFrame(_rightClickLastFrame + i, _rightClickOverInput[i]);
							}
						}
						else
						{
							_rightClickOverInput = new string[_rightClickInput.Length];
						}

						_rightClickLastFrame = frame; // Set new restore log
						CurrentTasMovie.GetLogEntries().CopyTo(frame, _rightClickOverInput, 0, _rightClickOverInput.Length);

						for (int i = 0; i < _rightClickInput.Length; i++) // Place copied input
						{
							CurrentTasMovie.SetFrame(frame + i, _rightClickInput[i]);
						}
					}
					else if (_rightClickAlt)
					{
						int shiftBy = _rightClickFrame - frame;
						string[] shiftInput = new string[Math.Abs(shiftBy)];
						int shiftFrom = frame;
						if (shiftBy < 0)
						{
							shiftFrom = _rightClickFrame + _rightClickInput.Length;
						}

						CurrentTasMovie.GetLogEntries().CopyTo(shiftFrom, shiftInput, 0, shiftInput.Length);
						int shiftTo = shiftFrom + (_rightClickInput.Length * Math.Sign(shiftBy));
						for (int i = 0; i < shiftInput.Length; i++)
						{
							CurrentTasMovie.SetFrame(shiftTo + i, shiftInput[i]);
						}

						for (int i = 0; i < _rightClickInput.Length; i++)
						{
							CurrentTasMovie.SetFrame(frame + i, _rightClickInput[i]);
						}

						_rightClickFrame = frame;
					}
				}

				if (_rightClickAlt || _rightClickControl || _rightClickShift)
				{
					_triggerAutoRestore = true;
					JumpToGreenzone();
					_suppressContextMenu = true;
				}
			}

			// Left-click
			else if (TasView.IsPaintDown && !string.IsNullOrEmpty(_startBoolDrawColumn))
			{
				CurrentTasMovie.IsCountingRerecords = false;

				for (int i = startVal; i <= endVal; i++) // Inclusive on both ends (drawing up or down)
				{
					bool setVal = _boolPaintState;

					if (_patternPaint && _boolPaintState)
					{
						if (CurrentTasMovie[frame].Lagged.HasValue && CurrentTasMovie[frame].Lagged.Value)
						{
							setVal = CurrentTasMovie.BoolIsPressed(i - 1, _startBoolDrawColumn);
						}
						else
						{
							setVal = BoolPatterns[ControllerType.BoolButtons.IndexOf(_startBoolDrawColumn)].GetNextValue();
						}
					}

					CurrentTasMovie.SetBoolState(i, _startBoolDrawColumn, setVal); // Notice it uses new row, old column, you can only paint across a single column

					if (!_triggerAutoRestore)
					{
						JumpToGreenzone();
					}
				}
			}

			else if (TasView.IsPaintDown && !string.IsNullOrEmpty(_startAxisDrawColumn))
			{
				CurrentTasMovie.IsCountingRerecords = false;

				for (int i = startVal; i <= endVal; i++) // Inclusive on both ends (drawing up or down)
				{
					var setVal = _axisPaintState;
					if (_patternPaint)
					{
						if (CurrentTasMovie[frame].Lagged.HasValue && CurrentTasMovie[frame].Lagged.Value)
						{
							setVal = CurrentTasMovie.GetAxisState(i - 1, _startAxisDrawColumn);
						}
						else
						{
							setVal = AxisPatterns[ControllerType.Axes.IndexOf(_startAxisDrawColumn)].GetNextValue();
						}
					}

					var getVal = (i < CurrentTasMovie.InputLogLength) ? CurrentTasMovie.GetAxisState(i, _startAxisDrawColumn) : setVal;
					CurrentTasMovie.SetAxisState(i, _startAxisDrawColumn, setVal); // Notice it uses new row, old column, you can only paint across a single column

					if (getVal != setVal) { JumpToGreenzone(); }
				}				
			}

			CurrentTasMovie.IsCountingRerecords = wasCountingRerecords;

			if (MouseButtonHeld)
			{
				TasView.MakeIndexVisible(TasView.CurrentCell.RowIndex.Value); // todo: limit scrolling speed
			}

			SetTasViewRowCount();
		}

		private void TasView_MouseMove(object sender, MouseEventArgs e)
		{
			// For axis editing
			if (AxisEditingMode)
			{
				int increment = (_axisEditYPos - e.Y) / 4;
				if (_axisEditYPos == -1)
				{
					return;
				}

				var value = (_axisPaintState + increment).ConstrainWithin(ControllerType.Axes[_axisEditColumn].Range);
				CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, value);
				_axisTypedValue = value.ToString();
				RefreshDialog();
			}
		}

		private void TasView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
		}

		public void AnalogIncrementByOne()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Up));
			}
		}

		public void AnalogDecrementByOne()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Down));
			}
		}

		public void AnalogIncrementByTen()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Up | Keys.Shift));
			}
		}

		public void AnalogDecrementByTen()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Down | Keys.Shift));
			}
		}

		public void AnalogMax()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Right));
			}
		}

		public void AnalogMin()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Left));
			}
		}

		public void EditAnalogProgrammatically(KeyEventArgs e)
		{
			if (!AxisEditingMode)
			{
				return;
			}

			float value = CurrentTasMovie.GetAxisState(_axisEditRow, _axisEditColumn);
			float prev = value;
			string prevTyped = _axisTypedValue;

			var range = ControllerType.Axes[_axisEditColumn];
			float rMin = range.Min;
			float rMax = range.Max;

			// feos: typing past max digits overwrites existing value, not touching the sign
			// but doesn't handle situations where the range is like -50 through 100, where minimum is negative and has less digits
			// it just uses 3 as maxDigits there too, leaving room for typing impossible values (that are still ignored by the game and then clamped)
			int maxDigits = range.MaxDigits;
			int curDigits = _axisTypedValue.Length;
			string curMinus;
			if (_axisTypedValue.StartsWith("-"))
			{
				curDigits -= 1;
				curMinus = "-";
			}
			else
			{
				curMinus = "";
			}

			if (e.KeyCode == Keys.Right)
			{
				value = rMax;
				_axisTypedValue = value.ToString();
			}
			else if (e.KeyCode == Keys.Left)
			{
				value = rMin;
				_axisTypedValue = value.ToString();
			}
			else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
			{
				if (curDigits >= maxDigits)
				{
					_axisTypedValue = curMinus;
				}

				_axisTypedValue += e.KeyCode - Keys.D0;
			}
			else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
			{
				if (curDigits >= maxDigits)
				{
					_axisTypedValue = curMinus;
				}

				_axisTypedValue += e.KeyCode - Keys.NumPad0;
			}
			else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
			{
				_axisTypedValue = _axisTypedValue.StartsWith("-")
					? _axisTypedValue.Substring(1)
					: $"-{_axisTypedValue}";
			}
			else if (e.KeyCode == Keys.Back)
			{
				if (_axisTypedValue == "") // Very first key press is backspace?
				{
					_axisTypedValue = value.ToString();
				}

				_axisTypedValue = _axisTypedValue.Substring(0, _axisTypedValue.Length - 1);
				if (_axisTypedValue == "" || _axisTypedValue == "-")
				{
					value = 0f;
				}
				else
				{
					value = Convert.ToSingle(_axisTypedValue);
				}
			}
			else if (e.KeyCode == Keys.Enter)
			{
				_axisEditYPos = -1;
				AxisEditRow = -1;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				_axisEditYPos = -1;

				if (_axisBackupState != _axisPaintState)
				{
					CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, _axisBackupState);
					_triggerAutoRestore = Emulator.Frame > _axisEditRow;
					JumpToGreenzone();
					DoTriggeredAutoRestoreIfNeeded();
				}

				AxisEditRow = -1;
			}
			else
			{
				float changeBy = 0;
				if (e.KeyCode == Keys.Up)
				{
					changeBy = 1; // We're assuming for now that ALL axis controls should contain integers.
				}
				else if (e.KeyCode == Keys.Down)
				{
					changeBy = -1;
				}

				if (ModifierKeys == Keys.Shift)
				{
					changeBy *= 10;
				}

				value += changeBy;
				if (changeBy != 0)
				{
					_axisTypedValue = value.ToString();
				}
			}

			if (!AxisEditingMode)
			{
				CurrentTasMovie.ChangeLog.EndBatch();
			}
			else
			{
				if (_axisTypedValue == "")
				{
					if (prevTyped != "")
					{
						value = 0f;
						CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, (int) value);
					}
				}
				else
				{
					if (float.TryParse(_axisTypedValue, out value)) // String "-" can't be parsed.
					{
						if (value > rMax)
						{
							value = rMax;
						}
						else if (value < rMin)
						{
							value = rMin;
						}

						_axisTypedValue = value.ToString();
						CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, (int) value);
					}
				}

				if (_extraAxisRows.Any())
				{
					foreach (int row in _extraAxisRows)
					{
						CurrentTasMovie.SetAxisState(row, _axisEditColumn, (int) value);
					}
				}

				if (value != prev) // Auto-restore
				{
					_triggerAutoRestore = Emulator.Frame > _axisEditRow;
					JumpToGreenzone();
					DoTriggeredAutoRestoreIfNeeded();
				}
			}

			RefreshDialog();
		}

		private void TasView_KeyDown(object sender, KeyEventArgs e)
		{
			// taseditor uses Ctrl for selection and Shift for frame cursor
			if (e.IsShift(Keys.PageUp))
			{
				GoToPreviousMarker();
			}
			else if (e.IsShift(Keys.PageDown))
			{
				GoToNextMarker();
			}
			else if (e.IsShift(Keys.Home))
			{
				GoToFrame(0);
			}
			else if (e.IsShift(Keys.End))
			{
				GoToFrame(CurrentTasMovie.InputLogLength-1);
			}

			if (AxisEditingMode
				&& e.KeyCode != Keys.Right
				&& e.KeyCode != Keys.Left
				&& e.KeyCode != Keys.Up
				&& e.KeyCode != Keys.Down)
			{
				EditAnalogProgrammatically(e);
			}

			RefreshDialog();
		}
	}
}
