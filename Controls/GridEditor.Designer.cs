namespace Torn5.Controls
{
	partial class GridEditor
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
			this.SuspendLayout();
			// 
			// GridEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "GridEditor";
			this.Size = new System.Drawing.Size(100, 98);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.ThisPaint);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GridEditorMouseDown);
			this.MouseLeave += new System.EventHandler(this.GridEditorMouseLeave);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GridEditorMouseMove);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GridEditorMouseUp);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
