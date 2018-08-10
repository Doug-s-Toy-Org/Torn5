﻿
namespace Torn.UI
{
	partial class BaseBox
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.listView1 = new System.Windows.Forms.ListView();
			this.colPack = new System.Windows.Forms.ColumnHeader();
			this.colPlayer = new System.Windows.Forms.ColumnHeader();
			this.colScore = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			// 
			// listView1
			// 
			this.listView1.AllowDrop = true;
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
									this.colPack,
									this.colPlayer,
									this.colScore});
			this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView1.FullRowSelect = true;
			this.listView1.Location = new System.Drawing.Point(0, 0);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(327, 150);
			this.listView1.TabIndex = 0;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.ListView1ItemDrag);
			this.listView1.SizeChanged += new System.EventHandler(this.ListView1SizeChanged);
			this.listView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.ListView1DragDrop);
			this.listView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.ListView1DragEnter);
			this.listView1.DoubleClick += new System.EventHandler(this.ListView1DoubleClick);
			// 
			// colPack
			// 
			this.colPack.Text = "Pack";
			this.colPack.Width = 70;
			// 
			// colPlayer
			// 
			this.colPlayer.Text = "Player";
			this.colPlayer.Width = 100;
			// 
			// colScore
			// 
			this.colScore.Text = "Score";
			this.colScore.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.colScore.Width = 50;
			// 
			// BaseBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.listView1);
			this.Name = "BaseBox";
			this.Size = new System.Drawing.Size(327, 150);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.ColumnHeader colScore;
		private System.Windows.Forms.ColumnHeader colPlayer;
		private System.Windows.Forms.ColumnHeader colPack;
		private System.Windows.Forms.ListView listView1;
	}
}
