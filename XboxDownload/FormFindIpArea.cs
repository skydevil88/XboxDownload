using System;
using System.Windows.Forms;

namespace XboxDownload
{
    public partial class FormFindIpArea : Form
    {
        public string key = string.Empty;
        public FormFindIpArea()
        {
            InitializeComponent();

            textBox1.Text = Properties.Settings.Default.IpArea;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            key = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;
            Properties.Settings.Default.IpArea = key;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
