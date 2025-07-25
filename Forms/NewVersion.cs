﻿using System;
using System.Windows.Forms;
using Torn;
using Torn5.Properties;

namespace Torn5.Forms
{
    public partial class NewVersion : Form
    {
        public NewVersion()
        {
            InitializeComponent();
        }

        private void NewVersion_Load(object sender, EventArgs e)
        {
            string currentVersion = Resources.version;
            current.Text = currentVersion;
            string deployedVersion = Utility.GetDeployedVersion();
            latest.Text = deployedVersion;
        }

        private void tornDownload_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://torn.lasersports.au/downloads/latest");
        }
    }
}
