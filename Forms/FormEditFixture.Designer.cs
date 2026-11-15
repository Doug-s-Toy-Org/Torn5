namespace Torn5.Forms
{
	partial class FormEditFixture
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
			this.textPlayCounts = new System.Windows.Forms.TextBox();
			this.textScore = new System.Windows.Forms.TextBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainerTop = new System.Windows.Forms.SplitContainer();
			this.gridEditor = new Torn5.Controls.GridEditor();
			this.panel1 = new System.Windows.Forms.Panel();
			this.buttonPaste = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonCopy = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).BeginInit();
			this.splitContainerTop.Panel1.SuspendLayout();
			this.splitContainerTop.Panel2.SuspendLayout();
			this.splitContainerTop.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// textPlayCounts
			// 
			this.textPlayCounts.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textPlayCounts.Font = new System.Drawing.Font("Lucida Console", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textPlayCounts.Location = new System.Drawing.Point(0, 0);
			this.textPlayCounts.Margin = new System.Windows.Forms.Padding(2);
			this.textPlayCounts.Multiline = true;
			this.textPlayCounts.Name = "textPlayCounts";
			this.textPlayCounts.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textPlayCounts.Size = new System.Drawing.Size(503, 190);
			this.textPlayCounts.TabIndex = 1;
			this.textPlayCounts.Text = "test";
			// 
			// textScore
			// 
			this.textScore.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textScore.Location = new System.Drawing.Point(0, 0);
			this.textScore.Margin = new System.Windows.Forms.Padding(2);
			this.textScore.Multiline = true;
			this.textScore.Name = "textScore";
			this.textScore.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textScore.Size = new System.Drawing.Size(357, 99);
			this.textScore.TabIndex = 2;
			// 
			// splitContainer1
			// 
			this.splitContainer1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Margin = new System.Windows.Forms.Padding(2);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainerTop);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.BackColor = System.Drawing.SystemColors.Control;
			this.splitContainer1.Panel2.Controls.Add(this.textScore);
			this.splitContainer1.Panel2.Controls.Add(this.panel1);
			this.splitContainer1.Size = new System.Drawing.Size(533, 292);
			this.splitContainer1.SplitterDistance = 190;
			this.splitContainer1.SplitterWidth = 3;
			this.splitContainer1.TabIndex = 3;
			this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.SplitterMoved);
			// 
			// splitContainerTop
			// 
			this.splitContainerTop.BackColor = System.Drawing.SystemColors.ControlDark;
			this.splitContainerTop.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainerTop.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainerTop.Location = new System.Drawing.Point(0, 0);
			this.splitContainerTop.Margin = new System.Windows.Forms.Padding(2);
			this.splitContainerTop.Name = "splitContainerTop";
			// 
			// splitContainerTop.Panel1
			// 
			this.splitContainerTop.Panel1.BackColor = System.Drawing.SystemColors.Control;
			this.splitContainerTop.Panel1.Controls.Add(this.gridEditor);
			// 
			// splitContainerTop.Panel2
			// 
			this.splitContainerTop.Panel2.BackColor = System.Drawing.SystemColors.Control;
			this.splitContainerTop.Panel2.Controls.Add(this.textPlayCounts);
			this.splitContainerTop.Size = new System.Drawing.Size(533, 190);
			this.splitContainerTop.SplitterDistance = 27;
			this.splitContainerTop.SplitterWidth = 3;
			this.splitContainerTop.TabIndex = 0;
			this.splitContainerTop.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.SplitterMoved);
			// 
			// gridEditor
			// 
			this.gridEditor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridEditor.Fixture = null;
			this.gridEditor.Location = new System.Drawing.Point(0, 0);
			this.gridEditor.Margin = new System.Windows.Forms.Padding(1);
			this.gridEditor.Name = "gridEditor";
			this.gridEditor.Rings = false;
			this.gridEditor.Size = new System.Drawing.Size(27, 190);
			this.gridEditor.TabIndex = 0;
			this.gridEditor.Changed += new System.EventHandler(this.GridEditorChanged);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.buttonCopy);
			this.panel1.Controls.Add(this.buttonPaste);
			this.panel1.Controls.Add(this.buttonOK);
			this.panel1.Controls.Add(this.buttonCancel);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(357, 0);
			this.panel1.Margin = new System.Windows.Forms.Padding(2);
			this.panel1.MinimumSize = new System.Drawing.Size(176, 62);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(176, 99);
			this.panel1.TabIndex = 5;
			// 
			// buttonPaste
			// 
			this.buttonPaste.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonPaste.Location = new System.Drawing.Point(90, 38);
			this.buttonPaste.Margin = new System.Windows.Forms.Padding(2);
			this.buttonPaste.Name = "buttonPaste";
			this.buttonPaste.Size = new System.Drawing.Size(75, 23);
			this.buttonPaste.TabIndex = 1;
			this.buttonPaste.Text = "Paste";
			this.buttonPaste.UseVisualStyleBackColor = true;
			this.buttonPaste.Click += new System.EventHandler(this.ButtonPasteClick);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(11, 65);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(2);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "&OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(90, 65);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(2);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonCopy
			// 
			this.buttonCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCopy.Location = new System.Drawing.Point(11, 38);
			this.buttonCopy.Margin = new System.Windows.Forms.Padding(2);
			this.buttonCopy.Name = "buttonCopy";
			this.buttonCopy.Size = new System.Drawing.Size(75, 23);
			this.buttonCopy.TabIndex = 0;
			this.buttonCopy.Text = "Copy";
			this.buttonCopy.UseVisualStyleBackColor = true;
			this.buttonCopy.Click += new System.EventHandler(this.ButtonCopyClick);
			// 
			// FormEditFixture
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(533, 292);
			this.Controls.Add(this.splitContainer1);
			this.Margin = new System.Windows.Forms.Padding(2);
			this.Name = "FormEditFixture";
			this.Text = "Edit Fixture";
			this.Shown += new System.EventHandler(this.FormEditFixture_Shown);
			this.ResizeBegin += new System.EventHandler(this.FormEditFixture_ResizeBegin);
			this.ResizeEnd += new System.EventHandler(this.FormEditFixture_ResizeEnd);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainerTop.Panel1.ResumeLayout(false);
			this.splitContainerTop.Panel2.ResumeLayout(false);
			this.splitContainerTop.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).EndInit();
			this.splitContainerTop.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Controls.GridEditor gridEditor;
		private System.Windows.Forms.TextBox textPlayCounts;
		private System.Windows.Forms.TextBox textScore;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.SplitContainer splitContainerTop;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button buttonPaste;
		private System.Windows.Forms.Button buttonCopy;
	}
}