using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormNSBH : Form
    {
        public FormNSBH()
        {
            InitializeComponent();
            tbNSHomepage.Text = Properties.Settings.Default.NSHomepage;
        }

        private void ButSubmit_Click(object sender, EventArgs e)
        {
            string homepage = tbNSHomepage.Text.Trim();
            if (!Regex.IsMatch(homepage, @"https?://")) homepage = "https://" + homepage;
            Properties.Settings.Default.NSHomepage = homepage;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
