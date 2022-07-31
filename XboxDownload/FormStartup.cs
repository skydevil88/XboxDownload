using System;
using System.IO;
using System.Windows.Forms;

namespace XboxDownload
{
    public partial class FormStartup : Form
    {
        public FormStartup()
        {
            InitializeComponent();

            if (File.Exists(Application.StartupPath + "\\Interop.TaskScheduler.dll"))
                cbStartup.Checked = SchTaskExt.IsExists(Form1.appName);
            else
                cbStartup.Enabled = butSubmit.Enabled = false;
        }

        private void ButSubmit_Click(object sender, EventArgs e)
        {
            if (SchTaskExt.IsExists(Form1.appName))
                SchTaskExt.DeleteTask(Form1.appName);
            if (cbStartup.Checked)
                SchTaskExt.CreateRestartTask(Form1.appName);
            this.Close();
        }
    }
}
