using System;
using System.Windows.Forms;
using Torn;
using Torn5.Properties;

namespace Torn5.Forms
{
    public partial class About : Form
    {
		public string Caption { get => caption.Text; set { caption.Text = value; } }
		public string LatestVersion { get => latest.Text; set { latest.Text = value; } }

        public About()
        {
            InitializeComponent();
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(((LinkLabel)sender).Text);
			((LinkLabel)sender).LinkVisited = true;
        }

        private void About_Load(object sender, EventArgs e)
        {
            current.Text = Resources.version;
            //latest.Text = Utility.GetDeployedVersion().Result;
        }
    }
}
