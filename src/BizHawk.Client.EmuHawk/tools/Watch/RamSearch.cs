using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.Common.RamSearchEngine;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// A form designed to search through ram values
	/// </summary>
	public partial class RamSearch : ToolFormBase, IToolFormAutoConfig
	{
		private const int MaxDetailedSize = 1024 * 1024; // 1mb, semi-arbitrary decision, sets the size to check for and automatically switch to fast mode for the user
		private const int MaxSupportedSize = 1024 * 1024 * 64; // 64mb, semi-arbitrary decision, sets the maximum size RAM Search will support (as it will crash beyond this)

		// TODO: DoSearch grabs the state of widgets and passes it to the engine before running, so rip out code that is attempting to keep the state up to date through change events
		private string _currentFileName = "";

		private RamSearchEngine _searches;
		private SearchEngineSettings _settings;

		private string _sortedColumn;
		private bool _sortReverse;
		private bool _forcePreviewClear;
		private bool _autoSearch;

		private bool _dropdownDontfire; // Used as a hack to get around lame .net dropdowns, there's no way to set their index without firing the SelectedIndexChanged event!

		protected override string WindowTitleStatic => "RAM Search";

		public RamSearch()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			InitializeComponent();
			Icon = Resources.SearchIcon;
			SearchMenuItem.Image = Resources.Search;
			DoSearchContextMenuItem.Image = Resources.Search;
			NewSearchContextMenuItem.Image = Resources.Restart;
			RemoveContextMenuItem.Image = Resources.Delete;
			AddToRamWatchContextMenuItem.Image = Resources.Find;
			PokeContextMenuItem.Image = Resources.Poke;
			FreezeContextMenuItem.Image = Resources.Freeze;
			UnfreezeAllContextMenuItem.Image = Resources.Unfreeze;
			OpenMenuItem.Image = Resources.OpenFile;
			SaveMenuItem.Image = Resources.SaveAs;
			TruncateFromFileMenuItem.Image = Resources.TruncateFromFile;
			RecentSubMenu.Image = Resources.Recent;
			newSearchToolStripMenuItem.Image = Resources.Restart;
			UndoMenuItem.Image = Resources.Undo;
			RedoMenuItem.Image = Resources.Redo;
			CopyValueToPrevMenuItem.Image = Resources.Previous;
			RemoveMenuItem.Image = Resources.Delete;
			AddToRamWatchMenuItem.Image = Resources.Find;
			PokeAddressMenuItem.Image = Resources.Poke;
			FreezeAddressMenuItem.Image = Resources.Freeze;
			AutoSearchCheckBox.Image = Resources.AutoSearch;
			DoSearchToolButton.Image = Resources.Search;
			NewSearchToolButton.Image = Resources.Restart;
			CopyValueToPrevToolBarItem.Image = Resources.Previous;
			ClearChangeCountsToolBarItem.Image = Resources.Placeholder;
			RemoveToolBarItem.Image = Resources.Delete;
			AddToRamWatchToolBarItem.Image = Resources.Find;
			PokeAddressToolBarItem.Image = Resources.Poke;
			FreezeAddressToolBarItem.Image = Resources.Freeze;
			UndoToolBarButton.Image = Resources.Undo;
			RedoToolBarItem.Image = Resources.Redo;
			RebootToolbarButton.Image = Resources.Reboot;
			ErrorIconButton.Image = Resources.ExclamationRed;
			SearchButton.Image = Resources.Search;

			WatchListView.QueryItemText += ListView_QueryItemText;
			WatchListView.QueryItemBkColor += ListView_QueryItemBkColor;
			Closing += (o, e) => { Settings.Columns = WatchListView.AllColumns; };

			_sortedColumn = "";
			_sortReverse = false;

			Settings = new RamSearchSettings();
			SetColumns();
		}

		[RequiredService]
		public IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		public IEmulator Emu { get; set; }

		[OptionalService]
		public IInputPollable InputPollableCore { get; set; }

		[ConfigPersist]
		public RamSearchSettings Settings { get; set; }

		private void HardSetDisplayTypeDropDown(Common.WatchDisplayType type)
		{
			foreach (var item in DisplayTypeDropdown.Items)
			{
				if (Watch.DisplayTypeToString(type) == item.ToString())
				{
					DisplayTypeDropdown.SelectedItem = item;
				}
			}
		}

		private void HardSetSizeDropDown(WatchSize size)
		{
			SizeDropdown.SelectedIndex = size switch
			{
				WatchSize.Byte => 0,
				WatchSize.Word => 1,
				WatchSize.DWord => 2,
				_ => SizeDropdown.SelectedIndex
			};
		}

		private void ColumnToggleCallback()
		{
			Settings.Columns = WatchListView.AllColumns;
		}

		private void RamSearch_Load(object sender, EventArgs e)
		{
			// Hack for previous config settings
			if (Settings.Columns.Any(c => string.IsNullOrWhiteSpace(c.Text)))
			{
				Settings = new RamSearchSettings();
			}

			RamSearchMenu.Items.Add(WatchListView.ToColumnsMenu(ColumnToggleCallback));

			_settings = new SearchEngineSettings(MemoryDomains, Settings.UseUndoHistory);
			_searches = new RamSearchEngine(_settings, MemoryDomains);

			ErrorIconButton.Visible = false;
			_dropdownDontfire = true;
			LoadConfigSettings();
			SpecificValueBox.ByteSize = _settings.Size;
			SpecificValueBox.Type = _settings.Type;
			DifferentByBox.Type = Common.WatchDisplayType.Unsigned;
			DifferenceBox.Type = Common.WatchDisplayType.Unsigned;

			MessageLabel.Text = "";
			SpecificAddressBox.MaxLength = (MemoryDomains.MainMemory.Size - 1).NumHexDigits();
			HardSetSizeDropDown(_settings.Size);
			PopulateTypeDropDown();
			HardSetDisplayTypeDropDown(_settings.Type);
			DoDomainSizeCheck();
			SetReboot(false);

			SpecificAddressBox.SetHexProperties(_settings.Domain.Size);
			SpecificValueBox.ResetText();
			SpecificAddressBox.ResetText();
			NumberOfChangesBox.ResetText();
			DifferenceBox.ResetText();
			DifferentByBox.ResetText();

			_dropdownDontfire = false;

			if (_settings.IsFastMode())
			{
				SetToFastMode();
			}

			NewSearch();
		}

		private void OutOfRangeCheck()
		{
			ErrorIconButton.Visible = _searches.OutOfRangeAddress.Any();
		}

		private void ListView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			if ((_searches.Count > 0) && (index < _searches.Count))
			{
				var nextColor = Color.White;

				var isCheat = MainForm.CheatList.IsActive(_settings.Domain, _searches[index].Address);
				var isWeeded = Settings.PreviewMode && !_forcePreviewClear && _searches.Preview(_searches[index].Address);

				if (!_searches[index].IsValid)
				{
					nextColor = Color.PeachPuff;
				}
				else if (isCheat)
				{
					nextColor = isWeeded ? Color.Lavender : Color.LightCyan;
				}
				else
				{
					if (isWeeded)
					{
						nextColor = Color.Pink;
					}
				}

				color = nextColor;
			}
		}

		private void ListView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			if (index >= _searches.Count)
			{
				return;
			}

			var columnName = column.Name;
			text = columnName switch
			{
				WatchList.Address => _searches[index].AddressString,
				WatchList.Value => _searches[index].ValueString,
				WatchList.Prev => _searches[index].PreviousStr,
				WatchList.ChangesCol => _searches[index].ChangeCount.ToString(),
				WatchList.Diff => _searches[index].Diff,
				_ => text
			};
		}

		private void LoadConfigSettings()
		{
			WatchListView.AllColumns.Clear();
			SetColumns();
		}

		private void SetColumns()
		{
			WatchListView.AllColumns.AddRange(Settings.Columns);
			WatchListView.Refresh();
		}

		/// <summary>
		/// This should be called anytime the search list changes
		/// </summary>
		private void UpdateList()
		{
			WatchListView.RowCount = _searches.Count;
			SetTotal();
		}

		public override void UpdateValues(ToolFormUpdateType type)
		{
			switch (type)
			{
				case ToolFormUpdateType.PostFrame:
				case ToolFormUpdateType.General:
					FrameUpdate();
					break;
				case ToolFormUpdateType.FastPostFrame:
					MinimalUpdate();
					break;
			}
		}

		private void FrameUpdate()
		{
			if (_searches.Count > 0)
			{
				_searches.Update();

				if (_autoSearch)
				{
					if (InputPollableCore != null && Settings.AutoSearchTakeLagFramesIntoAccount && InputPollableCore.IsLagFrame)
					{
						// Do nothing
					}
					else
					{
						DoSearch();
					}
				}

				_forcePreviewClear = false;
				WatchListView.RowCount = _searches.Count;
			}
		}

		private void MinimalUpdate()
		{
			if (_searches.Count > 0)
			{
				_searches.Update();

				if (_autoSearch)
				{
					DoSearch();
				}
			}
		}

		public override void Restart()
		{
			_settings = new SearchEngineSettings(MemoryDomains, Settings.UseUndoHistory);
			_searches = new RamSearchEngine(_settings, MemoryDomains);
			MessageLabel.Text = "Search restarted";
			DoDomainSizeCheck();
			_dropdownDontfire = true;
			SetSize(_settings.Size); // Calls NewSearch() automatically
			_dropdownDontfire = false;
			HardSetDisplayTypeDropDown(_settings.Type);
		}

		public void NewSearch()
		{
			var compareTo = _searches.CompareTo;
			var compareVal = _searches.CompareValue;
			var differentBy = _searches.DifferentBy;

			_searches = new RamSearchEngine(_settings, MemoryDomains, compareTo, compareVal, differentBy);
			_searches.Start();
			if (Settings.AlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}

			UpdateList();
			ToggleSearchDependentToolBarItems();
			SetReboot(false);
			MessageLabel.Text = "";
			SetDomainLabel();
		}

		public void NextCompareTo(bool reverse = false)
		{
			var radios = CompareToBox.Controls
				.OfType<RadioButton>()
				.Select(control => control)
				.OrderBy(x => x.TabIndex)
				.ToList();

			var selected = radios.FirstOrDefault(x => x.Checked);
			var index = radios.IndexOf(selected);

			if (reverse)
			{
				if (index == 0)
				{
					index = radios.Count - 1;
				}
				else
				{
					index--;
				}
			}
			else
			{
				index++;
				if (index >= radios.Count)
				{
					index = 0;
				}
			}

			radios[index].Checked = true;
			var mi = radios[index].GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
			mi?.Invoke(radios[index], new object[] { new EventArgs() });
		}

		public void NextOperator(bool reverse = false)
		{
			var radios = ComparisonBox.Controls
				.OfType<RadioButton>()
				.Select(control => control)
				.OrderBy(x => x.TabIndex)
				.ToList();

			var selected = radios.FirstOrDefault(x => x.Checked);
			var index = radios.IndexOf(selected);

			if (reverse)
			{
				if (index == 0)
				{
					index = radios.Count - 1;
				}
				else
				{
					index--;
				}
			}
			else
			{
				index++;
				if (index >= radios.Count)
				{
					index = 0;
				}
			}

			radios[index].Checked = true;
			var mi = radios[index].GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
			mi?.Invoke(radios[index], new object[] { new EventArgs() });
		}

		private void ToggleSearchDependentToolBarItems()
		{
			DoSearchToolButton.Enabled =
				CopyValueToPrevToolBarItem.Enabled =
				_searches.Count > 0;

			UpdateUndoToolBarButtons();
			OutOfRangeCheck();

			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any()
				&& _searches.Domain.Writable;
		}

		private long? CompareToValue
		{
			get
			{
				if (PreviousValueRadio.Checked)
				{
					return null;
				}

				if (SpecificValueRadio.Checked)
				{
					return (long)SpecificValueBox.ToRawInt() & 0x00000000FFFFFFFF;
				}

				if (SpecificAddressRadio.Checked)
				{
					return SpecificAddressBox.ToRawInt();
				}

				if (NumberOfChangesRadio.Checked)
				{
					return NumberOfChangesBox.ToRawInt();
				}

				if (DifferenceRadio.Checked)
				{
					return DifferenceBox.ToRawInt();
				}

				return null;
			}
		}

		private int? DifferentByValue => DifferentByRadio.Checked ? DifferentByBox.ToRawInt() : null;

		private ComparisonOperator Operator
		{
			get
			{
				if (NotEqualToRadio.Checked)
				{
					return ComparisonOperator.NotEqual;
				}

				if (LessThanRadio.Checked)
				{
					return ComparisonOperator.LessThan;
				}

				if (GreaterThanRadio.Checked)
				{
					return ComparisonOperator.GreaterThan;
				}

				if (LessThanOrEqualToRadio.Checked)
				{
					return ComparisonOperator.LessThanEqual;
				}

				if (GreaterThanOrEqualToRadio.Checked)
				{
					return ComparisonOperator.GreaterThanEqual;
				}

				if (DifferentByRadio.Checked)
				{
					return ComparisonOperator.DifferentBy;
				}

				return ComparisonOperator.Equal;
			}
		}

		private Compare Compare
		{
			get
			{
				if (SpecificValueRadio.Checked)
				{
					return Compare.SpecificValue;
				}

				if (SpecificAddressRadio.Checked)
				{
					return Compare.SpecificAddress;
				}

				if (NumberOfChangesRadio.Checked)
				{
					return Compare.Changes;
				}

				if (DifferenceRadio.Checked)
				{
					return Compare.Difference;
				}

				return Compare.Previous;
			}
		}

		public void DoSearch()
		{
			_searches.CompareValue = CompareToValue;
			_searches.DifferentBy = DifferentByValue;
			_searches.Operator = Operator;
			_searches.CompareTo = Compare;

			var removed = _searches.DoSearch();
			UpdateList();
			SetRemovedMessage(removed);
			ToggleSearchDependentToolBarItems();
			_forcePreviewClear = true;
		}

		private IEnumerable<int> SelectedIndices => WatchListView.SelectedRows;

		private IEnumerable<Watch> SelectedItems => SelectedIndices.Select(index => _searches[index]);

		private IEnumerable<Watch> SelectedWatches => SelectedItems.Where(x => !x.IsSeparator);

		private void SetRemovedMessage(int val)
		{
			MessageLabel.Text = $"{val} {(val == 1 ? "address" : "addresses")} removed";
		}

		private void SetTotal()
		{
			TotalSearchLabel.Text = $"{_searches.Count:n0} addresses";
		}

		private void SetDomainLabel()
		{
			MemDomainLabel.Text = _searches.Domain.Name;
		}

		private void LoadFileFromRecent(string path)
		{
			var file = new FileInfo(path);

			if (!file.Exists)
			{
				Settings.RecentSearches.HandleLoadError(MainForm, path);
			}
			else
			{
				LoadWatchFile(file, append: false);
			}
		}

		private void SetMemoryDomain(string name)
		{
			_settings.Domain = MemoryDomains[name];
			SetReboot(true);
			SpecificAddressBox.MaxLength = (_settings.Domain.Size - 1).NumHexDigits();
			DoDomainSizeCheck();
		}

		private void DoDomainSizeCheck()
		{
			if (_settings.Domain.Size >= MaxDetailedSize
				&& _settings.IsDetailed())
			{
				_settings.Mode = SearchMode.Fast;
				SetReboot(true);
				MessageLabel.Text = "Large domain, switching to fast mode";
			}
		}

		private void DoDisplayTypeClick(Common.WatchDisplayType type)
		{
			if (_settings.Type != type)
			{
				if (!string.IsNullOrEmpty(SpecificValueBox.Text))
				{
					SpecificValueBox.Text = "0";
				}

				if (!string.IsNullOrEmpty(DifferenceBox.Text))
				{
					DifferenceBox.Text = "0";
				}

				if (!string.IsNullOrEmpty(DifferentByBox.Text))
				{
					DifferentByBox.Text = "0";
				}
			}

			SpecificValueBox.Type = _settings.Type = type;
			DifferenceBox.Type = type;
			DifferentByBox.Type = type;
			_searches.SetType(type);

			_dropdownDontfire = true;
			DisplayTypeDropdown.SelectedItem = Watch.DisplayTypeToString(type);
			_dropdownDontfire = false;
			WatchListView.Refresh();
		}

		private void SetPreviousType(PreviousType type)
		{
			_settings.PreviousType = type;
			_searches.SetPreviousType(type);
		}

		private void HandleWatchSizeSelected(WatchSize newWatchSize)
		{
			if (_settings.Size != newWatchSize)
			{
				SetSize(newWatchSize);
			}
		}

		private void SetSize(WatchSize size)
		{
			_settings.Size = size;
			SpecificValueBox.ByteSize = size;
			if (!string.IsNullOrEmpty(SpecificAddressBox.Text))
			{
				SpecificAddressBox.Text = "0";
			}

			if (!string.IsNullOrEmpty(SpecificValueBox.Text))
			{
				SpecificValueBox.Text = "0";
			}

			bool isTypeCompatible = false;
			switch (size)
			{
				case WatchSize.Byte:
					isTypeCompatible = ByteWatch.ValidTypes.Any(t => t == _settings.Type);
					SizeDropdown.SelectedIndex = 0;
					break;

				case WatchSize.Word:
					isTypeCompatible = WordWatch.ValidTypes.Any(t => t == _settings.Type);
					SizeDropdown.SelectedIndex = 1;
					break;

				case WatchSize.DWord:
					isTypeCompatible = DWordWatch.ValidTypes.Any(t => t == _settings.Type);
					SizeDropdown.SelectedIndex = 2;
					break;
			}

			if (!isTypeCompatible)
			{
				_settings.Type = Common.WatchDisplayType.Unsigned;
			}

			_dropdownDontfire = true;

			PopulateTypeDropDown();
			_dropdownDontfire = false;
			SpecificValueBox.Type = _settings.Type;
			NewSearch();
		}

		private void PopulateTypeDropDown()
		{
			var previous = DisplayTypeDropdown.SelectedItem?.ToString() ?? "";
			var next = "";

			DisplayTypeDropdown.Items.Clear();

			var types = _settings.Size switch
			{
				WatchSize.Byte => ByteWatch.ValidTypes,
				WatchSize.Word => WordWatch.ValidTypes,
				WatchSize.DWord => DWordWatch.ValidTypes,
				_ => new List<Common.WatchDisplayType>()
			};

			foreach (var type in types)
			{
				var typeStr = Watch.DisplayTypeToString(type);
				DisplayTypeDropdown.Items.Add(typeStr);
				if (previous == typeStr)
				{
					next = typeStr;
				}
			}

			if (!string.IsNullOrEmpty(next))
			{
				DisplayTypeDropdown.SelectedItem = next;
			}
			else
			{
				DisplayTypeDropdown.SelectedIndex = 0;
			}
		}

		private void SetComparisonOperator(ComparisonOperator op)
		{
			_searches.Operator = op;
			WatchListView.Refresh();
		}

		private void SetCompareTo(Compare comp)
		{
			_searches.CompareTo = comp;
			WatchListView.Refresh();
		}

		private void SetCompareValue(int? value)
		{
			_searches.CompareValue = value;
			WatchListView.Refresh();
		}

		private void SetReboot(bool rebootNeeded)
		{
			RebootToolBarSeparator.Visible =
				RebootToolbarButton.Visible =
				rebootNeeded;
		}

		private void SetToDetailedMode()
		{
			_settings.Mode = SearchMode.Detailed;
			NumberOfChangesRadio.Enabled = true;
			NumberOfChangesBox.Enabled = true;
			DifferenceRadio.Enabled = true;
			DifferentByBox.Enabled = true;
			ClearChangeCountsToolBarItem.Enabled = true;

			WatchListView.AllColumns[WatchList.ChangesCol].Visible = true;
			ChangesMenuItem.Checked = true;

			ColumnToggleCallback();
			SetReboot(true);
		}

		private ToolStripMenuItem ChangesMenuItem
		{
			get
			{
				var subMenu = (ToolStripMenuItem)RamSearchMenu.Items
					.Cast<ToolStripItem>()
					.Single(t => t.Name == "GeneratedColumnsSubMenu"); // TODO - make name a constant
				return subMenu.DropDownItems
					.Cast<ToolStripMenuItem>()
					.Single(t => t.Name == WatchList.ChangesCol);
			}
		}

		private void SetToFastMode()
		{
			_settings.Mode = SearchMode.Fast;

			if (_settings.PreviousType == PreviousType.LastFrame || _settings.PreviousType == PreviousType.LastChange)
			{
				SetPreviousType(PreviousType.LastSearch);
			}

			NumberOfChangesRadio.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			NumberOfChangesBox.Text = "";
			ClearChangeCountsToolBarItem.Enabled = false;

			if (NumberOfChangesRadio.Checked || DifferenceRadio.Checked)
			{
				PreviousValueRadio.Checked = true;
			}

			WatchListView.AllColumns[WatchList.ChangesCol].Visible = false;
			ChangesMenuItem.Checked = false;

			ColumnToggleCallback();
			SetReboot(true);
		}

		private void RemoveAddresses()
		{
			var indices = SelectedIndices.ToList();
			if (indices.Any())
			{
				SetRemovedMessage(indices.Count);
				_searches.RemoveRange(indices);

				WatchListView.DeselectAll();
				UpdateList();
				ToggleSearchDependentToolBarItems();
			}
		}

		private void LoadWatchFile(FileInfo file, bool append, bool truncate = false)
		{
			if (file != null)
			{
				if (!truncate)
				{
					_currentFileName = file.FullName;
				}

				var watches = new WatchList(MemoryDomains, Emu.SystemId);
				watches.Load(file.FullName, append);
				Settings.RecentSearches.Add(watches.CurrentFileName);

				var watchList = watches.Where(x => !x.IsSeparator).ToList();
				var addresses = watchList.Select(x => x.Address).ToList();

				if (truncate)
				{
					SetRemovedMessage(addresses.Count);
					_searches.RemoveSmallWatchRange(watchList);
				}
				else
				{
					_searches.AddRange(addresses, append);
					MessageLabel.Text = $"{file.Name} loaded";
				}

				UpdateList();
				Settings.RecentSearches.Add(file.FullName);

				if (!append && !truncate)
				{
					_searches.ClearHistory();
				}

				ToggleSearchDependentToolBarItems();
			}
		}

		private void AddToRamWatch()
		{
			var watches = SelectedWatches.ToList();
			if (watches.Any())
			{
				Tools.LoadRamWatch(true);
				watches.ForEach(Tools.RamWatch.AddWatch);
				if (Settings.AlwaysExcludeRamWatch)
				{
					RemoveRamWatchesFromList();
				}
			}
		}

		private void PokeAddress()
		{
			if (SelectedIndices.Any())
			{
				var poke = new RamPoke(DialogController, SelectedIndices.Select(t => _searches[t]), MainForm.CheatList)
				{
					InitialLocation = this.ChildPointToScreen(WatchListView)
				};

				this.ShowDialogWithTempMute(poke);
				UpdateList();
			}
		}

		private void RemoveRamWatchesFromList()
		{
			if (Tools.Has<RamWatch>())
			{
				_searches.RemoveSmallWatchRange(Tools.RamWatch.Watches);
				UpdateList();
			}
		}

		private void UpdateUndoToolBarButtons()
		{
			UndoToolBarButton.Enabled = _searches.CanUndo;
			RedoToolBarItem.Enabled = _searches.CanRedo;
		}

		private void GoToSpecifiedAddress()
		{
			WatchListView.DeselectAll();
			var prompt = new InputPrompt
			{
				Text = "Go to Address",
				StartLocation = this.ChildPointToScreen(WatchListView),
				Message = "Enter a hexadecimal value"
			};

			var result = this.ShowDialogWithTempMute(prompt);
			while (result.IsOk())
			{
				try
				{
					var addr = int.Parse(prompt.PromptText, NumberStyles.HexNumber);
					for (int index = 0; index < _searches.Count; index++)
					{
						if (_searches[index].Address == addr)
						{
							WatchListView.SelectRow(index, true);
							WatchListView.ScrollToIndex(index);
							return; // Don't re-show dialog on success
						}
					}

					// TODO add error text to dialog?
					// Re-show dialog if the address isn't found
				}
				catch (FormatException)
				{
					// Re-show dialog if given invalid text (shouldn't happen)
				}
				catch (OverflowException)
				{
					// TODO add error text to dialog?
					// Re-show dialog if the address isn't valid
				}
			}
		}

		public class RamSearchSettings
		{
			public RamSearchSettings()
			{
				Columns = new List<RollColumn>
				{
					new RollColumn { Text = "Address", Name = WatchList.Address, Visible = true, UnscaledWidth = 60, Type = ColumnType.Text },
					new RollColumn { Text = "Value", Name = WatchList.Value, Visible = true, UnscaledWidth = 59, Type = ColumnType.Text },
					new RollColumn { Text = "Prev", Name = WatchList.Prev, Visible = true, UnscaledWidth = 59, Type = ColumnType.Text },
					new RollColumn { Text = "Changes", Name = WatchList.ChangesCol, Visible = true, UnscaledWidth = 60, Type = ColumnType.Text },
					new RollColumn { Text = "Diff", Name = WatchList.Diff, Visible = false, UnscaledWidth = 59, Type = ColumnType.Text }
				};

				PreviewMode = true;
				RecentSearches = new RecentFiles(8);
				AutoSearchTakeLagFramesIntoAccount = true;

			}

			public List<RollColumn> Columns { get; set; }
			public bool PreviewMode { get; set; }
			public bool AlwaysExcludeRamWatch { get; set; }
			public bool AutoSearchTakeLagFramesIntoAccount { get; set; }
			public bool UseUndoHistory { get; set; } = true;

			public RecentFiles RecentSearches { get; set; }
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = !string.IsNullOrWhiteSpace(_currentFileName);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Settings.RecentSearches.RecentMenu(MainForm, LoadFileFromRecent, "Search", noAutoload: true));
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			LoadWatchFile(
				GetWatchFileFromUser(""),
				sender == AppendFileMenuItem,
				sender == TruncateFromFileMenuItem);
		}

		private string CurrentFileName()
		{
			return !string.IsNullOrWhiteSpace(_currentFileName)
				? _currentFileName
				: Game.FilesystemSafeName();
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_currentFileName))
			{
				var watches = new WatchList(MemoryDomains, Emu.SystemId) { CurrentFileName = _currentFileName };
				for (var i = 0; i < _searches.Count; i++)
				{
					watches.Add(_searches[i]);
				}

				if (!string.IsNullOrWhiteSpace(watches.CurrentFileName))
				{
					if (watches.Save())
					{
						_currentFileName = watches.CurrentFileName;
						MessageLabel.Text = $"{Path.GetFileName(_currentFileName)} saved";
						Settings.RecentSearches.Add(watches.CurrentFileName);
					}
				}
				else
				{
					var result = watches.SaveAs(GetWatchSaveFileFromUser(CurrentFileName()));
					if (result)
					{
						MessageLabel.Text = $"{Path.GetFileName(_currentFileName)} saved";
						Settings.RecentSearches.Add(watches.CurrentFileName);
					}
				}
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			var watches = new WatchList(MemoryDomains, Emu.SystemId) { CurrentFileName = _currentFileName };
			for (var i = 0; i < _searches.Count; i++)
			{
				watches.Add(_searches[i]);
			}

			if (watches.SaveAs(GetWatchSaveFileFromUser(CurrentFileName())))
			{
				_currentFileName = watches.CurrentFileName;
				MessageLabel.Text = $"{Path.GetFileName(_currentFileName)} saved";
				Settings.RecentSearches.Add(watches.CurrentFileName);
			}
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			CheckMisalignedMenuItem.Checked = _settings.CheckMisAligned;
			BigEndianMenuItem.Checked = _settings.BigEndian;
		}

		private void ModeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DetailedMenuItem.Checked = _settings.IsDetailed();
			FastMenuItem.Checked = _settings.IsFastMode();
		}

		private void MemoryDomainsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsSubMenu.DropDownItems.Clear();
			MemoryDomainsSubMenu.DropDownItems.AddRange(
				MemoryDomains.MenuItems(SetMemoryDomain, _searches.Domain.Name, MaxSupportedSize)
				.ToArray());
		}

		private void SizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ByteMenuItem.Checked = _settings.Size == WatchSize.Byte;
			WordMenuItem.Checked = _settings.Size == WatchSize.Word;
			DWordMenuItem.Checked = _settings.Size == WatchSize.DWord;
		}

		private void DisplayTypeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayTypeSubMenu.DropDownItems.Clear();

			var types = _settings.Size switch
			{
				WatchSize.Byte => ByteWatch.ValidTypes,
				WatchSize.Word => WordWatch.ValidTypes,
				WatchSize.DWord => DWordWatch.ValidTypes,
				_ => new List<Common.WatchDisplayType>()
			};

			foreach (var type in types)
			{
				var item = new ToolStripMenuItem
					{
						Name = $"{type}ToolStripMenuItem",
						Text = Watch.DisplayTypeToString(type),
						Checked = _settings.Type == type
					};
				var type1 = type;
				item.Click += (o, ev) => DoDisplayTypeClick(type1);

				DisplayTypeSubMenu.DropDownItems.Add(item);
			}
		}

		private void DefinePreviousValueSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			Previous_LastSearchMenuItem.Checked = false;
			PreviousFrameMenuItem.Checked = false;
			Previous_OriginalMenuItem.Checked = false;
			Previous_LastChangeMenuItem.Checked = false;

			switch (_settings.PreviousType)
			{
				default:
				case PreviousType.LastSearch:
					Previous_LastSearchMenuItem.Checked = true;
					break;
				case PreviousType.LastFrame:
					PreviousFrameMenuItem.Checked = true;
					break;
				case PreviousType.Original:
					Previous_OriginalMenuItem.Checked = true;
					break;
				case PreviousType.LastChange:
					Previous_LastChangeMenuItem.Checked = true;
					break;
			}

			PreviousFrameMenuItem.Enabled = _settings.IsDetailed();
			Previous_LastChangeMenuItem.Enabled = _settings.IsDetailed();
		}

		private void DetailedMenuItem_Click(object sender, EventArgs e)
		{
			SetToDetailedMode();
		}

		private void FastMenuItem_Click(object sender, EventArgs e)
		{
			SetToFastMode();
		}

		private void ByteMenuItem_Click(object sender, EventArgs e)
		{
			HandleWatchSizeSelected(WatchSize.Byte);
		}

		private void WordMenuItem_Click(object sender, EventArgs e)
		{
			HandleWatchSizeSelected(WatchSize.Word);
		}

		private void DWordMenuItem_Click_Click(object sender, EventArgs e)
		{
			HandleWatchSizeSelected(WatchSize.DWord);
		}

		private void CheckMisalignedMenuItem_Click(object sender, EventArgs e)
		{
			_settings.CheckMisAligned ^= true;
			SetReboot(true);
		}

		private void Previous_LastFrameMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousType(PreviousType.LastFrame);
		}

		private void Previous_LastSearchMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousType(PreviousType.LastSearch);
		}

		private void Previous_OriginalMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousType(PreviousType.Original);
		}

		private void Previous_LastChangeMenuItem_Click(object sender, EventArgs e)
		{
			SetPreviousType(PreviousType.LastChange);
		}

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
		{
			_settings.BigEndian ^= true;
			_searches.SetEndian(_settings.BigEndian);
		}

		private void SearchSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ClearChangeCountsMenuItem.Enabled = _settings.IsDetailed();

			RemoveMenuItem.Enabled =
				AddToRamWatchMenuItem.Enabled =
				SelectedIndices.Any();

			PokeAddressMenuItem.Enabled =
				FreezeAddressMenuItem.Enabled =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.Writable);

			UndoMenuItem.Enabled =
				ClearUndoMenuItem.Enabled =
				_searches.CanUndo;

			RedoMenuItem.Enabled = _searches.CanRedo;
		}

		private void NewSearchMenuMenuItem_Click(object sender, EventArgs e)
		{
			NewSearch();
		}

		private void SearchMenuItem_Click(object sender, EventArgs e)
		{
			DoSearch();
		}

		private void UndoMenuItem_Click(object sender, EventArgs e)
		{
			if (_searches.CanUndo)
			{
				int restoredCount = _searches.Undo();
				UpdateList();
				ToggleSearchDependentToolBarItems();
				_forcePreviewClear = true;
				UpdateUndoToolBarButtons();
				MessageLabel.Text = $"{restoredCount} {(restoredCount == 1 ? "address" : "addresses")} restored";
			}
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			if (_searches.CanRedo)
			{
				int restoredCount = _searches.Redo();
				UpdateList();
				ToggleSearchDependentToolBarItems();
				_forcePreviewClear = true;
				UpdateUndoToolBarButtons();
				MessageLabel.Text = $"{restoredCount} {(restoredCount == 1 ? "address" : "addresses")} removed";
			}
		}

		private void CopyValueToPrevMenuItem_Click(object sender, EventArgs e)
		{
			_searches.SetPreviousToCurrent();
			WatchListView.Refresh();
		}

		private void ClearChangeCountsMenuItem_Click(object sender, EventArgs e)
		{
			_searches.ClearChangeCounts();
			WatchListView.Refresh();
		}

		private void RemoveMenuItem_Click(object sender, EventArgs e)
		{
			RemoveAddresses();
		}

		private void GoToAddressMenuItem_Click(object sender, EventArgs e)
		{
			GoToSpecifiedAddress();
		}

		private void AddToRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			AddToRamWatch();
		}

		private void PokeAddressMenuItem_Click(object sender, EventArgs e)
		{
			PokeAddress();
		}

		private void FreezeAddressMenuItem_Click(object sender, EventArgs e)
		{
			var allCheats = SelectedWatches.All(x => MainForm.CheatList.IsActive(x.Domain, x.Address));
			if (allCheats)
			{
				MainForm.CheatList.RemoveRange(SelectedWatches);
			}
			else
			{
				MainForm.CheatList.AddRange(
					SelectedWatches.Select(w => new Cheat(w, w.Value)));
			}
		}

		private void ClearUndoMenuItem_Click(object sender, EventArgs e)
		{
			_searches.ClearHistory();
			UpdateUndoToolBarButtons();
		}

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ExcludeRamWatchMenuItem.Checked = Settings.AlwaysExcludeRamWatch;
			UseUndoHistoryMenuItem.Checked = Settings.UseUndoHistory;
			PreviewModeMenuItem.Checked = Settings.PreviewMode;
			AutoSearchMenuItem.Checked = _autoSearch;
			AutoSearchAccountForLagMenuItem.Checked = Settings.AutoSearchTakeLagFramesIntoAccount;
		}

		private void PreviewModeMenuItem_Click(object sender, EventArgs e)
		{
			Settings.PreviewMode ^= true;
		}

		private void AutoSearchMenuItem_Click(object sender, EventArgs e)
		{
			_autoSearch ^= true;
			AutoSearchCheckBox.Checked = _autoSearch;
			DoSearchToolButton.Enabled =
				SearchButton.Enabled =
				!_autoSearch;
		}

		private void AutoSearchAccountForLagMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AutoSearchTakeLagFramesIntoAccount ^= true;
		}

		private void ExcludeRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AlwaysExcludeRamWatch ^= true;
			if (Settings.AlwaysExcludeRamWatch)
			{
				RemoveRamWatchesFromList();
			}
		}

		private void UseUndoHistoryMenuItem_Click(object sender, EventArgs e)
		{
			_searches.UndoEnabled ^= true;
			Settings.UseUndoHistory = _searches.UndoEnabled;
		}

		[RestoreDefaults]
		private void RestoreDefaultsMenuItem()
		{
			var recentFiles = Settings.RecentSearches; // We don't want to wipe recent files when restoring

			Settings = new RamSearchSettings { RecentSearches = recentFiles };

			RamSearchMenu.Items.Remove(
				RamSearchMenu.Items
					.OfType<ToolStripMenuItem>()
					.Single(x => x.Name == "GeneratedColumnsSubMenu"));

			RamSearchMenu.Items.Add(WatchListView.ToColumnsMenu(ColumnToggleCallback));

			_settings = new SearchEngineSettings(MemoryDomains, Settings.UseUndoHistory);
			if (_settings.IsFastMode())
			{
				SetToFastMode();
			}

			WatchListView.AllColumns.Clear();
			SetColumns();
		}

		private void ListViewContextMenu_Opening(object sender, CancelEventArgs e)
		{
			DoSearchContextMenuItem.Enabled = _searches.Count > 0;

			RemoveContextMenuItem.Visible =
				AddToRamWatchContextMenuItem.Visible =
				FreezeContextMenuItem.Visible =
				ContextMenuSeparator2.Visible =
				ViewInHexEditorContextMenuItem.Visible =
				SelectedIndices.Any();

			PokeContextMenuItem.Enabled =
				FreezeContextMenuItem.Visible =
				SelectedIndices.Any() &&
				SelectedWatches.All(w => w.Domain.Writable);

			UnfreezeAllContextMenuItem.Visible = MainForm.CheatList.ActiveCount > 0;

			ContextMenuSeparator3.Visible = SelectedIndices.Any() || (MainForm.CheatList.ActiveCount > 0);

			var allCheats = true;
			foreach (var index in SelectedIndices)
			{
				if (!MainForm.CheatList.IsActive(_settings.Domain, _searches[index].Address))
				{
					allCheats = false;
				}
			}

			if (allCheats)
			{
				FreezeContextMenuItem.Text = "&Unfreeze Address";
				FreezeContextMenuItem.Image = Resources.Unfreeze;
			}
			else
			{
				FreezeContextMenuItem.Text = "&Freeze Address";
				FreezeContextMenuItem.Image = Resources.Freeze;
			}
		}

		private void UnfreezeAllContextMenuItem_Click(object sender, EventArgs e)
		{
			MainForm.CheatList.RemoveAll();
		}

		private void ViewInHexEditorContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedWatches.Any())
			{
				ViewInHexEditor(_searches.Domain, SelectedWatches.Select(x => x.Address), SelectedSize);
			}
		}

		private void ClearPreviewContextMenuItem_Click(object sender, EventArgs e)
		{
			_forcePreviewClear = true;
			WatchListView.Refresh();
		}

		private WatchSize SelectedSize =>
			SizeDropdown.SelectedIndex switch
			{
				1 => WatchSize.Word,
				2 => WatchSize.DWord,
				_ => WatchSize.Byte
			};

		private void SizeDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_dropdownDontfire)
			{
				return;
			}

			HandleWatchSizeSelected(SelectedSize);
		}

		private void DisplayTypeDropdown_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_dropdownDontfire)
			{
				DoDisplayTypeClick(Watch.StringToDisplayType(DisplayTypeDropdown.SelectedItem.ToString()));
			}
		}

		private void ErrorIconButton_Click(object sender, EventArgs e)
		{
			var outOfRangeAddresses = _searches.OutOfRangeAddress.ToList();
			_searches.RemoveAddressRange(outOfRangeAddresses);
			SetRemovedMessage(outOfRangeAddresses.Count);

			UpdateList();
			ToggleSearchDependentToolBarItems();
		}

		private void CopyWatchesToClipBoard()
		{
			if (SelectedItems.Any())
			{
				var sb = new StringBuilder();
				foreach (var watch in SelectedItems)
				{
					sb.AppendLine(watch.ToString());
				}

				if (sb.Length > 0)
				{
					Clipboard.SetDataObject(sb.ToString());
				}
			}
		}

		private void PreviousValueRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(Compare.Previous);
		}

		private void SpecificValueRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(SpecificValueBox.Text))
			{
				SpecificAddressBox.ResetText();
			}

			_searches.CompareValue = SpecificValueBox.ToRawInt();

			if (Focused)
			{
				SpecificValueBox.Focus();
			}

			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(Compare.SpecificValue);
		}

		private void SpecificAddressRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(SpecificAddressBox.Text))
			{
				SpecificAddressBox.ResetText();
			}

			_searches.CompareValue = SpecificAddressBox.ToRawInt();

			if (Focused)
			{
				SpecificAddressBox.Focus();
			}

			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = false;
			SetCompareTo(Compare.SpecificAddress);
		}

		private void NumberOfChangesRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(NumberOfChangesBox.Text))
			{
				NumberOfChangesBox.ResetText();
			}

			_searches.CompareValue = NumberOfChangesBox.ToRawInt();

			if (Focused)
			{
				NumberOfChangesBox.Focus();
			}

			DifferenceBox.Enabled = false;
			SetCompareTo(Compare.Changes);
		}

		private void DifferenceRadio_Click(object sender, EventArgs e)
		{
			SpecificValueBox.Enabled = false;
			SpecificAddressBox.Enabled = false;
			NumberOfChangesBox.Enabled = false;
			DifferenceBox.Enabled = true;
			if (string.IsNullOrWhiteSpace(DifferenceBox.Text))
			{
				DifferenceBox.ResetText();
			}

			_searches.CompareValue = DifferenceBox.ToRawInt();

			if (Focused)
			{
				DifferenceBox.Focus();
			}

			SetCompareTo(Compare.Difference);
		}

		private void CompareToValue_TextChanged(object sender, EventArgs e)
		{
			SetCompareValue(((INumberBox)sender).ToRawInt());
		}

		private void EqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(ComparisonOperator.Equal);
		}

		private void NotEqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(ComparisonOperator.NotEqual);
		}

		private void LessThanRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(ComparisonOperator.LessThan);
		}

		private void GreaterThanRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(ComparisonOperator.GreaterThan);
		}

		private void LessThanOrEqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(ComparisonOperator.LessThanEqual);
		}

		private void GreaterThanOrEqualToRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = false;
			SetComparisonOperator(ComparisonOperator.GreaterThanEqual);
		}

		private void DifferentByRadio_Click(object sender, EventArgs e)
		{
			DifferentByBox.Enabled = true;

			if (string.IsNullOrWhiteSpace(DifferentByBox.Text))
			{
				DifferentByBox.ResetText();
			}

			_searches.DifferentBy = DifferenceBox.ToRawInt();

			if (Focused)
			{
				DifferentByBox.Focus();
			}

			SetComparisonOperator(ComparisonOperator.DifferentBy);
		}

		private void DifferentByBox_TextChanged(object sender, EventArgs e)
		{
			_searches.DifferentBy = !string.IsNullOrWhiteSpace(DifferentByBox.Text) ? DifferentByBox.ToRawInt() : null;
			WatchListView.Refresh();
		}

		private void WatchListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.Delete))
			{
				RemoveAddresses();
			}
			else if (e.IsCtrl(Keys.C))
			{
				CopyWatchesToClipBoard();
			}
		}

		private void WatchListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoveToolBarItem.Enabled =
				AddToRamWatchToolBarItem.Enabled =
				SelectedIndices.Any();

			PokeAddressToolBarItem.Enabled =
				FreezeAddressToolBarItem.Enabled =
				SelectedIndices.Any()
				&& _searches.Domain.Writable;
		}

		private void WatchListView_Enter(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void WatchListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var column = e.Column;
			if (column.Name != _sortedColumn)
			{
				_sortReverse = false;
			}

			_searches.Sort(column.Name, _sortReverse);

			_sortedColumn = column.Name;
			_sortReverse ^= true;
			WatchListView.Refresh();
		}

		private void WatchListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (SelectedIndices.Any())
			{
				AddToRamWatch();
			}
		}

		private void NewRamSearch_Activated(object sender, EventArgs e)
		{
			WatchListView.Refresh();
		}

		private void NewRamSearch_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == ".wch")
			{
				var file = new FileInfo(filePaths[0]);
				if (file.Exists)
				{
					LoadWatchFile(file, false);
				}
			}
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}
	}
}
