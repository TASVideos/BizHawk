using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : ToolFormBase, IToolFormAutoConfig, IControlMainform
	{
		public override bool BlocksInputWhenFocused => IsInMenuLoop;

		public new IMainFormForTools MainForm => base.MainForm;

		public new IMovieSession MovieSession => base.MovieSession;

		// TODO: UI flow that conveniently allows to start from savestate
		public ITasMovie CurrentTasMovie => MovieSession.Movie as ITasMovie;

		public bool IsInMenuLoop { get; private set; }
		public string StatesPath => Config.PathEntries.TastudioStatesAbsolutePath();

		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();
		private const string CursorColumnName = "CursorColumn";
		private const string FrameColumnName = "FrameColumn";
		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)
		private UndoHistoryForm _undoForm;
		private Timer _autosaveTimer;

		private readonly int _defaultMainSplitDistance;
		private readonly int _defaultBranchMarkerSplitDistance;

		private bool _initialized;
		private bool _exiting;
		private bool _engaged;

		private bool CanAutoload => Settings.RecentTas.AutoLoad && !string.IsNullOrEmpty(Settings.RecentTas.MostRecent);

		/// <summary>
		/// Gets a value that separates "restore last position" logic from seeking caused by navigation.
		/// TASEditor never kills LastPositionFrame, and it only pauses on it, if it hasn't been greenzoned beforehand and middle mouse button was pressed.
		/// </summary>
		public int LastPositionFrame { get; private set; }

		[ConfigPersist]
		public TAStudioSettings Settings { get; set; } = new TAStudioSettings();

		[ConfigPersist]
		public Font TasViewFont { get; set; } = new Font("Arial", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);

		public class TAStudioSettings
		{
			public TAStudioSettings()
			{
				RecentTas = new RecentFiles(8);
				AutoPause = true;
				FollowCursor = true;
				ScrollSpeed = 6;
				FollowCursorAlwaysScroll = false;
				FollowCursorScrollMethod = "near";
				BranchCellHoverInterval = 1;
				SeekingCutoffInterval = 2;
				AutosaveInterval = 120000;
				AutosaveAsBk2 = false;
				AutosaveAsBackupFile = false;
				BackupPerFileSave = false;
				SingleClickAxisEdit = false;
				OldControlSchemeForBranches = false;
				LoadBranchOnDoubleClick = true;

				// default to taseditor fashion
				DenoteStatesWithIcons = false;
				DenoteStatesWithBGColor = true;
				DenoteMarkersWithIcons = false;
				DenoteMarkersWithBGColor = true;
			}

			public RecentFiles RecentTas { get; set; }
			public bool AutoPause { get; set; }
			public bool AutoRestoreLastPosition { get; set; }
			public bool FollowCursor { get; set; }
			public bool EmptyMarkers { get; set; }
			public int ScrollSpeed { get; set; }
			public bool FollowCursorAlwaysScroll { get; set; }
			public string FollowCursorScrollMethod { get; set; }
			public int BranchCellHoverInterval { get; set; }
			public int SeekingCutoffInterval { get; set; } // unused, relying on VisibleRows is smarter
			public uint AutosaveInterval { get; set; }
			public bool AutosaveAsBk2 { get; set; }
			public bool AutosaveAsBackupFile { get; set; }
			public bool BackupPerFileSave { get; set; }
			public bool SingleClickAxisEdit { get; set; }
			public bool OldControlSchemeForBranches { get; set; } // branch loading will behave differently depending on the recording mode
			public bool LoadBranchOnDoubleClick { get; set; }
			public bool DenoteStatesWithIcons { get; set; }
			public bool DenoteStatesWithBGColor { get; set; }
			public bool DenoteMarkersWithIcons { get; set; }
			public bool DenoteMarkersWithBGColor { get; set; }
			public int MainVerticalSplitDistance { get; set; }
			public int BranchMarkerSplitDistance { get; set; }
			public bool BindMarkersToInput { get; set; }
		}

		public TAStudio()
		{
			InitializeComponent();

			RecentSubMenu.Image = Resources.Recent;
			recentMacrosToolStripMenuItem.Image = Resources.Recent;
			TASEditorManualOnlineMenuItem.Image = Resources.Help;
			ForumThreadMenuItem.Image = Resources.TAStudio;
			Icon = Resources.TAStudioIcon;

			_defaultMainSplitDistance = MainVertialSplit.SplitterDistance;
			_defaultBranchMarkerSplitDistance = BranchesMarkersSplit.SplitterDistance;

			// TODO: show this at all times or hide it when saving is done?
			SavingProgressBar.Visible = false;

			WantsToControlStopMovie = true;
			WantsToControlRestartMovie = true;
			TasPlaybackBox.Tastudio = this;
			MarkerControl.Tastudio = this;
			BookMarkControl.Tastudio = this;
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.QueryRowBkColor += TasView_QueryRowBkColor;
			TasView.QueryItemIcon += TasView_QueryItemIcon;
			TasView.QueryFrameLag += TasView_QueryFrameLag;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
			LastPositionFrame = -1;

			BookMarkControl.LoadedCallback = BranchLoaded;
			BookMarkControl.SavedCallback = BranchSaved;
			BookMarkControl.RemovedCallback = BranchRemoved;
			TasView.MouseLeave += TAStudio_MouseLeave;
		}

		private void Tastudio_Load(object sender, EventArgs e)
		{
			if (!Engage())
			{
				Close();
				return;
			}

			if (TasView.Rotatable)
			{
				RightClickMenu.Items.AddRange(TasView.GenerateContextMenuItems()
					.ToArray());

				RightClickMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(t => t.Name == "RotateMenuItem")
					.Click += (o, ov) => { CurrentTasMovie.FlagChanges(); };
			}

			TasView.ScrollSpeed = Settings.ScrollSpeed;
			TasView.AlwaysScroll = Settings.FollowCursorAlwaysScroll;
			TasView.ScrollMethod = Settings.FollowCursorScrollMethod;
			TasView.SeekingCutoffInterval = Settings.SeekingCutoffInterval;
			BookMarkControl.HoverInterval = Settings.BranchCellHoverInterval;

			_autosaveTimer = new Timer(components);
			_autosaveTimer.Tick += AutosaveTimerEventProcessor;
			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Interval = (int)Settings.AutosaveInterval;
				_autosaveTimer.Start();
			}

			MainVertialSplit.SetDistanceOrDefault(
				Settings.MainVerticalSplitDistance,
				_defaultMainSplitDistance);

			BranchesMarkersSplit.SetDistanceOrDefault(
				Settings.BranchMarkerSplitDistance,
				_defaultBranchMarkerSplitDistance);

			TasView.Font = TasViewFont;
			CurrentTasMovie.BindMarkersToInput = Settings.BindMarkersToInput;
			RefreshDialog();
			_initialized = true;
		}

		private bool Engage()
		{
			_engaged = false;
			MainForm.PauseOnFrame = null;
			MainForm.PauseEmulator();

			SetupBoolPatterns();

			// Nag if inaccurate core, but not if auto-loading or movie is already loaded
			if (!CanAutoload && MovieSession.Movie.NotActive())
			{
				// Nag but allow the user to continue anyway, so ignore the return value
				MainForm.EnsureCoreIsAccurate();
			}

			// Start Scenario 1: A regular movie is active
			if (MovieSession.Movie.IsActive() && !(MovieSession.Movie is ITasMovie))
			{
				var changesString = "Would you like to save the current movie before closing it?";
				if (MovieSession.Movie.Changes)
				{
					changesString = "The current movie has unsaved changes. Would you like to save before closing it?";
				}
				var result = DialogController.ShowMessageBox3(
					"TAStudio will create a new project file from the current movie.\n\n" + changesString,
					"Convert movie",
					EMsgBoxIcon.Question);
				if (result == true)
				{
					MovieSession.Movie.Save();
				}
				else if (result == null)
				{
					return false;
				}

				ConvertCurrentMovieToTasproj();
				StartNewMovieWrapper(CurrentTasMovie);
				SetUpColumns();
			}

			// Start Scenario 2: A tasproj is already active
			else if (MovieSession.Movie.IsActive() && MovieSession.Movie is ITasMovie)
			{
				bool result = LoadFile(new FileInfo(CurrentTasMovie.Filename), gotoFrame: Emulator.Frame);
				if (!result)
				{
					TasView.AllColumns.Clear();
					StartNewTasMovie();
				}
			}

			// Start Scenario 3: No movie, but user wants to autoload their last project
			else if (CanAutoload)
			{
				bool result = LoadFile(new FileInfo(Settings.RecentTas.MostRecent));
				if (!result)
				{
					TasView.AllColumns.Clear();
					StartNewTasMovie();
				}
			}

			// Start Scenario 4: No movie, default behavior of engaging tastudio with a new default project
			else
			{
				StartNewTasMovie();
			}

			// Attempts to load failed, abort
			if (Emulator.IsNull())
			{
				Disengage();
				return false;
			}

			MainForm.AddOnScreenMessage("TAStudio engaged");
			SetTasMovieCallbacks(CurrentTasMovie);
			UpdateWindowTitle();
			MainForm.RelinquishControl(this);
			_originalEndAction = Config.Movies.MovieEndAction;
			MainForm.DisableRewind();
			Config.Movies.MovieEndAction = MovieEndAction.Record;
			MainForm.SetMainformMovieInfo();
			MovieSession.ReadOnly = true;
			SetSplicer();

			_engaged = true;
			return true;
		}

		private void AutosaveTimerEventProcessor(object sender, EventArgs e)
		{
			if (CurrentTasMovie == null)
			{
				return;
			}

			if (!CurrentTasMovie.Changes || Settings.AutosaveInterval == 0
				|| CurrentTasMovie.Filename == DefaultTasProjName())
			{
				return;
			}

			if (Settings.AutosaveAsBackupFile)
			{
				if (Settings.AutosaveAsBk2)
				{
					SaveBk2BackupMenuItem_Click(sender, e);
				}
				else
				{
					SaveBackupMenuItem_Click(sender, e);
				}
			}
			else
			{
				if (Settings.AutosaveAsBk2)
				{
					ToBk2MenuItem_Click(sender, e);
				}
				else
				{
					SaveTas();
				}
			}
		}

		private void SetTasMovieCallbacks(ITasMovie movie)
		{
			movie.ClientSettingsForSave = () => TasView.UserSettingsSerialized();
			movie.GetClientSettingsOnLoad = json => TasView.LoadSettingsSerialized(json);
		}

		private void SetUpColumns()
		{
			TasView.AllColumns.Clear();
			AddColumn(CursorColumnName, "", 18);
			TasView.AllColumns.Add(new RollColumn
			{
				Name = FrameColumnName,
				Text = "Frame#",
				UnscaledWidth = 68,
				Type = ColumnType.Text,
				Rotatable = true
			});

			var columnNames = MovieSession.Movie
				.LogGeneratorInstance(MovieSession.MovieController)
				.Map();

			foreach (var kvp in columnNames)
			{
				ColumnType type;
				int digits;
				if (ControllerType.Axes.TryGetValue(kvp.Key, out var range))
				{
					type = ColumnType.Axis;
					digits = Math.Max(kvp.Value.Length, range.MaxDigits);
				}
				else
				{
					type = ColumnType.Boolean;
					digits = kvp.Value.Length;
				}

				AddColumn(kvp.Key, kvp.Value, (digits * 6) + 14, type); // magic numbers reused in EditBranchTextPopUp()
			}

			var columnsToHide = TasView.AllColumns
				.Where(c =>
					// todo: make a proper user editable list?
					c.Name == "Power"
					|| c.Name == "Reset"
					|| c.Name == "Light Sensor"
					|| c.Name == "Open"
					|| c.Name == "Close"
					|| c.Name == "Disc Select"
					|| c.Name.StartsWith("Tilt")
					|| c.Name.StartsWith("Key ")
					|| c.Name.EndsWith("Tape")
					|| c.Name.EndsWith("Disk")
					|| c.Name.EndsWith("Block")
					|| c.Name.EndsWith("Status"));

			if (Emulator is N64)
			{
				var fakeAnalogControls = TasView.AllColumns
					.Where(c =>
						c.Name.EndsWith("A Up")
						|| c.Name.EndsWith("A Down")
						|| c.Name.EndsWith("A Left")
						|| c.Name.EndsWith("A Right"));

				columnsToHide = columnsToHide.Concat(fakeAnalogControls);
			}

			foreach (var column in columnsToHide)
			{
				column.Visible = false;
			}

			foreach (var column in TasView.VisibleColumns)
			{
				if (InputManager.StickyXorAdapter.IsSticky(column.Name))
				{
					column.Emphasis = true;
				}
			}

			TasView.AllColumns.ColumnsChanged();
			SetupBoolPatterns();
		}

		private void SetupBoolPatterns()
		{
			// Patterns
			int bStart = 0;
			int fStart = 0;
			if (BoolPatterns == null)
			{
				BoolPatterns = new AutoPatternBool[ControllerType.BoolButtons.Count + 2];
				AxisPatterns = new AutoPatternAxis[ControllerType.Axes.Count + 2];
			}
			else
			{
				bStart = BoolPatterns.Length - 2;
				fStart = AxisPatterns.Length - 2;
				Array.Resize(ref BoolPatterns, ControllerType.BoolButtons.Count + 2);
				Array.Resize(ref AxisPatterns, ControllerType.Axes.Count + 2);
			}

			for (int i = bStart; i < BoolPatterns.Length - 2; i++)
			{
				BoolPatterns[i] = new AutoPatternBool(1, 1);
			}

			BoolPatterns[BoolPatterns.Length - 2] = new AutoPatternBool(1, 0);
			BoolPatterns[BoolPatterns.Length - 1] = new AutoPatternBool(
				Config.AutofireOn, Config.AutofireOff);

			for (int i = fStart; i < AxisPatterns.Length - 2; i++)
			{
				AxisPatterns[i] = new AutoPatternAxis(new[] { 1 });
			}

			AxisPatterns[AxisPatterns.Length - 2] = new AutoPatternAxis(new[] { 1 });
			AxisPatterns[AxisPatterns.Length - 1] = new AutoPatternAxis(1, Config.AutofireOn, 0, Config.AutofireOff);

			SetUpToolStripColumns();
		}

		public void AddColumn(string columnName, string columnText, int columnWidth, ColumnType columnType = ColumnType.Boolean)
		{
			TasView.AllColumns.Add(new RollColumn
			{
				Name = columnName,
				Text = columnText,
				UnscaledWidth = columnWidth,
				Type = columnType
			});
		}

		public void LoadBranchByIndex(int index) => BookMarkControl.LoadBranchExternal(index);
		public void ClearFramesExternal() => ClearFramesMenuItem_Click(null, null);
		public void InsertFrameExternal() => InsertFrameMenuItem_Click(null, null);
		public void InsertNumFramesExternal() => InsertNumFramesMenuItem_Click(null, null);
		public void DeleteFramesExternal() => DeleteFramesMenuItem_Click(null, null);
		public void CloneFramesExternal() => CloneFramesMenuItem_Click(null, null);
		public void CloneFramesXTimesExternal() => CloneFramesXTimesMenuItem_Click(null, null);
		public void UndoExternal() => UndoMenuItem_Click(null, null);
		public void RedoExternal() => RedoMenuItem_Click(null, null);
		public void SelectBetweenMarkersExternal() => SelectBetweenMarkersMenuItem_Click(null, null);
		public void SelectAllExternal() => SelectAllMenuItem_Click(null, null);
		public void ReselectClipboardExternal() => ReselectClipboardMenuItem_Click(null, null);

		public IMovieController GetBranchInput(string branchId, int frame)
		{
			var branch = CurrentTasMovie.Branches.FirstOrDefault(b => b.Uuid.ToString() == branchId);
			if (branch == null || frame >= branch.InputLog.Count)
			{
				return null;
			}

			var controller = MovieSession.GenerateMovieController();
			controller.SetFromMnemonic(branch.InputLog[frame]);
			return controller;
		}

		private int? FirstNonEmptySelectedFrame
		{
			get
			{
				var lg = CurrentTasMovie.LogGeneratorInstance(MovieSession.MovieController);
				var empty = lg.EmptyEntry;
				foreach (var row in TasView.SelectedRows)
				{
					if (CurrentTasMovie[row].LogEntry != empty)
					{
						return row;
					}
				}

				return null;
			}
		}

		private void ConvertCurrentMovieToTasproj()
		{
			MovieSession.ConvertToTasProj();
			CurrentTasMovie.GreenzoneInvalidated = GreenzoneInvalidated;
			Settings.RecentTas.Add(MovieSession.Movie.Filename);
			MainForm.SetMainformMovieInfo();
			CurrentTasMovie.PropertyChanged += TasMovie_OnPropertyChanged;
		}

		private bool LoadFile(FileInfo file, bool startsFromSavestate = false, int gotoFrame = 0)
		{
			if (!file.Exists)
			{
				Settings.RecentTas.HandleLoadError(MainForm, file.FullName);
				return false;
			}

			_engaged = false;
			var newMovie = (ITasMovie)MovieSession.Get(file.FullName);
			newMovie.BindMarkersToInput = Settings.BindMarkersToInput;
			newMovie.GreenzoneInvalidated = GreenzoneInvalidated;

			if (!HandleMovieLoadStuff(newMovie))
			{
				return false;
			}

			_engaged = true;
			Settings.RecentTas.Add(newMovie.Filename); // only add if it did load

			if (startsFromSavestate)
			{
				GoToFrame(0);
			}
			else if (gotoFrame > 0)
			{
				GoToFrame(gotoFrame);
			}
			else
			{
				GoToFrame(CurrentTasMovie.TasSession.CurrentFrame);
			}

			// If we are loading an existing non-default movie, we will already have columns generated
			// Only set up columns if needed
			if (!TasView.AllColumns.Any())
			{
				SetUpColumns();
			}
			UpdateAutoFire();

			SetUpToolStripColumns();

			CurrentTasMovie.PropertyChanged += TasMovie_OnPropertyChanged;
			CurrentTasMovie.Branches.Current = CurrentTasMovie.TasSession.CurrentBranch;
			BookMarkControl.UpdateTextColumnWidth();
			MarkerControl.UpdateTextColumnWidth();
			// clear all selections
			TasView.DeselectAll();
			BookMarkControl.Restart();
			MarkerControl.Restart();

			RefreshDialog();
			return true;
		}

		private void StartNewTasMovie()
		{
			if (!AskSaveChanges())
			{
				return;
			}

			var filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
			var tasMovie = (ITasMovie)MovieSession.Get(filename);
			tasMovie.BindMarkersToInput = Settings.BindMarkersToInput;


			tasMovie.GreenzoneInvalidated = GreenzoneInvalidated;
			tasMovie.PropertyChanged += TasMovie_OnPropertyChanged;

			tasMovie.PopulateWithDefaultHeaderValues(
				Emulator,
				Game,
				MainForm.FirmwareManager,
				Config.DefaultAuthor);

			SetTasMovieCallbacks(tasMovie);
			tasMovie.ClearChanges(); // Don't ask to save changes here.
			tasMovie.Save();
			if (HandleMovieLoadStuff(tasMovie))
			{
			}

			// clear all selections
			TasView.DeselectAll();
			BookMarkControl.Restart();
			MarkerControl.Restart();
			SetUpColumns();
			RefreshDialog();
			TasView.Refresh();
		}

		private bool HandleMovieLoadStuff(ITasMovie movie)
		{
			WantsToControlStopMovie = false;
			WantsToControlReboot = false;
			var result = StartNewMovieWrapper(movie);

			if (!result)
			{
				return false;
			}

			WantsToControlStopMovie = true;
			WantsToControlReboot = true;

			CurrentTasMovie.ChangeLog.Clear();
			CurrentTasMovie.ClearChanges();

			UpdateWindowTitle();
			MessageStatusLabel.Text = $"{Path.GetFileName(CurrentTasMovie.Filename)} loaded.";

			return true;
		}

		private bool StartNewMovieWrapper(ITasMovie movie)
		{
			_initializing = true;

			SetTasMovieCallbacks(movie);

			SuspendLayout();
			bool result = MainForm.StartNewMovie(movie, false);
			ResumeLayout();
			if (result)
			{
				BookMarkControl.UpdateTextColumnWidth();
				MarkerControl.UpdateTextColumnWidth();
			}

			TastudioPlayMode();

			_initializing = false;

			return result;
		}

		private void DummyLoadProject(string path)
		{
			if (AskSaveChanges())
			{
				LoadFileWithFallback(path);
			}
		}

		private void LoadFileWithFallback(string path)
		{
			var result = LoadFile(new FileInfo(path));
			if (!result)
			{
				TasView.AllColumns.Clear();
				WantsToControlReboot = false;
				StartNewTasMovie();
				_engaged = true;
			}
		}

		private void DummyLoadMacro(string path)
		{
			if (!TasView.AnyRowsSelected)
			{
				return;
			}

			var loadZone = new MovieZone(path, MainForm, Emulator, MovieSession, Tools)
			{
				Start = TasView.FirstSelectedIndex.Value
			};
			loadZone.PlaceZone(CurrentTasMovie, Config);
		}

		private void TastudioPlayMode()
		{
			TasPlaybackBox.RecordingMode = false;
		}

		private void TastudioRecordMode()
		{
			TasPlaybackBox.RecordingMode = true;
		}

		private void TastudioStopMovie()
		{
			MovieSession.StopMovie(false);
			MainForm.SetMainformMovieInfo();
		}

		private void Disengage()
		{
			_engaged = false;
			MainForm.PauseOnFrame = null;
			MainForm.AddOnScreenMessage("TAStudio disengaged");
			MainForm.TakeBackControl();
			Config.Movies.MovieEndAction = _originalEndAction;
			MainForm.EnableRewind(true);
			MainForm.SetMainformMovieInfo();
		}

		private const string DefaultTasProjectName = "default";

		// Used when starting a new project
		private string DefaultTasProjName()
		{
			return Path.Combine(
				Config.PathEntries.MovieAbsolutePath(),
				$"{DefaultTasProjectName}.{MovieService.TasMovieExtension}");
		}

		// Used for things like SaveFile dialogs to suggest a name to the user
		private string SuggestedTasProjName()
		{
			return Path.Combine(
				Config.PathEntries.MovieAbsolutePath(),
				$"{Game.FilesystemSafeName()}.{MovieService.TasMovieExtension}");
		}

		private void SaveTas()
		{
			if (string.IsNullOrEmpty(CurrentTasMovie.Filename)
				|| CurrentTasMovie.Filename == DefaultTasProjName())
			{
				SaveAsTas();
			}
			else
			{
				_autosaveTimer?.Stop();
				MainForm.DoWithTempMute(() =>
				{
					MessageStatusLabel.Text = "Saving...";
					Cursor = Cursors.WaitCursor;
					Update();
					CurrentTasMovie.Save();
					if (Settings.AutosaveInterval > 0)
					{
						_autosaveTimer?.Start();
					}

					MessageStatusLabel.Text = "File saved.";
					Settings.RecentTas.Add(CurrentTasMovie.Filename);
					Cursor = Cursors.Default;
				});
			}
		}

		private void SaveAsTas()
		{
			_autosaveTimer.Stop();
			MainForm.DoWithTempMute(() =>
			{
				ClearLeftMouseStates();
				var filename = CurrentTasMovie.Filename;
				if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
				{
					filename = SuggestedTasProjName();
				}

				var file = SaveFileDialog(
					filename,
					Config.PathEntries.MovieAbsolutePath(),
					"Tas Project Files",
					"tasproj",
					this
				);

				if (file != null)
				{
					CurrentTasMovie.Filename = file.FullName;
					MessageStatusLabel.Text = "Saving...";
					Cursor = Cursors.WaitCursor;
					Update();
					CurrentTasMovie.Save();
					Settings.RecentTas.Add(CurrentTasMovie.Filename);
					UpdateWindowTitle();
					MessageStatusLabel.Text = "File saved.";
					Cursor = Cursors.Default;
				}

				// keep insisting
				if (Settings.AutosaveInterval > 0)
				{
					_autosaveTimer.Start();
				}

				MainForm.SetWindowText();
			});
		}

		protected override string WindowTitle
			=> CurrentTasMovie == null
				? "TAStudio"
				: CurrentTasMovie.Changes
					? $"TAStudio - {CurrentTasMovie.Name}*"
					: $"TAStudio - {CurrentTasMovie.Name}";

		protected override string WindowTitleStatic => "TAStudio";

		public IEnumerable<int> GetSelection() => TasView.SelectedRows;

		// Slow but guarantees the entire dialog refreshes
		private void FullRefresh()
		{
			SetTasViewRowCount();
			TasView.Refresh(); // An extra refresh potentially but we need to guarantee
			MarkerControl.UpdateValues();
			BookMarkControl.UpdateValues();

			if (_undoForm != null && !_undoForm.IsDisposed)
			{
				_undoForm.UpdateValues();
			}
		}

		public void RefreshDialog(bool refreshTasView = true, bool refreshBranches = true)
		{
			if (_exiting)
			{
				return;
			}

			if (refreshTasView)
			{
				SetTasViewRowCount();
			}

			MarkerControl?.UpdateValues();

			if (refreshBranches)
			{
				BookMarkControl?.UpdateValues();
			}

			if (_undoForm != null && !_undoForm.IsDisposed)
			{
				_undoForm.UpdateValues();
			}
		}

		private void SetTasViewRowCount()
		{
			TasView.RowCount = CurrentTasMovie.InputLogLength + 1;
			_lastRefresh = Emulator.Frame;
		}

		public void DoAutoRestore()
		{
			if (Settings.AutoRestoreLastPosition && LastPositionFrame != -1)
			{
				if (LastPositionFrame > Emulator.Frame) // Don't unpause if we are already on the desired frame, else runaway seek
				{
					StartSeeking(LastPositionFrame);
				}
			}
			else
			{
				if (_autoRestorePaused.HasValue && !_autoRestorePaused.Value)
				{
					// this happens when we're holding the left button while unpaused - view scrolls down, new input gets drawn, seek pauses
					MainForm.UnpauseEmulator();
				}

				_autoRestorePaused = null;
			}
		}

		/// <summary>
		/// Get a savestate prior to the previous frame so code following the call can frame advance and have a framebuffer.
		/// If frame is 0, return the initial state.
		/// </summary>
		private KeyValuePair<int,Stream> GetPriorStateForFramebuffer(int frame)
		{
			return CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame > 0 ? frame - 1 : 0);
		}

		private void StartAtNearestFrameAndEmulate(int frame, bool fromLua, bool fromRewinding)
		{
			if (frame == Emulator.Frame)
			{
				return;
			}

			_unpauseAfterSeeking = (fromRewinding || WasRecording) && !MainForm.EmulatorPaused;
			TastudioPlayMode();
			var closestState = GetPriorStateForFramebuffer(frame);
			if (closestState.Value.Length > 0 && (frame < Emulator.Frame || closestState.Key > Emulator.Frame))
			{
				LoadState(closestState);
			}

			if (fromLua)
			{
				bool wasPaused = MainForm.EmulatorPaused;
				
				// why not use this? because I'm not letting the form freely run. it all has to be under this loop.
				// i could use this and then poll StepRunLoop_Core() repeatedly, but.. that's basically what I'm doing
				// PauseOnFrame = frame;

				while (Emulator.Frame != frame)
				{
					MainForm.SeekFrameAdvance();
				}

				if (!wasPaused)
				{
					MainForm.UnpauseEmulator();
				}

				// lua botting users will want to re-activate record mode automatically -- it should be like nothing ever happened
				if (WasRecording)
				{
					TastudioRecordMode();
				}

				// now the next section won't happen since we're at the right spot
			}

			// frame == Emulator.Frame when frame == 0
			if (frame > Emulator.Frame)
			{
				// make seek frame keep up with emulation on fast scrolls
				if (MainForm.EmulatorPaused || MainForm.IsSeeking || fromRewinding || WasRecording)
				{
					StartSeeking(frame);
				}
				else
				{
					// GUI users may want to be protected from clobbering their video when skipping around...
					// well, users who are rewinding aren't. (that gets done through the seeking system in the call above)
					// users who are clicking around.. I don't know.
				}
			}
		}

		public void LoadState(KeyValuePair<int, Stream> state)
		{
			StatableEmulator.LoadStateBinary(new BinaryReader(state.Value));

			if (state.Key == 0 && CurrentTasMovie.StartsFromSavestate)
			{
				Emulator.ResetCounters();
			}

			UpdateOtherTools();
		}

		public void AddBranchExternal() => BookMarkControl.AddBranchExternal();
		public void RemoveBranchExternal() => BookMarkControl.RemoveBranchExternal();

		private void UpdateOtherTools() // a hack probably, surely there is a better way to do this
		{
			_hackyDontUpdate = true;
			Tools.UpdateToolsBefore();
			Tools.UpdateToolsAfter();
			_hackyDontUpdate = false;
		}

		public void TogglePause()
		{
			MainForm.TogglePause();
		}

		private void SetSplicer()
		{
			// TODO: columns selected?
			var temp = $"Selected: {TasView.SelectedRows.Count()} {(TasView.SelectedRows.Count() == 1 ? "frame" : "frames")}, States: {CurrentTasMovie.TasStateManager.Count}";
			if (_tasClipboard.Any()) temp += $", Clipboard: {_tasClipboard.Count} {(_tasClipboard.Count == 1 ? "frame" : "frames")}";
			SplicerStatusLabel.Text = temp;
		}

		private void DoTriggeredAutoRestoreIfNeeded()
		{
			// Disable the seek that could have been initiated when painting.
			// This must done before DoAutoRestore, otherwise it would disable the auto-restore seek.
			if (_playbackInterrupted)
			{
				MainForm.PauseOnFrame = null;
			}

			if (_triggerAutoRestore)
			{
				DoAutoRestore();

				_triggerAutoRestore = false;
				_autoRestorePaused = null;
			}

			if (_playbackInterrupted)
			{
				MainForm.UnpauseEmulator();
				_playbackInterrupted = false;
			}
		}

		public void InsertNumFrames(int insertionFrame, int numberOfFrames)
		{
			if (insertionFrame <= CurrentTasMovie.InputLogLength)
			{
				bool needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;

				CurrentTasMovie.InsertEmptyFrame(insertionFrame, numberOfFrames);

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(insertionFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		public void DeleteFrames(int beginningFrame, int numberOfFrames)
		{
			if (beginningFrame < CurrentTasMovie.InputLogLength)
			{
				int[] framesToRemove = Enumerable.Range(beginningFrame, numberOfFrames).ToArray();
				CurrentTasMovie.RemoveFrames(framesToRemove);
				SetSplicer();

				var needsToRollback = beginningFrame < Emulator.Frame;
				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(beginningFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		public void ClearFrames(int beginningFrame, int numberOfFrames)
		{
			if (beginningFrame < CurrentTasMovie.InputLogLength)
			{
				bool needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;
				int last = Math.Min(beginningFrame + numberOfFrames, CurrentTasMovie.InputLogLength);
				for (int i = beginningFrame; i < last; i++)
				{
					CurrentTasMovie.ClearFrame(i);
				}
				
				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(beginningFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void Tastudio_Closing(object sender, FormClosingEventArgs e)
		{
			if (!_initialized)
			{
				return;
			}

			_exiting = true;

			if (AskSaveChanges())
			{
				WantsToControlStopMovie = false;
				TastudioStopMovie();
				Disengage();
			}
			else
			{
				e.Cancel = true;
				_exiting = false;
			}

			_undoForm?.Close();
		}

		/// <summary>
		/// This method is called every time the Changes property is toggled on a <see cref="TasMovie"/> instance.
		/// </summary>
		private void TasMovie_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			UpdateWindowTitle();
		}

		private void TAStudio_DragDrop(object sender, DragEventArgs e)
		{
			// TODO: Maybe this should call Mainform's DragDrop method,
			// since that can file types that are not movies,
			// and it can process multiple files sequentially
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			LoadMovieFile(filePaths[0]);
		}

		private void TAStudio_MouseLeave(object sender, EventArgs e)
		{
			DoTriggeredAutoRestoreIfNeeded();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Tab
				|| keyData == (Keys.Shift | Keys.Tab)
				|| keyData == Keys.Space)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private bool AutoAdjustInput()
		{
			var lagLog = CurrentTasMovie[Emulator.Frame - 1]; // Minus one because get frame is +1;
			bool isLag = Emulator.AsInputPollable().IsLagFrame;

			if (lagLog.WasLagged.HasValue)
			{
				if (lagLog.WasLagged.Value && !isLag)
				{
					// Deleting this frame requires rewinding a frame.
					CurrentTasMovie.ChangeLog.AddInputBind(Emulator.Frame - 1, true, $"Bind Input; Delete {Emulator.Frame - 1}");
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.RemoveFrame(Emulator.Frame - 1);
					CurrentTasMovie.LagLog.RemoveHistoryAt(Emulator.Frame); // Removes from WasLag

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					GoToFrame(Emulator.Frame - 1);
					return true;
				}

				if (!lagLog.WasLagged.Value && isLag)
				{
					// (it shouldn't need to rewind, since the inserted input wasn't polled)
					CurrentTasMovie.ChangeLog.AddInputBind(Emulator.Frame - 1, false, $"Bind Input; Insert {Emulator.Frame - 1}");
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.InsertInput(Emulator.Frame - 1, CurrentTasMovie.GetInputLogEntry(Emulator.Frame - 2));
					CurrentTasMovie.LagLog.InsertHistoryAt(Emulator.Frame, true);

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					return true;
				}
			}

			return false;
		}

		private void MainVerticalSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.MainVerticalSplitDistance = MainVertialSplit.SplitterDistance;
		}

		private void BranchesMarkersSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.BranchMarkerSplitDistance = BranchesMarkersSplit.SplitterDistance;
		}

		private void TasView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell?.RowIndex != null && !CurrentTasMovie.Markers.IsMarker(e.NewCell.RowIndex.Value))
			{
				var currentMarker = CurrentTasMovie.Markers.Single(m => m.Frame == e.OldCell.RowIndex.Value);
				int newFrame = e.NewCell.RowIndex.Value;
				var newMarker = new TasMovieMarker(newFrame, currentMarker.Message);
				CurrentTasMovie.Markers.Remove(currentMarker);
				CurrentTasMovie.Markers.Add(newMarker);
				RefreshDialog();
			}
		}

		private void TASMenu_MenuActivate(object sender, EventArgs e)
		{
			IsInMenuLoop = true;
		}

		private void TASMenu_MenuDeactivate(object sender, EventArgs e)
		{
			IsInMenuLoop = false;
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}

		private void SetFontMenuItem_Click(object sender, EventArgs e)
		{
			using var fontDialog = new FontDialog
			{
				ShowColor = false,
				Font = TasView.Font
			};
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
			{
				TasView.Font = TasViewFont = fontDialog.Font;
				TasView.Refresh();
			}
		}

		private IMovieController ControllerFromMnemonicStr(string inputLogEntry)
		{
			try
			{
				var controller = MovieSession.GenerateMovieController();
				controller.SetFromMnemonic(inputLogEntry);

				return controller;
			}
			catch (Exception)
			{
				DialogController.ShowMessageBox($"Invalid mnemonic string: {inputLogEntry}", "Paste Input failed!");
				return null;
			}
		}
	}
}
