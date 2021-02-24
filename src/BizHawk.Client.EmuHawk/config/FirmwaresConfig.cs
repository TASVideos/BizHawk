using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Emulation.Common;

// notes: eventually, we intend to have a "firmware acquisition interface" exposed to the emulator cores.
// it will be implemented by EmuHawk, and use firmware keys to fetch the firmware content.
// however, for now, the cores are using strings from the config class. so we have the `configMember` which is 
// used by reflection to set the configuration for firmwares which were found

// TODO - we may eventually need to add a progress dialog for this. we should have one for other reasons.
// I started making one in BizHawk.Util as QuickProgressPopup but ran out of time

// IDEA: show current path in tooltip (esp. for custom resolved)
// IDEA: prepop set customization to dir of current custom

// TODO - display some kind if [!] if you have a user-specified file which is known but defined as incompatible by the firmware DB
namespace BizHawk.Client.EmuHawk
{
	public partial class FirmwaresConfig : Form
	{
		private readonly IDictionary<string, string> _firmwareUserSpecifications;

		private readonly IGameInfo _game;

		private readonly IMainFormForConfig _mainForm;

		private readonly PathEntryCollection _pathEntries;

		private readonly FirmwareManager Manager;

		// friendlier names than the system Ids
		// Redundant with SystemLookup? Not so fast. That data drives things. This is one step abstracted. Don't be such a smart guy. Keep this redundant list up to date.
		private static readonly Dictionary<string, string> SystemGroupNames = new Dictionary<string, string>
		{
			["NES"] = "NES",
			["SNES"] = "SNES",
			["PCECD"] = "PCE-CD",
			["SAT"] = "Saturn",
			["A78"] = "Atari 7800",
			["Coleco"] = "Colecovision",
			["GBA"] = "GBA",
			["NDS"] = "Nintendo DS",
			["TI83"] = "TI-83",
			["INTV"] = "Intellivision",
			["C64"] = "C64",
			["GEN"] = "Genesis",
			["SMS"] = "Sega Master System",
			["PSX"] = "PlayStation",
			["Lynx"] = "Lynx",
			["AppleII"] = "Apple II",
			["O2"] = "Odyssey 2",
			["GB"] = "Game Boy",
			["GBC"] = "Game Boy Color",
			["PCFX"] = "PC-FX",
			["32X"] = "32X",
			["ZXSpectrum"] = "ZX Spectrum",
			["AmstradCPC"] = "Amstrad CPC",
			["ChannelF"] = "Channel F",
			["Vectrex"] = "Vectrex",
			["MSX"] = "MSX",
			["G7400"] = "Philips Videopac+ G7400",
			["PS2"] = "Sony PlayStation 2",
		};

		public string TargetSystem { get; set; }

		private CheckBox _cbAllowImport;

		private const int IdUnsure = 0;
		private const int IdMissing = 1;
		private const int IdOk = 2;

		private Font _fixedFont, _boldFont, _boldFixedFont;

		private class ListViewSorter : IComparer
		{
			public int Column { get; set; }
			public int Sign { get; set; }

			public ListViewSorter(int column)
			{
				Column = column;
			}

			public int Compare(object a, object b)
			{
				var lva = (ListViewItem)a;
				var lvb = (ListViewItem)b;
				return Sign * string.Compare(lva.SubItems[Column].Text, lvb.SubItems[Column].Text);
			}
		}

		private string _currSelectorDir;
		private readonly ListViewSorter _listViewSorter;

		public FirmwaresConfig(
			FirmwareManager firmwareManager,
			IDictionary<string, string> firmwareUserSpecifications,
			IGameInfo game,
			IMainFormForConfig mainForm,
			PathEntryCollection pathEntries,
			bool retryLoadRom = false,
			string reloadRomPath = null)
		{
			_firmwareUserSpecifications = firmwareUserSpecifications;
			_game = game;
			_mainForm = mainForm;
			_pathEntries = pathEntries;
			Manager = firmwareManager;

			InitializeComponent();

			tbbGroup.Image
				= tbbScan.Image 
				= tbbOrganize.Image
				= tbbImport.Image
				= tbbClose.Image
				= tbbCloseReload.Image
				= tbbOpenFolder.Image = Resources.Placeholder;

			// prep ImageList for ListView with 3 item states for {idUnsure, idMissing, idOk}
			imageList1.Images.AddRange(new Image[] { Resources.RetroQuestion, Resources.ExclamationRed, Resources.GreenCheck });

			_listViewSorter = new ListViewSorter(-1);

			if (retryLoadRom)
			{
				toolStripSeparator1.Visible = true;
				tbbCloseReload.Visible = true;
				tbbCloseReload.Enabled = true;


				tbbCloseReload.ToolTipText = string.IsNullOrWhiteSpace(reloadRomPath)
					? "Close Firmware Manager and reload ROM"
					: $"Close Firmware Manager and reload {reloadRomPath}";
			}
		}

