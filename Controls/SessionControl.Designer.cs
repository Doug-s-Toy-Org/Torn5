namespace Torn5.Controls
{
	partial class SessionControl
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
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.numericBetween = new System.Windows.Forms.NumericUpDown();
			this.dateTimeStart = new System.Windows.Forms.DateTimePicker();
			this.label25 = new System.Windows.Forms.Label();
			this.numericGames = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.numericBetween)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericGames)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 5);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Start";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 57);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(90, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Number of games";
			// 
			// numericBetween
			// 
			this.numericBetween.Location = new System.Drawing.Point(165, 29);
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
			this.numericBetween.TabIndex = 8;
			this.numericBetween.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
			this.numericBetween.ValueChanged += new System.EventHandler(this.ValueChanged);
			// 
			// dateTimeStart
			// 
			this.dateTimeStart.CustomFormat = "ddd dd/MM/yyyy hh:mm tt";
			this.dateTimeStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dateTimeStart.Location = new System.Drawing.Point(38, 3);
			this.dateTimeStart.Name = "dateTimeStart";
			this.dateTimeStart.Size = new System.Drawing.Size(180, 20);
			this.dateTimeStart.TabIndex = 7;
			this.dateTimeStart.ValueChanged += new System.EventHandler(this.ValueChanged);
			// 
			// label25
			// 
			this.label25.AutoSize = true;
			this.label25.Location = new System.Drawing.Point(3, 31);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(145, 13);
			this.label25.TabIndex = 9;
			this.label25.Text = "Minutes between game starts";
			// 
			// numericGames
			// 
			this.numericGames.Location = new System.Drawing.Point(165, 55);
			this.numericGames.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
			this.numericGames.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericGames.Name = "numericGames";
			this.numericGames.Size = new System.Drawing.Size(53, 20);
			this.numericGames.TabIndex = 10;
			this.numericGames.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.numericGames.ValueChanged += new System.EventHandler(this.ValueChanged);
			// 
			// SessionControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.numericGames);
			this.Controls.Add(this.label25);
			this.Controls.Add(this.numericBetween);
			this.Controls.Add(this.dateTimeStart);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label1);
			this.Name = "SessionControl";
			this.Size = new System.Drawing.Size(221, 78);
			((System.ComponentModel.ISupportInitialize)(this.numericBetween)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericGames)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown numericBetween;
		private System.Windows.Forms.DateTimePicker dateTimeStart;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.NumericUpDown numericGames;
	}
}
