﻿
namespace Torn.UI
{
	partial class FormFixture
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
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
			this.buttonGenerate = new System.Windows.Forms.Button();
			this.buttonImportGrid = new System.Windows.Forms.Button();
			this.buttonImportGames = new System.Windows.Forms.Button();
			this.textBoxSeparator = new System.Windows.Forms.TextBox();
			this.radioButtonOther = new System.Windows.Forms.RadioButton();
			this.radioButtonTab = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxGames = new System.Windows.Forms.TextBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabTeams = new System.Windows.Forms.TabPage();
			this.numericRings = new System.Windows.Forms.NumericUpDown();
			this.checkRings = new System.Windows.Forms.CheckBox();
			this.label26 = new System.Windows.Forms.Label();
			this.backToBackPenalty = new System.Windows.Forms.NumericUpDown();
			this.fixtureTeamsList = new System.Windows.Forms.Panel();
			this.label25 = new System.Windows.Forms.Label();
			this.minBetween = new System.Windows.Forms.NumericUpDown();
			this.gameDateTime = new System.Windows.Forms.DateTimePicker();
			this.label24 = new System.Windows.Forms.Label();
			this.outputList = new System.Windows.Forms.CheckBox();
			this.outputGrid = new System.Windows.Forms.CheckBox();
			this.printReport1 = new Torn5.Controls.PrintReport();
			this.reportTeamsList = new Torn5.Controls.DisplayReport();
			this.continueGenerating = new System.Windows.Forms.Button();
			this.scoreLabel = new System.Windows.Forms.Label();
			this.label23 = new System.Windows.Forms.Label();
			this.maxTime = new System.Windows.Forms.NumericUpDown();
			this.referee = new System.Windows.Forms.CheckBox();
			this.green = new System.Windows.Forms.CheckBox();
			this.yellow = new System.Windows.Forms.CheckBox();
			this.blue = new System.Windows.Forms.CheckBox();
			this.red = new System.Windows.Forms.CheckBox();
			this.label22 = new System.Windows.Forms.Label();
			this.gamesPerTeamInput = new System.Windows.Forms.NumericUpDown();
			this.tabGamesList = new System.Windows.Forms.TabPage();
			this.splitContainerGamesList = new System.Windows.Forms.SplitContainer();
			this.buttonExportGames = new System.Windows.Forms.Button();
			this.buttonClearGames = new System.Windows.Forms.Button();
			this.displayReportGames = new Torn5.Controls.DisplayReport();
			this.tabGamesGrid = new System.Windows.Forms.TabPage();
			this.splitContainerGamesGrid = new System.Windows.Forms.SplitContainer();
			this.datePicker = new System.Windows.Forms.DateTimePicker();
			this.timePicker = new System.Windows.Forms.DateTimePicker();
			this.numericMinutes = new System.Windows.Forms.NumericUpDown();
			this.buttonClearGrid = new System.Windows.Forms.Button();
			this.textBoxGrid = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.displayReportGrid = new Torn5.Controls.DisplayReport();
			this.tabGraphic = new System.Windows.Forms.TabPage();
			this.numericSize = new System.Windows.Forms.NumericUpDown();
			this.panelGraphic = new System.Windows.Forms.Panel();
			this.tabFinals = new System.Windows.Forms.TabPage();
			this.teamsList = new System.Windows.Forms.Panel();
			this.frameFinals1 = new Torn5.Controls.FrameFinals();
			this.tabPyramid = new System.Windows.Forms.TabPage();
			this.framePyramid1 = new Torn5.Controls.FramePyramid();
			this.tabPyramidRound = new System.Windows.Forms.TabPage();
			this.framePyramidRound1 = new Torn5.Controls.FramePyramidRound();
			this.tabControl1.SuspendLayout();
			this.tabTeams.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericRings)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.backToBackPenalty)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.minBetween)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.maxTime)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.gamesPerTeamInput)).BeginInit();
			this.tabGamesList.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerGamesList)).BeginInit();
			this.splitContainerGamesList.Panel1.SuspendLayout();
			this.splitContainerGamesList.Panel2.SuspendLayout();
			this.splitContainerGamesList.SuspendLayout();
			this.tabGamesGrid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerGamesGrid)).BeginInit();
			this.splitContainerGamesGrid.Panel1.SuspendLayout();
			this.splitContainerGamesGrid.Panel2.SuspendLayout();
			this.splitContainerGamesGrid.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericMinutes)).BeginInit();
			this.tabGraphic.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericSize)).BeginInit();
			this.tabFinals.SuspendLayout();
			this.tabPyramid.SuspendLayout();
			this.tabPyramidRound.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonGenerate
			// 
			this.buttonGenerate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonGenerate.Location = new System.Drawing.Point(206, 631);
			this.buttonGenerate.Name = "buttonGenerate";
			this.buttonGenerate.Size = new System.Drawing.Size(75, 23);
			this.buttonGenerate.TabIndex = 10;
			this.buttonGenerate.Text = "Generate";
			this.buttonGenerate.UseVisualStyleBackColor = true;
			this.buttonGenerate.Click += new System.EventHandler(this.ButtonGenerateClick);
			// 
			// buttonImportGrid
			// 
			this.buttonImportGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImportGrid.Location = new System.Drawing.Point(676, 702);
			this.buttonImportGrid.Name = "buttonImportGrid";
			this.buttonImportGrid.Size = new System.Drawing.Size(75, 23);
			this.buttonImportGrid.TabIndex = 25;
			this.buttonImportGrid.Text = "Import Grid";
			this.buttonImportGrid.UseVisualStyleBackColor = true;
			this.buttonImportGrid.Click += new System.EventHandler(this.ButtonImportGridClick);
			// 
			// buttonImportGames
			// 
			this.buttonImportGames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImportGames.Location = new System.Drawing.Point(450, 706);
			this.buttonImportGames.Name = "buttonImportGames";
			this.buttonImportGames.Size = new System.Drawing.Size(75, 23);
			this.buttonImportGames.TabIndex = 19;
			this.buttonImportGames.Text = "Import";
			this.buttonImportGames.UseVisualStyleBackColor = true;
			this.buttonImportGames.Click += new System.EventHandler(this.ButtonImportGamesClick);
			// 
			// textBoxSeparator
			// 
			this.textBoxSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxSeparator.Location = new System.Drawing.Point(65, 706);
			this.textBoxSeparator.Name = "textBoxSeparator";
			this.textBoxSeparator.Size = new System.Drawing.Size(49, 20);
			this.textBoxSeparator.TabIndex = 24;
			this.textBoxSeparator.Text = " ";
			// 
			// radioButtonOther
			// 
			this.radioButtonOther.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.radioButtonOther.Location = new System.Drawing.Point(10, 703);
			this.radioButtonOther.Name = "radioButtonOther";
			this.radioButtonOther.Size = new System.Drawing.Size(104, 24);
			this.radioButtonOther.TabIndex = 23;
			this.radioButtonOther.Text = "Other";
			this.radioButtonOther.UseVisualStyleBackColor = true;
			// 
			// radioButtonTab
			// 
			this.radioButtonTab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.radioButtonTab.Checked = true;
			this.radioButtonTab.Location = new System.Drawing.Point(10, 682);
			this.radioButtonTab.Name = "radioButtonTab";
			this.radioButtonTab.Size = new System.Drawing.Size(104, 24);
			this.radioButtonTab.TabIndex = 22;
			this.radioButtonTab.TabStop = true;
			this.radioButtonTab.Text = "Tab";
			this.radioButtonTab.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.Location = new System.Drawing.Point(6, 668);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(100, 23);
			this.label3.TabIndex = 21;
			this.label3.Text = "Separator:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 6);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(251, 27);
			this.label2.TabIndex = 20;
			this.label2.Text = "Enter a list of games, one game per line.\r\n(Date/time,teamnumber,teamnumber, ...)" +
    "";
			// 
			// textBoxGames
			// 
			this.textBoxGames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxGames.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxGames.Location = new System.Drawing.Point(6, 36);
			this.textBoxGames.Multiline = true;
			this.textBoxGames.Name = "textBoxGames";
			this.textBoxGames.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxGames.Size = new System.Drawing.Size(600, 629);
			this.textBoxGames.TabIndex = 18;
			this.textBoxGames.Text = "1/1/2000 7:00pm\t1\t2\t3\r\n1/1/2000 7:15pm\t2\t3\t4\r\n1/1/2000 7:30pm\t3\t4\t1\r\n1/1/2000 7:4" +
    "5pm\t2\t3\t4";
			this.textBoxGames.WordWrap = false;
			this.textBoxGames.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxKeyDown);
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabTeams);
			this.tabControl1.Controls.Add(this.tabGamesList);
			this.tabControl1.Controls.Add(this.tabGamesGrid);
			this.tabControl1.Controls.Add(this.tabGraphic);
			this.tabControl1.Controls.Add(this.tabFinals);
			this.tabControl1.Controls.Add(this.tabPyramid);
			this.tabControl1.Controls.Add(this.tabPyramidRound);
			this.tabControl1.Location = new System.Drawing.Point(12, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1232, 764);
			this.tabControl1.TabIndex = 19;
			// 
			// tabTeams
			// 
			this.tabTeams.Controls.Add(this.numericRings);
			this.tabTeams.Controls.Add(this.checkRings);
			this.tabTeams.Controls.Add(this.label26);
			this.tabTeams.Controls.Add(this.backToBackPenalty);
			this.tabTeams.Controls.Add(this.fixtureTeamsList);
			this.tabTeams.Controls.Add(this.label25);
			this.tabTeams.Controls.Add(this.minBetween);
			this.tabTeams.Controls.Add(this.gameDateTime);
			this.tabTeams.Controls.Add(this.label24);
			this.tabTeams.Controls.Add(this.outputList);
			this.tabTeams.Controls.Add(this.outputGrid);
			this.tabTeams.Controls.Add(this.printReport1);
			this.tabTeams.Controls.Add(this.continueGenerating);
			this.tabTeams.Controls.Add(this.scoreLabel);
			this.tabTeams.Controls.Add(this.label23);
			this.tabTeams.Controls.Add(this.maxTime);
			this.tabTeams.Controls.Add(this.referee);
			this.tabTeams.Controls.Add(this.green);
			this.tabTeams.Controls.Add(this.yellow);
			this.tabTeams.Controls.Add(this.blue);
			this.tabTeams.Controls.Add(this.red);
			this.tabTeams.Controls.Add(this.label22);
			this.tabTeams.Controls.Add(this.gamesPerTeamInput);
			this.tabTeams.Controls.Add(this.reportTeamsList);
			this.tabTeams.Controls.Add(this.buttonGenerate);
			this.tabTeams.Location = new System.Drawing.Point(4, 22);
			this.tabTeams.Name = "tabTeams";
			this.tabTeams.Padding = new System.Windows.Forms.Padding(3);
			this.tabTeams.Size = new System.Drawing.Size(1224, 738);
			this.tabTeams.TabIndex = 0;
			this.tabTeams.Text = "Teams";
			this.tabTeams.UseVisualStyleBackColor = true;
			// 
			// numericRings
			// 
			this.numericRings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.numericRings.Enabled = false;
			this.numericRings.Location = new System.Drawing.Point(211, 592);
			this.numericRings.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
			this.numericRings.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericRings.Name = "numericRings";
			this.numericRings.Size = new System.Drawing.Size(53, 20);
			this.numericRings.TabIndex = 37;
			this.numericRings.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// checkRings
			// 
			this.checkRings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkRings.AutoSize = true;
			this.checkRings.Location = new System.Drawing.Point(211, 575);
			this.checkRings.Name = "checkRings";
			this.checkRings.Size = new System.Drawing.Size(53, 17);
			this.checkRings.TabIndex = 36;
			this.checkRings.Text = "Rings";
			this.checkRings.UseVisualStyleBackColor = true;
			this.checkRings.CheckedChanged += new System.EventHandler(this.CheckRingsCheckedChanged);
			// 
			// label26
			// 
			this.label26.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label26.AutoSize = true;
			this.label26.Location = new System.Drawing.Point(6, 576);
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size(108, 13);
			this.label26.TabIndex = 35;
			this.label26.Text = "Back-to-back penalty";
			// 
			// backToBackPenalty
			// 
			this.backToBackPenalty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.backToBackPenalty.Location = new System.Drawing.Point(9, 592);
			this.backToBackPenalty.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this.backToBackPenalty.Name = "backToBackPenalty";
			this.backToBackPenalty.Size = new System.Drawing.Size(102, 20);
			this.backToBackPenalty.TabIndex = 34;
			this.backToBackPenalty.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
			// 
			// fixtureTeamsList
			// 
			this.fixtureTeamsList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.fixtureTeamsList.AutoScroll = true;
			this.fixtureTeamsList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.fixtureTeamsList.Location = new System.Drawing.Point(9, 8);
			this.fixtureTeamsList.Name = "fixtureTeamsList";
			this.fixtureTeamsList.Padding = new System.Windows.Forms.Padding(9, 4, 0, 0);
			this.fixtureTeamsList.Size = new System.Drawing.Size(272, 560);
			this.fixtureTeamsList.TabIndex = 33;
			// 
			// label25
			// 
			this.label25.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label25.AutoSize = true;
			this.label25.Location = new System.Drawing.Point(101, 654);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(88, 13);
			this.label25.TabIndex = 32;
			this.label25.Text = "Minutes between";
			// 
			// minBetween
			// 
			this.minBetween.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.minBetween.Location = new System.Drawing.Point(104, 670);
			this.minBetween.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
			this.minBetween.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.minBetween.Name = "minBetween";
			this.minBetween.Size = new System.Drawing.Size(89, 20);
			this.minBetween.TabIndex = 31;
			this.minBetween.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
			// 
			// gameDateTime
			// 
			this.gameDateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.gameDateTime.CustomFormat = "ddd dd/MM/yyyy hh:mm tt";
			this.gameDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.gameDateTime.Location = new System.Drawing.Point(9, 631);
			this.gameDateTime.Name = "gameDateTime";
			this.gameDateTime.Size = new System.Drawing.Size(184, 20);
			this.gameDateTime.TabIndex = 30;
			// 
			// label24
			// 
			this.label24.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label24.AutoSize = true;
			this.label24.Location = new System.Drawing.Point(6, 615);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(83, 13);
			this.label24.TabIndex = 29;
			this.label24.Text = "First Game Time";
			// 
			// outputList
			// 
			this.outputList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.outputList.AutoSize = true;
			this.outputList.Checked = true;
			this.outputList.CheckState = System.Windows.Forms.CheckState.Checked;
			this.outputList.Location = new System.Drawing.Point(206, 715);
			this.outputList.Name = "outputList";
			this.outputList.Size = new System.Drawing.Size(73, 17);
			this.outputList.TabIndex = 27;
			this.outputList.Text = "Game List";
			this.outputList.UseVisualStyleBackColor = true;
			this.outputList.CheckedChanged += new System.EventHandler(this.OutputCheckChanged);
			// 
			// outputGrid
			// 
			this.outputGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.outputGrid.AutoSize = true;
			this.outputGrid.Checked = true;
			this.outputGrid.CheckState = System.Windows.Forms.CheckState.Checked;
			this.outputGrid.Location = new System.Drawing.Point(206, 696);
			this.outputGrid.Name = "outputGrid";
			this.outputGrid.Size = new System.Drawing.Size(76, 17);
			this.outputGrid.TabIndex = 26;
			this.outputGrid.Text = "Game Grid";
			this.outputGrid.UseVisualStyleBackColor = true;
			this.outputGrid.CheckedChanged += new System.EventHandler(this.OutputCheckChanged);
			// 
			// printReport1
			// 
			this.printReport1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.printReport1.DisplayReport = this.reportTeamsList;
			this.printReport1.FileName = null;
			this.printReport1.Location = new System.Drawing.Point(1143, 6);
			this.printReport1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.printReport1.Name = "printReport1";
			this.printReport1.Size = new System.Drawing.Size(75, 513);
			this.printReport1.TabIndex = 25;
			this.printReport1.SaveHtmlTable += new System.EventHandler(this.PrintReportSaveHtmlTable);
			// 
			// reportTeamsList
			// 
			this.reportTeamsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.reportTeamsList.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.reportTeamsList.Location = new System.Drawing.Point(287, 8);
			this.reportTeamsList.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.reportTeamsList.Name = "reportTeamsList";
			this.reportTeamsList.Report = null;
			this.reportTeamsList.Size = new System.Drawing.Size(850, 724);
			this.reportTeamsList.TabIndex = 12;
			// 
			// continueGenerating
			// 
			this.continueGenerating.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.continueGenerating.Enabled = false;
			this.continueGenerating.Location = new System.Drawing.Point(206, 667);
			this.continueGenerating.Name = "continueGenerating";
			this.continueGenerating.Size = new System.Drawing.Size(75, 23);
			this.continueGenerating.TabIndex = 24;
			this.continueGenerating.Text = "Continue";
			this.continueGenerating.UseVisualStyleBackColor = true;
			this.continueGenerating.Click += new System.EventHandler(this.ContinueGenerateClick);
			// 
			// scoreLabel
			// 
			this.scoreLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.scoreLabel.Location = new System.Drawing.Point(600, 3);
			this.scoreLabel.Name = "scoreLabel";
			this.scoreLabel.Size = new System.Drawing.Size(618, 32);
			this.scoreLabel.TabIndex = 23;
			this.scoreLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.scoreLabel.Visible = false;
			// 
			// label23
			// 
			this.label23.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label23.AutoSize = true;
			this.label23.Location = new System.Drawing.Point(120, 576);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(81, 13);
			this.label23.TabIndex = 22;
			this.label23.Text = "Max Time (Sec)";
			// 
			// maxTime
			// 
			this.maxTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.maxTime.Location = new System.Drawing.Point(123, 592);
			this.maxTime.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this.maxTime.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.maxTime.Name = "maxTime";
			this.maxTime.Size = new System.Drawing.Size(75, 20);
			this.maxTime.TabIndex = 21;
			this.maxTime.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
			// 
			// referee
			// 
			this.referee.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.referee.AutoSize = true;
			this.referee.Checked = true;
			this.referee.CheckState = System.Windows.Forms.CheckState.Checked;
			this.referee.Location = new System.Drawing.Point(122, 715);
			this.referee.Name = "referee";
			this.referee.Size = new System.Drawing.Size(43, 17);
			this.referee.TabIndex = 20;
			this.referee.Text = "Ref";
			this.referee.UseVisualStyleBackColor = true;
			// 
			// green
			// 
			this.green.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.green.AutoSize = true;
			this.green.Location = new System.Drawing.Point(61, 715);
			this.green.Name = "green";
			this.green.Size = new System.Drawing.Size(55, 17);
			this.green.TabIndex = 19;
			this.green.Text = "Green";
			this.green.UseVisualStyleBackColor = true;
			// 
			// yellow
			// 
			this.yellow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.yellow.AutoSize = true;
			this.yellow.Checked = true;
			this.yellow.CheckState = System.Windows.Forms.CheckState.Checked;
			this.yellow.Location = new System.Drawing.Point(61, 696);
			this.yellow.Name = "yellow";
			this.yellow.Size = new System.Drawing.Size(57, 17);
			this.yellow.TabIndex = 18;
			this.yellow.Text = "Yellow";
			this.yellow.UseVisualStyleBackColor = true;
			// 
			// blue
			// 
			this.blue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.blue.AutoSize = true;
			this.blue.Checked = true;
			this.blue.CheckState = System.Windows.Forms.CheckState.Checked;
			this.blue.Location = new System.Drawing.Point(9, 715);
			this.blue.Name = "blue";
			this.blue.Size = new System.Drawing.Size(47, 17);
			this.blue.TabIndex = 17;
			this.blue.Text = "Blue";
			this.blue.UseVisualStyleBackColor = true;
			// 
			// red
			// 
			this.red.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.red.AutoSize = true;
			this.red.Checked = true;
			this.red.CheckState = System.Windows.Forms.CheckState.Checked;
			this.red.Location = new System.Drawing.Point(9, 696);
			this.red.Name = "red";
			this.red.Size = new System.Drawing.Size(46, 17);
			this.red.TabIndex = 16;
			this.red.Text = "Red";
			this.red.UseVisualStyleBackColor = true;
			// 
			// label22
			// 
			this.label22.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label22.AutoSize = true;
			this.label22.Location = new System.Drawing.Point(6, 654);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(89, 13);
			this.label22.TabIndex = 15;
			this.label22.Text = "Games Per Team";
			// 
			// gamesPerTeamInput
			// 
			this.gamesPerTeamInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.gamesPerTeamInput.Location = new System.Drawing.Point(9, 670);
			this.gamesPerTeamInput.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.gamesPerTeamInput.Name = "gamesPerTeamInput";
			this.gamesPerTeamInput.Size = new System.Drawing.Size(83, 20);
			this.gamesPerTeamInput.TabIndex = 14;
			this.gamesPerTeamInput.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
			// 
			// tabGamesList
			// 
			this.tabGamesList.Controls.Add(this.splitContainerGamesList);
			this.tabGamesList.Location = new System.Drawing.Point(4, 22);
			this.tabGamesList.Name = "tabGamesList";
			this.tabGamesList.Padding = new System.Windows.Forms.Padding(3);
			this.tabGamesList.Size = new System.Drawing.Size(1224, 738);
			this.tabGamesList.TabIndex = 1;
			this.tabGamesList.Text = "Games as a list";
			this.tabGamesList.UseVisualStyleBackColor = true;
			// 
			// splitContainerGamesList
			// 
			this.splitContainerGamesList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainerGamesList.Location = new System.Drawing.Point(3, 3);
			this.splitContainerGamesList.Name = "splitContainerGamesList";
			// 
			// splitContainerGamesList.Panel1
			// 
			this.splitContainerGamesList.Panel1.Controls.Add(this.buttonExportGames);
			this.splitContainerGamesList.Panel1.Controls.Add(this.label2);
			this.splitContainerGamesList.Panel1.Controls.Add(this.radioButtonTab);
			this.splitContainerGamesList.Panel1.Controls.Add(this.buttonClearGames);
			this.splitContainerGamesList.Panel1.Controls.Add(this.textBoxSeparator);
			this.splitContainerGamesList.Panel1.Controls.Add(this.textBoxGames);
			this.splitContainerGamesList.Panel1.Controls.Add(this.buttonImportGames);
			this.splitContainerGamesList.Panel1.Controls.Add(this.radioButtonOther);
			this.splitContainerGamesList.Panel1.Controls.Add(this.label3);
			// 
			// splitContainerGamesList.Panel2
			// 
			this.splitContainerGamesList.Panel2.Controls.Add(this.displayReportGames);
			this.splitContainerGamesList.Size = new System.Drawing.Size(1218, 732);
			this.splitContainerGamesList.SplitterDistance = 609;
			this.splitContainerGamesList.TabIndex = 27;
			// 
			// buttonExportGames
			// 
			this.buttonExportGames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonExportGames.Location = new System.Drawing.Point(531, 706);
			this.buttonExportGames.Name = "buttonExportGames";
			this.buttonExportGames.Size = new System.Drawing.Size(75, 23);
			this.buttonExportGames.TabIndex = 26;
			this.buttonExportGames.Text = "Export";
			this.buttonExportGames.UseVisualStyleBackColor = true;
			this.buttonExportGames.Click += new System.EventHandler(this.ButtonExportClick);
			// 
			// buttonClearGames
			// 
			this.buttonClearGames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClearGames.Location = new System.Drawing.Point(369, 706);
			this.buttonClearGames.Name = "buttonClearGames";
			this.buttonClearGames.Size = new System.Drawing.Size(75, 23);
			this.buttonClearGames.TabIndex = 25;
			this.buttonClearGames.Text = "Clear Fixture";
			this.buttonClearGames.UseVisualStyleBackColor = true;
			this.buttonClearGames.Click += new System.EventHandler(this.ButtonClearClick);
			// 
			// displayReportGames
			// 
			this.displayReportGames.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.displayReportGames.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayReportGames.Location = new System.Drawing.Point(0, 0);
			this.displayReportGames.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.displayReportGames.Name = "displayReportGames";
			this.displayReportGames.Report = null;
			this.displayReportGames.Size = new System.Drawing.Size(605, 732);
			this.displayReportGames.TabIndex = 26;
			// 
			// tabGamesGrid
			// 
			this.tabGamesGrid.Controls.Add(this.splitContainerGamesGrid);
			this.tabGamesGrid.Location = new System.Drawing.Point(4, 22);
			this.tabGamesGrid.Name = "tabGamesGrid";
			this.tabGamesGrid.Padding = new System.Windows.Forms.Padding(3);
			this.tabGamesGrid.Size = new System.Drawing.Size(1224, 738);
			this.tabGamesGrid.TabIndex = 2;
			this.tabGamesGrid.Text = "Games as a grid";
			this.tabGamesGrid.UseVisualStyleBackColor = true;
			// 
			// splitContainerGamesGrid
			// 
			this.splitContainerGamesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainerGamesGrid.Location = new System.Drawing.Point(3, 3);
			this.splitContainerGamesGrid.Name = "splitContainerGamesGrid";
			// 
			// splitContainerGamesGrid.Panel1
			// 
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.datePicker);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.timePicker);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.numericMinutes);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.buttonClearGrid);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.buttonImportGrid);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.textBoxGrid);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.label4);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.label5);
			this.splitContainerGamesGrid.Panel1.Controls.Add(this.label6);
			// 
			// splitContainerGamesGrid.Panel2
			// 
			this.splitContainerGamesGrid.Panel2.Controls.Add(this.displayReportGrid);
			this.splitContainerGamesGrid.Size = new System.Drawing.Size(1218, 732);
			this.splitContainerGamesGrid.SplitterDistance = 752;
			this.splitContainerGamesGrid.TabIndex = 33;
			// 
			// datePicker
			// 
			this.datePicker.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.datePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
			this.datePicker.Location = new System.Drawing.Point(85, 705);
			this.datePicker.Name = "datePicker";
			this.datePicker.Size = new System.Drawing.Size(98, 20);
			this.datePicker.TabIndex = 28;
			this.datePicker.Value = new System.DateTime(2019, 3, 1, 0, 0, 0, 0);
			// 
			// timePicker
			// 
			this.timePicker.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.timePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.timePicker.Location = new System.Drawing.Point(189, 705);
			this.timePicker.Name = "timePicker";
			this.timePicker.ShowUpDown = true;
			this.timePicker.Size = new System.Drawing.Size(98, 20);
			this.timePicker.TabIndex = 29;
			this.timePicker.Value = new System.DateTime(2000, 3, 1, 8, 0, 0, 0);
			// 
			// numericMinutes
			// 
			this.numericMinutes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.numericMinutes.Location = new System.Drawing.Point(433, 705);
			this.numericMinutes.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericMinutes.Name = "numericMinutes";
			this.numericMinutes.Size = new System.Drawing.Size(61, 20);
			this.numericMinutes.TabIndex = 30;
			this.numericMinutes.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
			// 
			// buttonClearGrid
			// 
			this.buttonClearGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClearGrid.Location = new System.Drawing.Point(595, 702);
			this.buttonClearGrid.Name = "buttonClearGrid";
			this.buttonClearGrid.Size = new System.Drawing.Size(75, 23);
			this.buttonClearGrid.TabIndex = 31;
			this.buttonClearGrid.Text = "Clear Fixture";
			this.buttonClearGrid.UseVisualStyleBackColor = true;
			this.buttonClearGrid.Click += new System.EventHandler(this.ButtonClearClick);
			// 
			// textBoxGrid
			// 
			this.textBoxGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxGrid.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxGrid.Location = new System.Drawing.Point(6, 36);
			this.textBoxGrid.Multiline = true;
			this.textBoxGrid.Name = "textBoxGrid";
			this.textBoxGrid.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxGrid.Size = new System.Drawing.Size(742, 660);
			this.textBoxGrid.TabIndex = 21;
			this.textBoxGrid.Text = "R...G.B\r\nBR...G.\r\n.BR...G\r\nG.BR...\r\n.G.BR..\r\n..G.BR.\r\n...G.BR\r\n";
			this.textBoxGrid.WordWrap = false;
			this.textBoxGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxKeyDown);
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label4.Location = new System.Drawing.Point(6, 6);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(740, 27);
			this.label4.TabIndex = 22;
			this.label4.Text = "Enter a grid of games, with each row being a team, each column being a game, and " +
    "each letter representing the colour of that team in that game: RGBYPMCOW";
			// 
			// label5
			// 
			this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label5.Location = new System.Drawing.Point(3, 707);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(99, 18);
			this.label5.TabIndex = 26;
			this.label5.Text = "First game time:";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label6.Location = new System.Drawing.Point(300, 707);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(137, 23);
			this.label6.TabIndex = 27;
			this.label6.Text = "Minutes between games:";
			// 
			// displayReportGrid
			// 
			this.displayReportGrid.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.displayReportGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayReportGrid.Location = new System.Drawing.Point(0, 0);
			this.displayReportGrid.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.displayReportGrid.Name = "displayReportGrid";
			this.displayReportGrid.Report = null;
			this.displayReportGrid.Size = new System.Drawing.Size(462, 732);
			this.displayReportGrid.TabIndex = 32;
			// 
			// tabGraphic
			// 
			this.tabGraphic.Controls.Add(this.numericSize);
			this.tabGraphic.Controls.Add(this.panelGraphic);
			this.tabGraphic.Location = new System.Drawing.Point(4, 22);
			this.tabGraphic.Name = "tabGraphic";
			this.tabGraphic.Padding = new System.Windows.Forms.Padding(3);
			this.tabGraphic.Size = new System.Drawing.Size(1224, 738);
			this.tabGraphic.TabIndex = 3;
			this.tabGraphic.Text = "Graphic";
			this.tabGraphic.UseVisualStyleBackColor = true;
			// 
			// numericSize
			// 
			this.numericSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.numericSize.Location = new System.Drawing.Point(698, 643);
			this.numericSize.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
			this.numericSize.Name = "numericSize";
			this.numericSize.Size = new System.Drawing.Size(56, 20);
			this.numericSize.TabIndex = 1;
			this.numericSize.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
			this.numericSize.ValueChanged += new System.EventHandler(this.NumericSizeValueChanged);
			// 
			// panelGraphic
			// 
			this.panelGraphic.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panelGraphic.Location = new System.Drawing.Point(6, 6);
			this.panelGraphic.Name = "panelGraphic";
			this.panelGraphic.Size = new System.Drawing.Size(754, 663);
			this.panelGraphic.TabIndex = 0;
			this.panelGraphic.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelGraphicPaint);
			this.panelGraphic.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PanelGraphicMouseClick);
			// 
			// tabFinals
			// 
			this.tabFinals.Controls.Add(this.teamsList);
			this.tabFinals.Controls.Add(this.frameFinals1);
			this.tabFinals.Location = new System.Drawing.Point(4, 22);
			this.tabFinals.Name = "tabFinals";
			this.tabFinals.Padding = new System.Windows.Forms.Padding(3);
			this.tabFinals.Size = new System.Drawing.Size(1224, 738);
			this.tabFinals.TabIndex = 7;
			this.tabFinals.Text = "Finals";
			this.tabFinals.UseVisualStyleBackColor = true;
			// 
			// teamsList
			// 
			this.teamsList.AutoScroll = true;
			this.teamsList.Dock = System.Windows.Forms.DockStyle.Left;
			this.teamsList.Location = new System.Drawing.Point(3, 3);
			this.teamsList.Name = "teamsList";
			this.teamsList.Padding = new System.Windows.Forms.Padding(9, 4, 0, 0);
			this.teamsList.Size = new System.Drawing.Size(156, 732);
			this.teamsList.TabIndex = 16;
			// 
			// frameFinals1
			// 
			this.frameFinals1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.frameFinals1.Games = null;
			this.frameFinals1.Location = new System.Drawing.Point(165, 3);
			this.frameFinals1.Name = "frameFinals1";
			this.frameFinals1.Size = new System.Drawing.Size(1056, 732);
			this.frameFinals1.TabIndex = 0;
			this.frameFinals1.Teams = null;
			// 
			// tabPyramid
			// 
			this.tabPyramid.Controls.Add(this.framePyramid1);
			this.tabPyramid.Location = new System.Drawing.Point(4, 22);
			this.tabPyramid.Name = "tabPyramid";
			this.tabPyramid.Padding = new System.Windows.Forms.Padding(3);
			this.tabPyramid.Size = new System.Drawing.Size(1224, 738);
			this.tabPyramid.TabIndex = 8;
			this.tabPyramid.Text = "Pyramid";
			this.tabPyramid.UseVisualStyleBackColor = true;
			// 
			// framePyramid1
			// 
			this.framePyramid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.framePyramid1.Holder = null;
			this.framePyramid1.Location = new System.Drawing.Point(3, 3);
			this.framePyramid1.Name = "framePyramid1";
			this.framePyramid1.Size = new System.Drawing.Size(1218, 732);
			this.framePyramid1.TabIndex = 0;
			// 
			// tabPyramidRound
			// 
			this.tabPyramidRound.Controls.Add(this.framePyramidRound1);
			this.tabPyramidRound.Location = new System.Drawing.Point(4, 22);
			this.tabPyramidRound.Name = "tabPyramidRound";
			this.tabPyramidRound.Size = new System.Drawing.Size(1224, 738);
			this.tabPyramidRound.TabIndex = 9;
			this.tabPyramidRound.Text = "Pyramid Round";
			this.tabPyramidRound.UseVisualStyleBackColor = true;
			// 
			// framePyramidRound1
			// 
			this.framePyramidRound1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.framePyramidRound1.Holder = null;
			this.framePyramidRound1.Location = new System.Drawing.Point(0, 0);
			this.framePyramidRound1.Name = "framePyramidRound1";
			this.framePyramidRound1.Size = new System.Drawing.Size(1224, 738);
			this.framePyramidRound1.TabIndex = 0;
			// 
			// FormFixture
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1256, 788);
			this.Controls.Add(this.tabControl1);
			this.Name = "FormFixture";
			this.Text = "Fixtures";
			this.Shown += new System.EventHandler(this.FormFixtureShown);
			this.ResizeBegin += new System.EventHandler(this.FormFixtureResizeBegin);
			this.ResizeEnd += new System.EventHandler(this.FormFixtureResizeEnd);
			this.tabControl1.ResumeLayout(false);
			this.tabTeams.ResumeLayout(false);
			this.tabTeams.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericRings)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.backToBackPenalty)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.minBetween)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.maxTime)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.gamesPerTeamInput)).EndInit();
			this.tabGamesList.ResumeLayout(false);
			this.splitContainerGamesList.Panel1.ResumeLayout(false);
			this.splitContainerGamesList.Panel1.PerformLayout();
			this.splitContainerGamesList.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainerGamesList)).EndInit();
			this.splitContainerGamesList.ResumeLayout(false);
			this.tabGamesGrid.ResumeLayout(false);
			this.splitContainerGamesGrid.Panel1.ResumeLayout(false);
			this.splitContainerGamesGrid.Panel1.PerformLayout();
			this.splitContainerGamesGrid.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainerGamesGrid)).EndInit();
			this.splitContainerGamesGrid.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericMinutes)).EndInit();
			this.tabGraphic.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericSize)).EndInit();
			this.tabFinals.ResumeLayout(false);
			this.tabPyramid.ResumeLayout(false);
			this.tabPyramidRound.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		private System.Windows.Forms.Panel panelGraphic;
		private System.Windows.Forms.NumericUpDown numericSize;
		private System.Windows.Forms.TabPage tabGraphic;
		private System.Windows.Forms.Button buttonClearGrid;
		private System.Windows.Forms.Button buttonClearGames;
		private System.Windows.Forms.TextBox textBoxGrid;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.DateTimePicker datePicker;
		private System.Windows.Forms.DateTimePicker timePicker;
		private System.Windows.Forms.NumericUpDown numericMinutes;
		private System.Windows.Forms.TabPage tabGamesGrid;
		private System.Windows.Forms.TabPage tabGamesList;
		private System.Windows.Forms.TabPage tabTeams;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.Button buttonImportGrid;
		private System.Windows.Forms.TextBox textBoxSeparator;
		private System.Windows.Forms.RadioButton radioButtonOther;
		private System.Windows.Forms.RadioButton radioButtonTab;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonImportGames;
		private System.Windows.Forms.Button buttonGenerate;
		private System.Windows.Forms.TextBox textBoxGames;
		private Torn5.Controls.DisplayReport displayReportGames;
		private Torn5.Controls.DisplayReport displayReportGrid;
		private System.Windows.Forms.SplitContainer splitContainerGamesList;
		private System.Windows.Forms.SplitContainer splitContainerGamesGrid;
		private System.Windows.Forms.Button buttonExportGames;
        private Torn5.Controls.DisplayReport reportTeamsList;
        private System.Windows.Forms.NumericUpDown gamesPerTeamInput;
        private System.Windows.Forms.CheckBox red;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.CheckBox referee;
        private System.Windows.Forms.CheckBox green;
        private System.Windows.Forms.CheckBox yellow;
        private System.Windows.Forms.CheckBox blue;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.NumericUpDown maxTime;
        private System.Windows.Forms.Label scoreLabel;
        private System.Windows.Forms.Button continueGenerating;
        private Torn5.Controls.PrintReport printReport1;
        private System.Windows.Forms.CheckBox outputList;
        private System.Windows.Forms.CheckBox outputGrid;
        private System.Windows.Forms.TabPage tabFinals;
        private Torn5.Controls.FrameFinals frameFinals1;
        private System.Windows.Forms.TabPage tabPyramid;
        private Torn5.Controls.FramePyramid framePyramid1;
        private System.Windows.Forms.TabPage tabPyramidRound;
        private Torn5.Controls.FramePyramidRound framePyramidRound1;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.DateTimePicker gameDateTime;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.NumericUpDown minBetween;
        private System.Windows.Forms.Panel teamsList;
        private System.Windows.Forms.Panel fixtureTeamsList;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.NumericUpDown backToBackPenalty;
		private System.Windows.Forms.NumericUpDown numericRings;
		private System.Windows.Forms.CheckBox checkRings;
	}
}
