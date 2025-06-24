﻿namespace Torn5.Controls
{
    partial class FramePyramidRound
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
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainerTop = new System.Windows.Forms.SplitContainer();
			this.panelKey = new System.Windows.Forms.Panel();
			this.labelKeyWithdrawn = new System.Windows.Forms.Label();
			this.labelKeyPlanB = new System.Windows.Forms.Label();
			this.labelKeyRepechage = new System.Windows.Forms.Label();
			this.labelKeyRound = new System.Windows.Forms.Label();
			this.labelKey = new System.Windows.Forms.Label();
			this.numericTeamsFromPlanB = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonWithdraw = new System.Windows.Forms.Button();
			this.buttonRepechage = new System.Windows.Forms.Button();
			this.textBoxTitle = new System.Windows.Forms.TextBox();
			this.checkBoxColour = new System.Windows.Forms.CheckBox();
			this.groupTopOrBottom = new System.Windows.Forms.GroupBox();
			this.radioTakeBottom = new System.Windows.Forms.RadioButton();
			this.radioTakeTop = new System.Windows.Forms.RadioButton();
			this.groupScoreOrRank = new System.Windows.Forms.GroupBox();
			this.radioCompareRank = new System.Windows.Forms.RadioButton();
			this.radioCompareScore = new System.Windows.Forms.RadioButton();
			this.labelTeamsPerGame = new System.Windows.Forms.Label();
			this.labelNumberOfTeams = new System.Windows.Forms.Label();
			this.numericTeamsFromLastRepechage = new System.Windows.Forms.NumericUpDown();
			this.numericTeamsFromLastRound = new System.Windows.Forms.NumericUpDown();
			this.numericGames = new System.Windows.Forms.NumericUpDown();
			this.labelRoundTitle = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.labelPyramidGamesIntro = new System.Windows.Forms.Label();
			this.listViewGames = new System.Windows.Forms.ListView();
			this.columnGame = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnTeams = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnTake = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnPriority = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnSecret = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.panelGamesButtons = new System.Windows.Forms.Panel();
			this.buttonEditPyramidGames = new System.Windows.Forms.Button();
			this.buttonClearPyramidGames = new System.Windows.Forms.Button();
			this.splitContainerReports = new System.Windows.Forms.SplitContainer();
			this.displayReportTaken = new Torn5.Controls.DisplayReport();
			this.displayReportDraw = new Torn5.Controls.DisplayReport();
			this.printReportDraw = new Torn5.Controls.PrintReport();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).BeginInit();
			this.splitContainerTop.Panel1.SuspendLayout();
			this.splitContainerTop.Panel2.SuspendLayout();
			this.splitContainerTop.SuspendLayout();
			this.panelKey.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericTeamsFromPlanB)).BeginInit();
			this.groupTopOrBottom.SuspendLayout();
			this.groupScoreOrRank.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericTeamsFromLastRepechage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTeamsFromLastRound)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericGames)).BeginInit();
			this.panelGamesButtons.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerReports)).BeginInit();
			this.splitContainerReports.Panel1.SuspendLayout();
			this.splitContainerReports.Panel2.SuspendLayout();
			this.splitContainerReports.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainerTop);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainerReports);
			this.splitContainer1.Panel2.Controls.Add(this.printReportDraw);
			this.splitContainer1.Size = new System.Drawing.Size(1224, 701);
			this.splitContainer1.SplitterDistance = 254;
			this.splitContainer1.TabIndex = 22;
			// 
			// splitContainerTop
			// 
			this.splitContainerTop.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainerTop.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainerTop.Location = new System.Drawing.Point(0, 0);
			this.splitContainerTop.Name = "splitContainerTop";
			// 
			// splitContainerTop.Panel1
			// 
			this.splitContainerTop.Panel1.Controls.Add(this.panelKey);
			this.splitContainerTop.Panel1.Controls.Add(this.numericTeamsFromPlanB);
			this.splitContainerTop.Panel1.Controls.Add(this.label1);
			this.splitContainerTop.Panel1.Controls.Add(this.buttonWithdraw);
			this.splitContainerTop.Panel1.Controls.Add(this.buttonRepechage);
			this.splitContainerTop.Panel1.Controls.Add(this.textBoxTitle);
			this.splitContainerTop.Panel1.Controls.Add(this.checkBoxColour);
			this.splitContainerTop.Panel1.Controls.Add(this.groupTopOrBottom);
			this.splitContainerTop.Panel1.Controls.Add(this.groupScoreOrRank);
			this.splitContainerTop.Panel1.Controls.Add(this.labelTeamsPerGame);
			this.splitContainerTop.Panel1.Controls.Add(this.labelNumberOfTeams);
			this.splitContainerTop.Panel1.Controls.Add(this.numericTeamsFromLastRepechage);
			this.splitContainerTop.Panel1.Controls.Add(this.numericTeamsFromLastRound);
			this.splitContainerTop.Panel1.Controls.Add(this.numericGames);
			this.splitContainerTop.Panel1.Controls.Add(this.labelRoundTitle);
			this.splitContainerTop.Panel1.Controls.Add(this.label16);
			this.splitContainerTop.Panel1.Controls.Add(this.label15);
			this.splitContainerTop.Panel1.Controls.Add(this.label14);
			this.splitContainerTop.Panel1.Controls.Add(this.label13);
			this.splitContainerTop.Panel1.Controls.Add(this.label12);
			this.splitContainerTop.Panel1.Controls.Add(this.labelPyramidGamesIntro);
			// 
			// splitContainerTop.Panel2
			// 
			this.splitContainerTop.Panel2.Controls.Add(this.listViewGames);
			this.splitContainerTop.Panel2.Controls.Add(this.panelGamesButtons);
			this.splitContainerTop.Size = new System.Drawing.Size(1224, 254);
			this.splitContainerTop.SplitterDistance = 663;
			this.splitContainerTop.TabIndex = 28;
			// 
			// panelKey
			// 
			this.panelKey.Controls.Add(this.labelKeyWithdrawn);
			this.panelKey.Controls.Add(this.labelKeyPlanB);
			this.panelKey.Controls.Add(this.labelKeyRepechage);
			this.panelKey.Controls.Add(this.labelKeyRound);
			this.panelKey.Controls.Add(this.labelKey);
			this.panelKey.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelKey.Location = new System.Drawing.Point(0, 240);
			this.panelKey.Name = "panelKey";
			this.panelKey.Size = new System.Drawing.Size(663, 14);
			this.panelKey.TabIndex = 28;
			// 
			// labelKeyWithdrawn
			// 
			this.labelKeyWithdrawn.AutoSize = true;
			this.labelKeyWithdrawn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
			this.labelKeyWithdrawn.Location = new System.Drawing.Point(354, 1);
			this.labelKeyWithdrawn.Name = "labelKeyWithdrawn";
			this.labelKeyWithdrawn.Size = new System.Drawing.Size(140, 13);
			this.labelKeyWithdrawn.TabIndex = 27;
			this.labelKeyWithdrawn.Text = "Team marked as Withdrawn";
			this.labelKeyWithdrawn.Visible = false;
			// 
			// labelKeyPlanB
			// 
			this.labelKeyPlanB.AutoSize = true;
			this.labelKeyPlanB.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(208)))), ((int)(((byte)(160)))));
			this.labelKeyPlanB.Location = new System.Drawing.Point(257, 1);
			this.labelKeyPlanB.Name = "labelKeyPlanB";
			this.labelKeyPlanB.Size = new System.Drawing.Size(91, 13);
			this.labelKeyPlanB.TabIndex = 26;
			this.labelKeyPlanB.Text = "Came from Plan B";
			this.labelKeyPlanB.Visible = false;
			// 
			// labelKeyRepechage
			// 
			this.labelKeyRepechage.AutoSize = true;
			this.labelKeyRepechage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(144)))));
			this.labelKeyRepechage.Location = new System.Drawing.Point(135, 1);
			this.labelKeyRepechage.Name = "labelKeyRepechage";
			this.labelKeyRepechage.Size = new System.Drawing.Size(116, 13);
			this.labelKeyRepechage.TabIndex = 25;
			this.labelKeyRepechage.Text = "Came from Repêchage";
			this.labelKeyRepechage.Visible = false;
			// 
			// labelKeyRound
			// 
			this.labelKeyRound.AutoSize = true;
			this.labelKeyRound.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(255)))), ((int)(((byte)(160)))));
			this.labelKeyRound.Location = new System.Drawing.Point(37, 1);
			this.labelKeyRound.Name = "labelKeyRound";
			this.labelKeyRound.Size = new System.Drawing.Size(92, 13);
			this.labelKeyRound.TabIndex = 24;
			this.labelKeyRound.Text = "Came from Round";
			this.labelKeyRound.Visible = false;
			// 
			// labelKey
			// 
			this.labelKey.AutoSize = true;
			this.labelKey.Location = new System.Drawing.Point(3, 1);
			this.labelKey.Name = "labelKey";
			this.labelKey.Size = new System.Drawing.Size(28, 13);
			this.labelKey.TabIndex = 23;
			this.labelKey.Text = "Key:";
			// 
			// numericTeamsFromPlanB
			// 
			this.numericTeamsFromPlanB.Location = new System.Drawing.Point(200, 120);
			this.numericTeamsFromPlanB.Name = "numericTeamsFromPlanB";
			this.numericTeamsFromPlanB.Size = new System.Drawing.Size(64, 20);
			this.numericTeamsFromPlanB.TabIndex = 8;
			this.numericTeamsFromPlanB.ValueChanged += new System.EventHandler(this.PyramidValueChanged);
			this.numericTeamsFromPlanB.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PyramidSpinKeyUp);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 122);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(147, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Number of teams from Plan B:";
			// 
			// buttonWithdraw
			// 
			this.buttonWithdraw.Location = new System.Drawing.Point(563, 195);
			this.buttonWithdraw.Name = "buttonWithdraw";
			this.buttonWithdraw.Size = new System.Drawing.Size(73, 23);
			this.buttonWithdraw.TabIndex = 19;
			this.buttonWithdraw.Text = "Withdraw...";
			this.buttonWithdraw.UseVisualStyleBackColor = true;
			this.buttonWithdraw.Click += new System.EventHandler(this.ButtonWithdrawClick);
			// 
			// buttonRepechage
			// 
			this.buttonRepechage.Location = new System.Drawing.Point(421, 195);
			this.buttonRepechage.Name = "buttonRepechage";
			this.buttonRepechage.Size = new System.Drawing.Size(73, 23);
			this.buttonRepechage.TabIndex = 15;
			this.buttonRepechage.Text = "Repêchage ";
			this.buttonRepechage.UseVisualStyleBackColor = true;
			this.buttonRepechage.Click += new System.EventHandler(this.ButtonRepechageClick);
			// 
			// textBoxTitle
			// 
			this.textBoxTitle.Location = new System.Drawing.Point(300, 197);
			this.textBoxTitle.Name = "textBoxTitle";
			this.textBoxTitle.Size = new System.Drawing.Size(115, 20);
			this.textBoxTitle.TabIndex = 14;
			this.textBoxTitle.Text = "Next Round ";
			this.textBoxTitle.TextChanged += new System.EventHandler(this.PyramidValueChanged);
			// 
			// checkBoxColour
			// 
			this.checkBoxColour.AutoSize = true;
			this.checkBoxColour.Location = new System.Drawing.Point(501, 199);
			this.checkBoxColour.Name = "checkBoxColour";
			this.checkBoxColour.Size = new System.Drawing.Size(56, 17);
			this.checkBoxColour.TabIndex = 18;
			this.checkBoxColour.Text = "Colour";
			this.checkBoxColour.UseVisualStyleBackColor = true;
			this.checkBoxColour.CheckedChanged += new System.EventHandler(this.PyramidValueChanged);
			// 
			// groupTopOrBottom
			// 
			this.groupTopOrBottom.Controls.Add(this.radioTakeBottom);
			this.groupTopOrBottom.Controls.Add(this.radioTakeTop);
			this.groupTopOrBottom.Location = new System.Drawing.Point(300, 42);
			this.groupTopOrBottom.Name = "groupTopOrBottom";
			this.groupTopOrBottom.Size = new System.Drawing.Size(336, 67);
			this.groupTopOrBottom.TabIndex = 16;
			this.groupTopOrBottom.TabStop = false;
			this.groupTopOrBottom.Text = "Top or bottom";
			// 
			// radioTakeBottom
			// 
			this.radioTakeBottom.AutoSize = true;
			this.radioTakeBottom.Location = new System.Drawing.Point(6, 42);
			this.radioTakeBottom.Name = "radioTakeBottom";
			this.radioTakeBottom.Size = new System.Drawing.Size(314, 17);
			this.radioTakeBottom.TabIndex = 1;
			this.radioTakeBottom.Text = "Take the bottom teams from previous games (i.e. repêchage).";
			this.radioTakeBottom.UseVisualStyleBackColor = true;
			this.radioTakeBottom.CheckedChanged += new System.EventHandler(this.PyramidValueChanged);
			// 
			// radioTakeTop
			// 
			this.radioTakeTop.AutoSize = true;
			this.radioTakeTop.Checked = true;
			this.radioTakeTop.Location = new System.Drawing.Point(6, 19);
			this.radioTakeTop.Name = "radioTakeTop";
			this.radioTakeTop.Size = new System.Drawing.Size(273, 17);
			this.radioTakeTop.TabIndex = 0;
			this.radioTakeTop.TabStop = true;
			this.radioTakeTop.Text = "Take the top teams from previous games (i.e. round).";
			this.radioTakeTop.UseVisualStyleBackColor = true;
			// 
			// groupScoreOrRank
			// 
			this.groupScoreOrRank.Controls.Add(this.radioCompareRank);
			this.groupScoreOrRank.Controls.Add(this.radioCompareScore);
			this.groupScoreOrRank.Location = new System.Drawing.Point(300, 115);
			this.groupScoreOrRank.Name = "groupScoreOrRank";
			this.groupScoreOrRank.Size = new System.Drawing.Size(336, 69);
			this.groupScoreOrRank.TabIndex = 17;
			this.groupScoreOrRank.TabStop = false;
			this.groupScoreOrRank.Text = "Score or rank";
			// 
			// radioCompareRank
			// 
			this.radioCompareRank.AutoSize = true;
			this.radioCompareRank.Location = new System.Drawing.Point(6, 42);
			this.radioCompareRank.Name = "radioCompareRank";
			this.radioCompareRank.Size = new System.Drawing.Size(318, 17);
			this.radioCompareRank.TabIndex = 1;
			this.radioCompareRank.Text = "Compare teams on rank; only compare scores if ranks are tied.";
			this.radioCompareRank.UseVisualStyleBackColor = true;
			this.radioCompareRank.CheckedChanged += new System.EventHandler(this.PyramidValueChanged);
			// 
			// radioCompareScore
			// 
			this.radioCompareScore.AutoSize = true;
			this.radioCompareScore.Checked = true;
			this.radioCompareScore.Location = new System.Drawing.Point(6, 19);
			this.radioCompareScore.Name = "radioCompareScore";
			this.radioCompareScore.Size = new System.Drawing.Size(245, 17);
			this.radioCompareScore.TabIndex = 0;
			this.radioCompareScore.TabStop = true;
			this.radioCompareScore.Text = "Compare teams on victory points and/or score.";
			this.radioCompareScore.UseVisualStyleBackColor = true;
			// 
			// labelTeamsPerGame
			// 
			this.labelTeamsPerGame.AutoSize = true;
			this.labelTeamsPerGame.Location = new System.Drawing.Point(199, 174);
			this.labelTeamsPerGame.Name = "labelTeamsPerGame";
			this.labelTeamsPerGame.Size = new System.Drawing.Size(13, 13);
			this.labelTeamsPerGame.TabIndex = 12;
			this.labelTeamsPerGame.Text = "0";
			// 
			// labelNumberOfTeams
			// 
			this.labelNumberOfTeams.AutoSize = true;
			this.labelNumberOfTeams.Location = new System.Drawing.Point(199, 148);
			this.labelNumberOfTeams.Name = "labelNumberOfTeams";
			this.labelNumberOfTeams.Size = new System.Drawing.Size(13, 13);
			this.labelNumberOfTeams.TabIndex = 10;
			this.labelNumberOfTeams.Text = "0";
			// 
			// numericTeamsFromLastRepechage
			// 
			this.numericTeamsFromLastRepechage.Location = new System.Drawing.Point(200, 94);
			this.numericTeamsFromLastRepechage.Name = "numericTeamsFromLastRepechage";
			this.numericTeamsFromLastRepechage.Size = new System.Drawing.Size(64, 20);
			this.numericTeamsFromLastRepechage.TabIndex = 6;
			this.numericTeamsFromLastRepechage.ValueChanged += new System.EventHandler(this.PyramidValueChanged);
			this.numericTeamsFromLastRepechage.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PyramidSpinKeyUp);
			// 
			// numericTeamsFromLastRound
			// 
			this.numericTeamsFromLastRound.Location = new System.Drawing.Point(200, 68);
			this.numericTeamsFromLastRound.Name = "numericTeamsFromLastRound";
			this.numericTeamsFromLastRound.Size = new System.Drawing.Size(64, 20);
			this.numericTeamsFromLastRound.TabIndex = 4;
			this.numericTeamsFromLastRound.ValueChanged += new System.EventHandler(this.PyramidValueChanged);
			this.numericTeamsFromLastRound.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PyramidSpinKeyUp);
			// 
			// numericGames
			// 
			this.numericGames.Location = new System.Drawing.Point(200, 42);
			this.numericGames.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericGames.Name = "numericGames";
			this.numericGames.Size = new System.Drawing.Size(64, 20);
			this.numericGames.TabIndex = 2;
			this.numericGames.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericGames.ValueChanged += new System.EventHandler(this.PyramidValueChanged);
			this.numericGames.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PyramidSpinKeyUp);
			// 
			// labelRoundTitle
			// 
			this.labelRoundTitle.AutoSize = true;
			this.labelRoundTitle.Location = new System.Drawing.Point(233, 200);
			this.labelRoundTitle.Name = "labelRoundTitle";
			this.labelRoundTitle.Size = new System.Drawing.Size(61, 13);
			this.labelRoundTitle.TabIndex = 13;
			this.labelRoundTitle.Text = "Round title:";
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(3, 174);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(137, 13);
			this.label16.TabIndex = 11;
			this.label16.Text = "Number of teams per game:";
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(3, 148);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(154, 13);
			this.label15.TabIndex = 9;
			this.label15.Text = "Number of teams in next round:";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(3, 96);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(186, 13);
			this.label14.TabIndex = 5;
			this.label14.Text = "Number of teams from last repêchage:";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(3, 70);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(162, 13);
			this.label13.TabIndex = 3;
			this.label13.Text = "Number of teams from last round:";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(3, 44);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(157, 13);
			this.label12.TabIndex = 1;
			this.label12.Text = "Number of games in next round:";
			// 
			// labelPyramidGamesIntro
			// 
			this.labelPyramidGamesIntro.AutoSize = true;
			this.labelPyramidGamesIntro.Location = new System.Drawing.Point(3, 8);
			this.labelPyramidGamesIntro.Name = "labelPyramidGamesIntro";
			this.labelPyramidGamesIntro.Size = new System.Drawing.Size(581, 26);
			this.labelPyramidGamesIntro.TabIndex = 0;
			this.labelPyramidGamesIntro.Text = "Pyramids can be used in large solos, doubles, etc. tournaments.\r\nSelect the games" +
    " from the previous round to draw teams from, and decide how many teams will proc" +
    "eed to the next round.";
			// 
			// listViewGames
			// 
			this.listViewGames.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnGame,
            this.columnDescription,
            this.columnTeams,
            this.columnTake,
            this.columnPriority,
            this.columnSecret});
			this.listViewGames.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listViewGames.FullRowSelect = true;
			this.listViewGames.HideSelection = false;
			this.listViewGames.Location = new System.Drawing.Point(0, 0);
			this.listViewGames.Name = "listViewGames";
			this.listViewGames.Size = new System.Drawing.Size(557, 219);
			this.listViewGames.TabIndex = 20;
			this.listViewGames.UseCompatibleStateImageBehavior = false;
			this.listViewGames.View = System.Windows.Forms.View.Details;
			this.listViewGames.DoubleClick += new System.EventHandler(this.ListViewGamesDoubleClick);
			// 
			// columnGame
			// 
			this.columnGame.Text = "Game Time";
			this.columnGame.Width = 100;
			// 
			// columnDescription
			// 
			this.columnDescription.Text = "Description";
			this.columnDescription.Width = 80;
			// 
			// columnTeams
			// 
			this.columnTeams.Text = "# teams";
			this.columnTeams.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// columnTake
			// 
			this.columnTake.Text = "Teams to take";
			this.columnTake.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.columnTake.Width = 80;
			// 
			// columnPriority
			// 
			this.columnPriority.Text = "Priority";
			this.columnPriority.Width = 70;
			// 
			// columnSecret
			// 
			this.columnSecret.Text = "Secret?";
			this.columnSecret.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.columnSecret.Width = 50;
			// 
			// panelGamesButtons
			// 
			this.panelGamesButtons.Controls.Add(this.buttonEditPyramidGames);
			this.panelGamesButtons.Controls.Add(this.buttonClearPyramidGames);
			this.panelGamesButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelGamesButtons.Location = new System.Drawing.Point(0, 219);
			this.panelGamesButtons.Name = "panelGamesButtons";
			this.panelGamesButtons.Size = new System.Drawing.Size(557, 35);
			this.panelGamesButtons.TabIndex = 0;
			// 
			// buttonEditPyramidGames
			// 
			this.buttonEditPyramidGames.Location = new System.Drawing.Point(3, 6);
			this.buttonEditPyramidGames.Name = "buttonEditPyramidGames";
			this.buttonEditPyramidGames.Size = new System.Drawing.Size(88, 23);
			this.buttonEditPyramidGames.TabIndex = 21;
			this.buttonEditPyramidGames.Text = "Edit Game(s)";
			this.buttonEditPyramidGames.UseVisualStyleBackColor = true;
			this.buttonEditPyramidGames.Click += new System.EventHandler(this.ButtonEditPyramidGamesClick);
			// 
			// buttonClearPyramidGames
			// 
			this.buttonClearPyramidGames.Location = new System.Drawing.Point(97, 6);
			this.buttonClearPyramidGames.Name = "buttonClearPyramidGames";
			this.buttonClearPyramidGames.Size = new System.Drawing.Size(88, 23);
			this.buttonClearPyramidGames.TabIndex = 22;
			this.buttonClearPyramidGames.Text = "Clear Game(s)";
			this.buttonClearPyramidGames.UseVisualStyleBackColor = true;
			this.buttonClearPyramidGames.Click += new System.EventHandler(this.ButtonClearPyramidGames);
			// 
			// splitContainerReports
			// 
			this.splitContainerReports.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainerReports.Location = new System.Drawing.Point(0, 0);
			this.splitContainerReports.Name = "splitContainerReports";
			// 
			// splitContainerReports.Panel1
			// 
			this.splitContainerReports.Panel1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.splitContainerReports.Panel1.Controls.Add(this.displayReportTaken);
			this.splitContainerReports.Panel1.Padding = new System.Windows.Forms.Padding(0, 1, 1, 0);
			// 
			// splitContainerReports.Panel2
			// 
			this.splitContainerReports.Panel2.BackColor = System.Drawing.SystemColors.ControlDark;
			this.splitContainerReports.Panel2.Controls.Add(this.displayReportDraw);
			this.splitContainerReports.Panel2.Padding = new System.Windows.Forms.Padding(1, 1, 0, 0);
			this.splitContainerReports.Size = new System.Drawing.Size(1160, 443);
			this.splitContainerReports.SplitterDistance = 382;
			this.splitContainerReports.TabIndex = 14;
			// 
			// displayReportTaken
			// 
			this.displayReportTaken.BackColor = System.Drawing.SystemColors.Control;
			this.displayReportTaken.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.displayReportTaken.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayReportTaken.Location = new System.Drawing.Point(0, 1);
			this.displayReportTaken.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.displayReportTaken.Name = "displayReportTaken";
			this.displayReportTaken.Report = null;
			this.displayReportTaken.Size = new System.Drawing.Size(381, 442);
			this.displayReportTaken.TabIndex = 0;
			// 
			// displayReportDraw
			// 
			this.displayReportDraw.BackColor = System.Drawing.SystemColors.Control;
			this.displayReportDraw.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.displayReportDraw.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayReportDraw.Location = new System.Drawing.Point(1, 1);
			this.displayReportDraw.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.displayReportDraw.Name = "displayReportDraw";
			this.displayReportDraw.Report = null;
			this.displayReportDraw.Size = new System.Drawing.Size(773, 442);
			this.displayReportDraw.TabIndex = 0;
			// 
			// printReportDraw
			// 
			this.printReportDraw.DisplayReport = this.displayReportDraw;
			this.printReportDraw.Dock = System.Windows.Forms.DockStyle.Right;
			this.printReportDraw.FileName = null;
			this.printReportDraw.Location = new System.Drawing.Point(1160, 0);
			this.printReportDraw.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.printReportDraw.Name = "printReportDraw";
			this.printReportDraw.Size = new System.Drawing.Size(64, 443);
			this.printReportDraw.TabIndex = 15;
			// 
			// FramePyramidRound
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Name = "FramePyramidRound";
			this.Size = new System.Drawing.Size(1224, 701);
			this.Enter += new System.EventHandler(this.FramePyramidRoundEnter);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainerTop.Panel1.ResumeLayout(false);
			this.splitContainerTop.Panel1.PerformLayout();
			this.splitContainerTop.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).EndInit();
			this.splitContainerTop.ResumeLayout(false);
			this.panelKey.ResumeLayout(false);
			this.panelKey.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericTeamsFromPlanB)).EndInit();
			this.groupTopOrBottom.ResumeLayout(false);
			this.groupTopOrBottom.PerformLayout();
			this.groupScoreOrRank.ResumeLayout(false);
			this.groupScoreOrRank.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericTeamsFromLastRepechage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericTeamsFromLastRound)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericGames)).EndInit();
			this.panelGamesButtons.ResumeLayout(false);
			this.splitContainerReports.Panel1.ResumeLayout(false);
			this.splitContainerReports.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainerReports)).EndInit();
			this.splitContainerReports.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button buttonRepechage;
        private System.Windows.Forms.Button buttonClearPyramidGames;
        private System.Windows.Forms.Button buttonEditPyramidGames;
        private System.Windows.Forms.TextBox textBoxTitle;
        private System.Windows.Forms.CheckBox checkBoxColour;
        private System.Windows.Forms.GroupBox groupTopOrBottom;
        private System.Windows.Forms.RadioButton radioTakeBottom;
        private System.Windows.Forms.RadioButton radioTakeTop;
        private System.Windows.Forms.GroupBox groupScoreOrRank;
        private System.Windows.Forms.RadioButton radioCompareRank;
        private System.Windows.Forms.RadioButton radioCompareScore;
        private System.Windows.Forms.Label labelTeamsPerGame;
        private System.Windows.Forms.Label labelNumberOfTeams;
        private System.Windows.Forms.NumericUpDown numericTeamsFromLastRepechage;
        private System.Windows.Forms.NumericUpDown numericTeamsFromLastRound;
        private System.Windows.Forms.NumericUpDown numericGames;
        private System.Windows.Forms.Label labelRoundTitle;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label labelPyramidGamesIntro;
        private System.Windows.Forms.ListView listViewGames;
        private System.Windows.Forms.ColumnHeader columnGame;
        private System.Windows.Forms.ColumnHeader columnDescription;
        private System.Windows.Forms.ColumnHeader columnTeams;
        private System.Windows.Forms.ColumnHeader columnTake;
        private System.Windows.Forms.ColumnHeader columnPriority;
        private System.Windows.Forms.ColumnHeader columnSecret;
        private System.Windows.Forms.SplitContainer splitContainerReports;
        private DisplayReport displayReportTaken;
        private DisplayReport displayReportDraw;
        private PrintReport printReportDraw;
        private System.Windows.Forms.Button buttonWithdraw;
		private System.Windows.Forms.Label labelKeyRound;
		private System.Windows.Forms.Label labelKey;
		private System.Windows.Forms.Label labelKeyRepechage;
		private System.Windows.Forms.Label labelKeyPlanB;
		private System.Windows.Forms.Label labelKeyWithdrawn;
		private System.Windows.Forms.NumericUpDown numericTeamsFromPlanB;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.SplitContainer splitContainerTop;
		private System.Windows.Forms.Panel panelGamesButtons;
		private System.Windows.Forms.Panel panelKey;
	}
}
