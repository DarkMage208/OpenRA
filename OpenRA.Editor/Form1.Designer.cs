﻿namespace OpenRA.Editor
{
	partial class Form1
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.tilePalette = new System.Windows.Forms.FlowLayoutPanel();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.actorPalette = new System.Windows.Forms.FlowLayoutPanel();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.resourcePalette = new System.Windows.Forms.FlowLayoutPanel();
			this.surface1 = new OpenRA.Editor.Surface();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton5 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton4 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton6 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton7 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.tt = new System.Windows.Forms.ToolTip(this.components);
			this.folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.Controls.Add(this.splitContainer1);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(985, 680);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(985, 705);
			this.toolStripContainer1.TabIndex = 1;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.tabControl1);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.surface1);
			this.splitContainer1.Size = new System.Drawing.Size(985, 680);
			this.splitContainer1.SplitterDistance = 198;
			this.splitContainer1.TabIndex = 0;
			// 
			// tabControl1
			// 
			this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Left;
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Multiline = true;
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(198, 680);
			this.tabControl1.TabIndex = 0;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.tilePalette);
			this.tabPage1.Location = new System.Drawing.Point(23, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(171, 672);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Templates";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// tilePalette
			// 
			this.tilePalette.AutoScroll = true;
			this.tilePalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tilePalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tilePalette.Location = new System.Drawing.Point(3, 3);
			this.tilePalette.Name = "tilePalette";
			this.tilePalette.Size = new System.Drawing.Size(165, 666);
			this.tilePalette.TabIndex = 1;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.actorPalette);
			this.tabPage2.Location = new System.Drawing.Point(23, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(171, 672);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Actors";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// actorPalette
			// 
			this.actorPalette.AutoScroll = true;
			this.actorPalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.actorPalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.actorPalette.Location = new System.Drawing.Point(3, 3);
			this.actorPalette.Name = "actorPalette";
			this.actorPalette.Size = new System.Drawing.Size(165, 666);
			this.actorPalette.TabIndex = 2;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.resourcePalette);
			this.tabPage3.Location = new System.Drawing.Point(23, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(171, 672);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Resources";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// resourcePalette
			// 
			this.resourcePalette.AutoScroll = true;
			this.resourcePalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.resourcePalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.resourcePalette.Location = new System.Drawing.Point(0, 0);
			this.resourcePalette.Name = "resourcePalette";
			this.resourcePalette.Size = new System.Drawing.Size(171, 672);
			this.resourcePalette.TabIndex = 3;
			// 
			// surface1
			// 
			this.surface1.BackColor = System.Drawing.Color.Black;
			this.surface1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.surface1.Location = new System.Drawing.Point(0, 0);
			this.surface1.Name = "surface1";
			this.surface1.Size = new System.Drawing.Size(783, 680);
			this.surface1.TabIndex = 2;
			this.surface1.Text = "surface1";
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton3,
            this.toolStripButton5,
            this.toolStripButton4,
            this.toolStripButton1,
            this.toolStripButton6,
            this.toolStripButton7,
            this.toolStripButton2});
			this.toolStrip1.Location = new System.Drawing.Point(3, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(511, 25);
			this.toolStrip1.TabIndex = 0;
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(51, 22);
			this.toolStripButton3.Text = "New";
			this.toolStripButton3.Click += new System.EventHandler(this.NewClicked);
			// 
			// toolStripButton5
			// 
			this.toolStripButton5.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton5.Image")));
			this.toolStripButton5.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton5.Name = "toolStripButton5";
			this.toolStripButton5.Size = new System.Drawing.Size(56, 22);
			this.toolStripButton5.Text = "Open";
			this.toolStripButton5.Click += new System.EventHandler(this.OpenClicked);
			// 
			// toolStripButton4
			// 
			this.toolStripButton4.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton4.Image")));
			this.toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton4.Name = "toolStripButton4";
			this.toolStripButton4.Size = new System.Drawing.Size(51, 22);
			this.toolStripButton4.Text = "Save";
			this.toolStripButton4.Click += new System.EventHandler(this.SaveClicked);
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(76, 22);
			this.toolStripButton1.Text = "Save As...";
			this.toolStripButton1.Click += new System.EventHandler(this.SaveAsClicked);
			// 
			// toolStripButton6
			// 
			this.toolStripButton6.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton6.Image")));
			this.toolStripButton6.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton6.Name = "toolStripButton6";
			this.toolStripButton6.Size = new System.Drawing.Size(59, 22);
			this.toolStripButton6.Text = "Resize";
			this.toolStripButton6.Click += new System.EventHandler(this.ResizeClicked);
			// 
			// toolStripButton7
			// 
			this.toolStripButton7.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton7.Image")));
			this.toolStripButton7.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton7.Name = "toolStripButton7";
			this.toolStripButton7.Size = new System.Drawing.Size(80, 22);
			this.toolStripButton7.Text = "Properties";
			this.toolStripButton7.Click += new System.EventHandler(this.PropertiesClicked);
			// 
			// toolStripButton2
			// 
			this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
			this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton2.Name = "toolStripButton2";
			this.toolStripButton2.Size = new System.Drawing.Size(95, 22);
			this.toolStripButton2.Text = "Spawnpoints";
			this.toolStripButton2.Click += new System.EventHandler(this.SpawnPointsClicked);
			// 
			// tt
			// 
			this.tt.ShowAlways = true;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(985, 705);
			this.Controls.Add(this.toolStripContainer1);
			this.Name = "Form1";
			this.Text = "OpenRA Editor";
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private Surface surface1;
		private System.Windows.Forms.ToolTip tt;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.FlowLayoutPanel tilePalette;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.FlowLayoutPanel actorPalette;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.FlowLayoutPanel resourcePalette;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ToolStripButton toolStripButton5;
		private System.Windows.Forms.ToolStripButton toolStripButton4;
		private System.Windows.Forms.FolderBrowserDialog folderBrowser;
		private System.Windows.Forms.ToolStripButton toolStripButton6;
		private System.Windows.Forms.ToolStripButton toolStripButton7;

	}
}