		// makes sure that the specified SystemId is selected in the list (and that all the firmwares for it are visible)
		private void WarpToSystemId(string sysId)
		{
			bool selectedFirst = false;
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				if (lvi.SubItems[1].Text == sysId)
				{
					if(!selectedFirst) lvi.Selected = true;
					lvi.EnsureVisible();
					selectedFirst = true;
				}
			}
		}

		private void FirmwaresConfig_Load(object sender, EventArgs e)
		{
			// we'll use this font for displaying the hash, so they don't look all jagged in a long list
			_fixedFont = new Font(new FontFamily("Courier New"), 8);
			_boldFont = new Font(lvFirmwares.Font, FontStyle.Bold);
			_boldFixedFont = new Font(_fixedFont, FontStyle.Bold);

			// populate ListView from firmware DB
			var groups = new Dictionary<string, ListViewGroup>();
			foreach (var fr in FirmwareDatabase.Instance.FirmwareRecords)
			{
				var sysID = fr.ID.System;
				var lvi = new ListViewItem
				{
					Tag = fr,
					UseItemStyleForSubItems = false,
					ImageIndex = IdUnsure,
					ToolTipText = null
				};
				lvi.SubItems.Add(sysID);
				lvi.SubItems.Add(fr.ID.Firmware);
				lvi.SubItems.Add(fr.Description);
				lvi.SubItems.Add(""); // resolved with
				lvi.SubItems.Add(""); // location
				lvi.SubItems.Add(""); // size
				lvi.SubItems.Add(""); // hash
				lvi.SubItems[6].Font = _fixedFont; // would be used for hash and size
				lvi.SubItems[7].Font = _fixedFont; // would be used for hash and size
				lvFirmwares.Items.Add(lvi);

				// build the groups in the ListView as we go:
				if (!groups.ContainsKey(sysID))
				{
					if (!SystemGroupNames.TryGetValue(sysID, out var name))
						name = "FIX ME (FirmwaresConfig.cs)";
					lvFirmwares.Groups.Add(sysID, name);
					var lvg = lvFirmwares.Groups[lvFirmwares.Groups.Count - 1];
					groups[sysID] = lvg;
				}
				lvi.Group = groups[sysID];
			}

			// now that we have some items in the ListView, we can size some columns to sensible widths
			lvFirmwares.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			lvFirmwares.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			if (TargetSystem != null)
			{
				WarpToSystemId(TargetSystem);
			}

			RefreshBasePath();

			_cbAllowImport = new CheckBox
			{
				Text = "Allow Importing of Unknown Files",
				BackColor = SystemColors.Control,
				CheckAlign = ContentAlignment.MiddleLeft,
				TextAlign = ContentAlignment.MiddleLeft,
				Font = new Font("Segeo UI", 9, FontStyle.Regular, GraphicsUnit.Point, 1, false),
				Checked = false,
				Size = new Size(230, 22),
			};
			ToolStripControlHost host = new ToolStripControlHost(_cbAllowImport);
			toolStrip1.Items.Add(host);
		}

		private void TbbClose_Click(object sender, EventArgs e)
		{
			this.Close();
			DialogResult = DialogResult.Cancel;
		}

		private void TbbCloseReload_Click(object sender, EventArgs e)
		{
			this.Close();
			DialogResult = DialogResult.Retry;
		}

		private void FirmwaresConfig_FormClosed(object sender, FormClosedEventArgs e)
		{
			_fixedFont.Dispose();
			_boldFont.Dispose();
			_boldFixedFont.Dispose();
		}

		private void TbbGroup_Click(object sender, EventArgs e)
		{
			// toggle the grouping state
			lvFirmwares.ShowGroups = !lvFirmwares.ShowGroups;
		}

