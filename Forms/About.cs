using System;
using System.Windows.Forms;
using Torn;
using Torn5.Properties;

namespace Torn5.Forms
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.dougburbidge.com/Apps/");
        }

        private void github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/MrMeeseeks200/Torn5/");
        }

        private void tornDownload_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://torn.lasersports.au/downloads/latest");
        }

        private void About_Load(object sender, EventArgs e)
        {

            string currentVersion = Resources.version;
            current.Text = currentVersion;
            string deployedVersion = Utility.GetDeployedVersion();
            latest.Text = deployedVersion;
        }
    }
}
