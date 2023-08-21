namespace XboxDownload
{
    public partial class FormIpLocation : Form
    {
        public string key = string.Empty;
        public FormIpLocation()
        {
            InitializeComponent();

            textBox1.Text = Properties.Settings.Default.IpLocation;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            key = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;
            Properties.Settings.Default.IpLocation = key;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