		private void LvFirmwares_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (_listViewSorter.Column != e.Column)
			{
				_listViewSorter.Column = e.Column;
				_listViewSorter.Sign = 1;
			}
			else _listViewSorter.Sign *= -1;
			lvFirmwares.ListViewItemSorter = _listViewSorter;
			lvFirmwares.SetSortIcon(e.Column, _listViewSorter.Sign == 1 ? SortOrder.Descending : SortOrder.Ascending);
			lvFirmwares.Sort();
		}

		private void TbbScan_Click(object sender, EventArgs e)
		{
			// user-initiated scan
			DoScan();
		}

		private void DoScan()
		{
			lvFirmwares.BeginUpdate();
			Manager.DoScanAndResolve(
				_pathEntries,
				_firmwareUserSpecifications);

			// for each type of firmware, try resolving and record the result
			foreach (ListViewItem lvi in lvFirmwares.Items)
			{
				var fr = (FirmwareRecord) lvi.Tag;
				var ri = Manager.Resolve(
					_pathEntries,
					_firmwareUserSpecifications,
					fr,
					true);

				for (int i = 4; i <= 7; i++)
				{
					lvi.SubItems[i].Text = "";
				}

				if (ri == null)
				{
					lvi.ImageIndex = IdMissing;
					lvi.ToolTipText = "No file bound for this firmware!";
				}
				else
				{
					// lazy substring extraction. really should do a better job
					var basePath = _pathEntries.FirmwareAbsolutePath() + Path.DirectorySeparatorChar;
					
					var path = ri.FilePath.Replace(basePath, "");

					// bolden the item if the user has specified a path for it
					bool bolden = ri.UserSpecified;

					// set columns based on whether it was a known file
					if (ri.KnownFirmwareFile == null)
					{
						lvi.ImageIndex = IdUnsure;
						lvi.ToolTipText = "You've bound a custom choice here. Hope you know what you're doing.";
						lvi.SubItems[4].Text = "-custom-";
					}
					else
					{
						lvi.ImageIndex = IdOk;
						lvi.ToolTipText = "Good! This file has been bound to some kind of a decent choice";
						lvi.SubItems[4].Text = ri.KnownFirmwareFile.Value.Description;
					}

					// bolden the item if necessary
					if (bolden)
					{
						foreach (ListViewItem.ListViewSubItem subItem in lvi.SubItems) subItem.Font = _boldFont;
						lvi.SubItems[6].Font = _boldFixedFont;
					}
					else
					{
						foreach (ListViewItem.ListViewSubItem subItem in lvi.SubItems) subItem.Font = lvFirmwares.Font;
						lvi.SubItems[6].Font = _fixedFont;
					}

					// if the user specified a file but its missing, mark it as such
					if (ri.Missing)
					{
						lvi.ImageIndex = IdMissing;
						lvi.ToolTipText = "The file that's specified is missing!";
					}

					// if the user specified a known firmware file but its for some other firmware, it was probably a mistake. mark it as suspicious
					if (ri.KnownMismatching)
					{
						lvi.ImageIndex = IdUnsure;
						lvi.ToolTipText = "You've manually specified a firmware file, and we're sure it's wrong. Hope you know what you're doing.";
					}


					lvi.SubItems[5].Text = path;

					lvi.SubItems[6].Text = ri.Size.ToString();

					lvi.SubItems[7].Text = ri.Hash != null
						? $"sha1:{ri.Hash}"
						: "";
				}
			}

			lvFirmwares.EndUpdate();
		}

		private void TbbOrganize_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(this, "This is going to move/rename every automatically-selected firmware file under your configured firmwares directory to match our recommended organizational scheme (which is not super great right now). Proceed?", "Firmwares Organization Confirm", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
			{
				return;
			}

			Manager.DoScanAndResolve(_pathEntries, _firmwareUserSpecifications);

			foreach (var fr in FirmwareDatabase.Instance.FirmwareRecords)
			{
				var ri = Manager.Resolve(_pathEntries, _firmwareUserSpecifications, fr);
				if (ri?.KnownFirmwareFile == null) continue;
				if (ri.UserSpecified) continue;

				var fpTarget = Path.Combine(_pathEntries.FirmwareAbsolutePath(), ri.KnownFirmwareFile.Value.RecommendedName);
				string fpSource = ri.FilePath;

				try
				{
				  File.Move(fpSource, fpTarget);
				}
				catch
				{
				  // sometimes moves fail. especially in newer versions of windows with explorers more fragile than your great-grandma.
				  // I am embarrassed that I know that. about windows, not your great-grandma.
				}
			}

			DoScan();
		}

		private void TbbOpenFolder_Click(object sender, EventArgs e)
		{
			var frmWares = _pathEntries.FirmwareAbsolutePath();
			if (!Directory.Exists(frmWares))
			{
				Directory.CreateDirectory(frmWares);
			}

			System.Diagnostics.Process.Start(frmWares);
		}

		private void LvFirmwares_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsCtrl(Keys.C))
			{
				PerformListCopy();
			}
		}

		private void PerformListCopy()
		{
			var str = lvFirmwares.CopyItemsAsText();
			if (str.Length > 0) Clipboard.SetDataObject(str);
		}

		private void LvFirmwares_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && lvFirmwares.GetItemAt(e.X, e.Y) != null)
				lvFirmwaresContextMenuStrip.Show(lvFirmwares, e.Location);
		}

		private void TsmiSetCustomization_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = _currSelectorDir,
				RestoreDirectory = true
			};
			string firmwarePath = _pathEntries.FirmwareAbsolutePath();

			if (ofd.ShowDialog() == DialogResult.OK)
			{
				// remember the location we selected this firmware from, maybe there are others
				_currSelectorDir = Path.GetDirectoryName(ofd.FileName);

				try
				{
					using var hf = new HawkFile(ofd.FileName);
					// for each selected item, set the user choice (even though multiple selection for this operation is no longer allowed)
					foreach (ListViewItem lvi in lvFirmwares.SelectedItems)
					{
						var fr = (FirmwareRecord) lvi.Tag;
						string filePath = ofd.FileName;

						// if the selected file is an archive, allow the user to pick the inside file
						// to always be copied to the global firmwares directory
						if (hf.IsArchive)
						{
							var ac = new ArchiveChooser(new HawkFile(filePath));
							int memIdx;

							if (ac.ShowDialog(this) == DialogResult.OK)
							{
								memIdx = ac.SelectedMemberIndex;
							}
							else
							{
								return;
							}

							var insideFile = hf.BindArchiveMember(memIdx);
							var fileData = insideFile.ReadAllBytes();

							// write to file in the firmwares folder
							File.WriteAllBytes(Path.Combine(firmwarePath, insideFile.Name), fileData);
							filePath = Path.Combine(firmwarePath, insideFile.Name);
						}
						else
						{
							// selected file is not an archive
							// check whether this file is currently outside of the global firmware directory
							if (_currSelectorDir != firmwarePath)
							{
								var askMoveResult = MessageBox.Show(this, "The selected custom firmware does not reside in the root of the global firmware directory.\nDo you want to copy it there?", "Import Custom Firmware", MessageBoxButtons.YesNo);
								if (askMoveResult == DialogResult.Yes)
								{
									try
									{
										var fi = new FileInfo(filePath);
										filePath = Path.Combine(firmwarePath, fi.Name);
										File.Copy(ofd.FileName, filePath);
									}
									catch (Exception ex)
									{
										MessageBox.Show(this, $"There was an issue copying the file. The customization has NOT been set.\n\n{ex.StackTrace}");
										continue;
									}
								}
							}
						}

						_firmwareUserSpecifications[fr.ID.ConfigKey] = filePath;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, $"There was an issue during the process. The customization has NOT been set.\n\n{ex.StackTrace}");
					return;
				}

				DoScan();
			}
		}

		private void TsmiClearCustomization_Click(object sender, EventArgs e)
		{
			// for each selected item, clear the user choice
			foreach (ListViewItem lvi in lvFirmwares.SelectedItems)
			{
				var fr = (FirmwareRecord) lvi.Tag;
				_firmwareUserSpecifications.Remove(fr.ID.ConfigKey);
			}

			DoScan();
		}

		private void TsmiInfo_Click(object sender, EventArgs e)
		{
			var lvi = lvFirmwares.SelectedItems[0];
			var fr = (FirmwareRecord) lvi.Tag;

			// get all options for this firmware (in order)
			var options = FirmwareDatabase.Instance.FirmwareOptions.Where(fo => fo.ID == fr.ID);

			var fciDialog = new FirmwaresConfigInfo
			{
				lblFirmware =
				{
					Text = $"{fr.ID} ({fr.Description})"
				}
			};

			foreach (var o in options)
			{
				ListViewItem olvi = new ListViewItem();
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				olvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				var ff = FirmwareDatabase.Instance.FirmwareFilesByHash[o.Hash];
				if (o.Status == FirmwareOptionStatus.Ideal)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idIdeal;
					olvi.ToolTipText = FirmwaresConfigInfo.ttIdeal;
				}
				if (o.Status == FirmwareOptionStatus.Acceptable)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idAcceptable;
					olvi.ToolTipText = FirmwaresConfigInfo.ttAcceptable;
				}
				if (o.Status == FirmwareOptionStatus.Unacceptable)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idUnacceptable;
					olvi.ToolTipText = FirmwaresConfigInfo.ttUnacceptable;
				}
				if (o.Status == FirmwareOptionStatus.Bad)
				{
					olvi.ImageIndex = FirmwaresConfigInfo.idBad;
					olvi.ToolTipText = FirmwaresConfigInfo.ttBad;
				}
				olvi.SubItems[0].Text = ff.Size.ToString();
				olvi.SubItems[0].Font = Font; // why doesn't this work?
				olvi.SubItems[1].Text = $"sha1:{o.Hash}";
				olvi.SubItems[1].Font = _fixedFont;
				olvi.SubItems[2].Text = ff.RecommendedName;
				olvi.SubItems[2].Font = Font; // why doesn't this work?
				olvi.SubItems[3].Text = ff.Description;
				olvi.SubItems[3].Font = Font; // why doesn't this work?
				olvi.SubItems[4].Text = ff.Info;
				olvi.SubItems[4].Font = Font; // why doesn't this work?
				fciDialog.lvOptions.Items.Add(olvi);
			}

			fciDialog.lvOptions.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
			fciDialog.lvOptions.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
			fciDialog.lvOptions.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			fciDialog.lvOptions.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);

			fciDialog.ShowDialog(this);
		}

		private void LvFirmwaresContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			// hide menu items that aren't appropriate for multi-select
			tsmiSetCustomization.Visible = lvFirmwares.SelectedItems.Count == 1;
			tsmiInfo.Visible = lvFirmwares.SelectedItems.Count == 1;
		}

		private void TsmiCopy_Click(object sender, EventArgs e)
		{
			PerformListCopy();
		}

		private void LinkBasePath_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (Owner is PathConfig)
			{
				MessageBox.Show("C-C-C-Combo Breaker!", "Nice try, but");
				return;
			}

			using var pathConfig = new PathConfig(Manager, _firmwareUserSpecifications, _game, _mainForm, _pathEntries);
			pathConfig.ShowDialog(this);
			RefreshBasePath();
		}

		private void RefreshBasePath()
		{
			string oldBasePath = _currSelectorDir;
			linkBasePath.Text = _currSelectorDir = _pathEntries.FirmwareAbsolutePath();
			if (oldBasePath != _currSelectorDir)
			{
				DoScan();
			}
		}

		private void TbbImport_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog { Multiselect = true };
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			RunImportJob(ofd.FileNames);
		}

		private bool RunImportJobSingle(string basePath, string f, ref string errors)
		{
			try
			{
				var fi = new FileInfo(f);
				if (!fi.Exists)
				{
					return false;
				}

				string target = Path.Combine(basePath, fi.Name);
				if (new FileInfo(target).Exists)
				{
					// compare the files, if they're the same. don't do anything
					if (File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(f)))
					{
						return false;
					}

					// hmm they're different. import but rename it
					string dir = Path.GetDirectoryName(target);
					string ext = Path.GetExtension(target);
					string name = Path.GetFileNameWithoutExtension(target);
					name += " (variant)";
					target = Path.Combine(dir, name) + ext;
				}

				Directory.CreateDirectory(Path.GetDirectoryName(target));
				fi.CopyTo(target, false);
				return true;
			}
			catch
			{
				if (errors != "")
				{
					errors += "\n";
				}

				errors += f;
				return false;
			}
		}

		private void RunImportJob(IEnumerable<string> files)
		{
			bool didSomething = false;
			var basePath = _pathEntries.FirmwareAbsolutePath();
			string errors = "";
			foreach(var f in files)
			{
				using var hf = new HawkFile(f);
				if (hf.IsArchive)
				{
					// blech. the worst extraction code in the universe.
					string extractPath = $"{Path.GetTempFileName()}.dir";
					var di = Directory.CreateDirectory(extractPath);

					try
					{
						foreach (var ai in hf.ArchiveItems)
						{
							hf.BindArchiveMember(ai);
							var stream = hf.GetStream();
							var ms = new MemoryStream();
							Util.CopyStream(hf.GetStream(), ms, stream.Length);
							string outfile = ai.Name;
							string myname = Path.GetFileName(outfile);
							outfile = Path.Combine(extractPath, myname);
							File.WriteAllBytes(outfile, ms.ToArray());
							hf.Unbind();

							if (_cbAllowImport.Checked || Manager.CanFileBeImported(outfile))
							{
								didSomething |= RunImportJobSingle(basePath, outfile, ref errors);
							}
						}
					}
					finally
					{
						di.Delete(true);
					}
				}
				else
				{
					if (_cbAllowImport.Checked || Manager.CanFileBeImported(hf.CanonicalFullPath))
					{
						didSomething |= RunImportJobSingle(basePath, f, ref errors);
					} 
				}
			}

			if (!string.IsNullOrEmpty(errors))
			{
				MessageBox.Show(errors, "Error importing these files");
			}

			if (didSomething)
			{
				DoScan();
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void LvFirmwares_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void LvFirmwares_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				RunImportJob(files);
			}
		}
	}
}
