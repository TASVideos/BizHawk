namespace BizHawk.Client.EmuHawk
{
	partial class PathConfig
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.PathTabControl = new System.Windows.Forms.TabControl();
			this.SaveBtn = new System.Windows.Forms.Button();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SpecialCommandsBtn = new System.Windows.Forms.Button();
			this.RecentForROMs = new System.Windows.Forms.CheckBox();
			this.DefaultsBtn = new System.Windows.Forms.Button();
			this.tcMain = new System.Windows.Forms.TabControl();
			this.tpGlobal = new System.Windows.Forms.TabPage();
			this.tpSystems = new System.Windows.Forms.TabPage();
			this.comboSystem = new System.Windows.Forms.ComboBox();
			this.lblSystem = new BizHawk.WinForms.Controls.LocLabelEx();
			this.tcMain.SuspendLayout();
			this.tpSystems.SuspendLayout();
			this.SuspendLayout();
			// 
			// Ok
			// 
			this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Ok.Location = new System.Drawing.Point(616, 505);
			this.Ok.Name = "OK";
			this.Ok.Size = new System.Drawing.Size(75, 23);
			this.Ok.TabIndex = 0;
			this.Ok.Text = "&OK";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(697, 505);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// PathTabControl
			// 
			this.PathTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PathTabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
			this.PathTabControl.ItemSize = new System.Drawing.Size(0, 1);
			this.PathTabControl.Location = new System.Drawing.Point(6, 31);
			this.PathTabControl.Multiline = true;
			this.PathTabControl.Name = "PathTabControl";
			this.PathTabControl.SelectedIndex = 0;
			this.PathTabControl.Size = new System.Drawing.Size(747, 401);
			this.PathTabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
			this.PathTabControl.TabIndex = 2;
			// 
			// SaveBtn
			// 
			this.SaveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.SaveBtn.Location = new System.Drawing.Point(12, 505);
			this.SaveBtn.Name = "SaveBtn";
			this.SaveBtn.Size = new System.Drawing.Size(75, 23);
			this.SaveBtn.TabIndex = 3;
			this.SaveBtn.Text = "&Save";
			this.SaveBtn.UseVisualStyleBackColor = true;
			this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(672, 19);
			this.label1.Name = "label1";
			this.label1.Text = "Special Commands";
			// 
			// SpecialCommandsBtn
			// 
			this.SpecialCommandsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SpecialCommandsBtn.Location = new System.Drawing.Point(641, 14);
			this.SpecialCommandsBtn.Name = "SpecialCommandsBtn";
			this.SpecialCommandsBtn.Size = new System.Drawing.Size(26, 23);
			this.SpecialCommandsBtn.TabIndex = 209;
			this.SpecialCommandsBtn.UseVisualStyleBackColor = true;
			this.SpecialCommandsBtn.Click += new System.EventHandler(this.SpecialCommandsBtn_Click);
			// 
			// RecentForROMs
			// 
			this.RecentForROMs.AutoSize = true;
			this.RecentForROMs.Location = new System.Drawing.Point(12, 18);
			this.RecentForROMs.Name = "RecentForROMs";
			this.RecentForROMs.Size = new System.Drawing.Size(184, 17);
			this.RecentForROMs.TabIndex = 207;
			this.RecentForROMs.Text = "Always use recent path for ROMs";
			this.RecentForROMs.UseVisualStyleBackColor = true;
			this.RecentForROMs.CheckedChanged += new System.EventHandler(this.RecentForRoms_CheckedChanged);
			// 
			// DefaultsBtn
			// 
			this.DefaultsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DefaultsBtn.Location = new System.Drawing.Point(93, 505);
			this.DefaultsBtn.Name = "DefaultsBtn";
			this.DefaultsBtn.Size = new System.Drawing.Size(75, 23);
			this.DefaultsBtn.TabIndex = 211;
			this.DefaultsBtn.Text = "&Defaults";
			this.DefaultsBtn.UseVisualStyleBackColor = true;
			this.DefaultsBtn.Click += new System.EventHandler(this.DefaultsBtn_Click);
			// 
			// tcMain
			// 
			this.tcMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tcMain.Controls.Add(this.tpGlobal);
			this.tcMain.Controls.Add(this.tpSystems);
			this.tcMain.Location = new System.Drawing.Point(12, 41);
			this.tcMain.Name = "tcMain";
			this.tcMain.SelectedIndex = 0;
			this.tcMain.Size = new System.Drawing.Size(760, 458);
			this.tcMain.TabIndex = 213;
			// 
			// tpGlobal
			// 
			this.tpGlobal.AutoScroll = true;
			this.tpGlobal.Location = new System.Drawing.Point(4, 22);
			this.tpGlobal.Name = "tpGlobal";
			this.tpGlobal.Padding = new System.Windows.Forms.Padding(3);
			this.tpGlobal.Size = new System.Drawing.Size(752, 432);
			this.tpGlobal.TabIndex = 0;
			this.tpGlobal.Text = "Global";
			this.tpGlobal.UseVisualStyleBackColor = true;
			// 
			// tpSystems
			// 
			this.tpSystems.Controls.Add(this.lblSystem);
			this.tpSystems.Controls.Add(this.comboSystem);
			this.tpSystems.Controls.Add(this.PathTabControl);
			this.tpSystems.Location = new System.Drawing.Point(4, 22);
			this.tpSystems.Name = "tpSystems";
			this.tpSystems.Padding = new System.Windows.Forms.Padding(3);
			this.tpSystems.Size = new System.Drawing.Size(752, 432);
			this.tpSystems.TabIndex = 1;
			this.tpSystems.Text = "by System";
			this.tpSystems.UseVisualStyleBackColor = true;
			// 
			// comboSystem
			// 
			this.comboSystem.FormattingEnabled = true;
			this.comboSystem.Location = new System.Drawing.Point(48, 4);
			this.comboSystem.Name = "comboSystem";
			this.comboSystem.Size = new System.Drawing.Size(200, 21);
			this.comboSystem.TabIndex = 3;
			this.comboSystem.SelectedIndexChanged += new System.EventHandler(this.comboSystem_SelectedIndexChanged);
			// 
			// lblSystem
			// 
			this.lblSystem.Location = new System.Drawing.Point(3, 7);
			this.lblSystem.Name = "lblSystem";
			this.lblSystem.Text = "System:";
			// 
			// PathConfig
			// 
			this.AcceptButton = this.Ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(784, 540);
			this.Controls.Add(this.tcMain);
			this.Controls.Add(this.DefaultsBtn);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.SpecialCommandsBtn);
			this.Controls.Add(this.RecentForROMs);
			this.Controls.Add(this.SaveBtn);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.Ok);
			this.MinimumSize = new System.Drawing.Size(360, 250);
			this.Name = "PathConfig";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Path Configuration";
			this.Load += new System.EventHandler(this.NewPathConfig_Load);
			this.tcMain.ResumeLayout(false);
			this.tpSystems.ResumeLayout(false);
			this.tpSystems.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.TabControl PathTabControl;
		private System.Windows.Forms.Button SaveBtn;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button SpecialCommandsBtn;
		private System.Windows.Forms.CheckBox RecentForROMs;
		private System.Windows.Forms.Button DefaultsBtn;
		private System.Windows.Forms.TabControl tcMain;
		private System.Windows.Forms.TabPage tpGlobal;
		private System.Windows.Forms.TabPage tpSystems;
		private System.Windows.Forms.ComboBox comboSystem;
		private WinForms.Controls.LocLabelEx lblSystem;
	}
}