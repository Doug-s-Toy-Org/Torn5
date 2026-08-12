namespace Torn5.Controls
{
	partial class SessionsControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.panelTop = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.label25 = new System.Windows.Forms.Label();
			this.numericBetween = new System.Windows.Forms.NumericUpDown();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.panelTop.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericBetween)).BeginInit();
			this.SuspendLayout();
			// 
			// panelTop
			// 
			this.panelTop.Controls.Add(this.label1);
			this.panelTop.Controls.Add(this.label25);
			this.panelTop.Controls.Add(this.numericBetween);
			this.panelTop.Controls.Add(this.buttonRemove);
			this.panelTop.Controls.Add(this.buttonAdd);
			this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelTop.Location = new System.Drawing.Point(0, 0);
			this.panelTop.Name = "panelTop";
			this.panelTop.Size = new System.Drawing.Size(240, 97);
			this.panelTop.TabIndex = 0;
			this.panelTop.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelTopPaint);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 76);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(105, 13);
			this.label1.TabIndex = 12;
			this.label1.Text = "between game starts";
			// 
			// label25
			// 
			this.label25.AutoSize = true;
			this.label25.Location = new System.Drawing.Point(62, 57);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(43, 13);
			this.label25.TabIndex = 11;
			this.label25.Text = "minutes";
			// 
			// numericBetween
			// 
			this.numericBetween.Location = new System.Drawing.Point(6, 55);
			this.numericBetween.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
			this.numericBetween.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericBetween.Name = "numericBetween";
			this.numericBetween.Size = new System.Drawing.Size(53, 20);
			this.numericBetween.TabIndex = 10;
			this.numericBetween.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
			// 
			// buttonRemove
			// 
			this.buttonRemove.Location = new System.Drawing.Point(3, 29);
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Size = new System.Drawing.Size(113, 23);
			this.buttonRemove.TabIndex = 1;
			this.buttonRemove.Text = "Remove last session";
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.ButtonRemoveClick);
			// 
			// buttonAdd
			// 
			this.buttonAdd.Location = new System.Drawing.Point(3, 3);
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.Size = new System.Drawing.Size(113, 23);
			this.buttonAdd.TabIndex = 0;
			this.buttonAdd.Text = "Add session";
			this.buttonAdd.UseVisualStyleBackColor = true;
			this.buttonAdd.Click += new System.EventHandler(this.ButtonAddClick);
			// 
			// SessionsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.Controls.Add(this.panelTop);
			this.Name = "SessionsControl";
			this.Size = new System.Drawing.Size(240, 150);
			this.panelTop.ResumeLayout(false);
			this.panelTop.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericBetween)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panelTop;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.NumericUpDown numericBetween;
		private System.Windows.Forms.Label label1;
	}
}
