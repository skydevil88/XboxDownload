using System.Diagnostics;
using System.Text;

namespace XboxDownload
{
    public partial class FormStartup : Form
    {
        public FormStartup()
        {
            InitializeComponent();
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\Tasks\XboxDownload") || File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\Tasks\Xbox下载助手"))
            {
                cbStartup.Checked = true;
            }
        }

        private void ButSubmit_Click(object sender, EventArgs e)
        {
            butSubmit.Enabled = false;

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\Tasks\Xbox下载助手"))
            {
                string cmdPath = Path.GetTempPath() + "XboxDownloadTask.cmd";
                string cmd = "chcp 65001\r\nschtasks /delete /tn \"XboxDownload\" /f\r\nschtasks /delete /tn \"Xbox下载助手\" /f";
                File.WriteAllText(cmdPath, cmd);
                using (Process p = new())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments = "/c \"" + cmdPath + "\"";
                    p.Start();
                    p.WaitForExit();
                }
                File.Delete(cmdPath);
            }

            if (cbStartup.Checked)
            {
                string taskPath = Path.GetTempPath() + "XboxDownloadTask.xml";
                string xml = String.Format(Properties.Resource.Task, Application.ExecutablePath);
                File.WriteAllText(taskPath, xml, Encoding.GetEncoding("UTF-16"));
                string cmd = "chcp 65001\r\nschtasks /create /xml \"" + taskPath + "\" /tn \"XboxDownload\" /f";
                using Process p = new();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
                File.Delete(taskPath);
            }
            else
            {
                string cmd = "chcp 65001\r\nschtasks /delete /tn \"XboxDownload\" /f\r\nschtasks /delete /tn \"XboxDownload\" /f";
                using Process p = new();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
            }
            this.Close();
        }
    }
}
