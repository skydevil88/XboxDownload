using System.Diagnostics;
using System.Reflection;

namespace XboxDownload
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();

            lbVersion.Text = string.Format(lbVersion.Text, Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = ((LinkLabel)sender).Text;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}