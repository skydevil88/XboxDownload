using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XboxDownload
{
    class UpdateFile
    {
        private const string updateUrl = "https://github.com/skydevil88/XboxDownload/releases/download/v1/";
        private const string exeFile = "XboxDownload.exe";
        public const string pdfFile = "ProductManual.pdf";
        private static readonly string[,] proxys = {
            { "proxy", "https://ghproxy.com/" },
            { "proxy", "https://gh.api.99988866.xyz/" },
            { "proxy", "https://github.91chi.fun/" },
            { "mirror", "https://cdn.githubjs.cf/" },
            { "mirror", "https://hub.fastgit.xyz/" },
            { "direct", "" }
        };

        public static void Start(bool autoupdate, Form1 parentForm)
        {
            Properties.Settings.Default.NextUpdate = DateTime.Now.AddDays(7).Ticks;
            Properties.Settings.Default.Save();

            //清理历史遗留文件
            if (Directory.Exists(Application.StartupPath + "\\Store"))
            {
                Directory.Delete(Application.StartupPath + "\\Store", true);
            }
            string[] files = new string[]
            {
                "Hosts",
                "Domain",
                "使用说明.docx",
                "IP.assets1.xboxlive.com.txt",
                "IP.origin-a.akamaihd.net.txt",
                "IP列表(assets1.xboxlive.cn).txt",
                "IP.uplaypc-s-ubisoft.cdn.ubi.com.txt"
            };
            foreach (string file in files)
            {
                if (File.Exists(Application.StartupPath + "\\" + file))
                {
                    File.Delete(Application.StartupPath + "\\" + file);
                }
            }

            string md5 = string.Empty;
            Task[] tasks = new Task[proxys.GetLongLength(0)];
            for (int i = 0; i <= tasks.Length - 1; i++)
            {
                string updateUrl;
                switch (proxys[i, 0])
                {
                    case "proxy":
                        updateUrl = proxys[i, 1] + UpdateFile.updateUrl;
                        break;
                    case "mirror":
                        updateUrl = proxys[i, 1] + Regex.Replace(UpdateFile.updateUrl, @"^https?:\/\/[^//]+/", "");
                        break;
                    default:
                        updateUrl = UpdateFile.updateUrl;
                        break;
                }
                tasks[i] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.exeFile + ".md5", "GET", null, null, true, false, true, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(md5) && Regex.IsMatch(socketPackage.Html, @"^[A-Z0-9]{32}$"))
                    {
                        md5 = socketPackage.Html;
                        Update(autoupdate, md5, updateUrl + UpdateFile.exeFile, updateUrl + UpdateFile.pdfFile, parentForm);
                    }
                    //parentForm.SaveLog("Update", Regex.Replace(updateUrl, @"^(https?://[^/]+).*$", "$1") + "    " + socketPackage.Html, null);
                });
            }
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            if (string.IsNullOrEmpty(md5) && !autoupdate)
            {
                parentForm.Invoke(new Action(() =>
                {
                    MessageBox.Show("检查更新出错，请稍候再试。", Form1.appName + " - 软件更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
        }

        private static void Update(bool autoupdate, string md5, string exeFile, string pdfFile, Form1 parentForm)
        {
            if (!string.Equals(md5, GetPathMD5(Application.ExecutablePath)))
            {
                bool isUpdate = false;
                parentForm.Invoke(new Action(() =>
                {
                    isUpdate = MessageBox.Show("已检测到新版本，是否立即更新？", Form1.appName + " - 软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes;
                    if (!isUpdate) parentForm.tsmUpdate.Enabled = true;
                }));
                if (!isUpdate) return;

                string filename = Path.GetFileName(Application.ExecutablePath);
                Task[] tasks = new Task[2];
                tasks[0] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(exeFile, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(Application.StartupPath + "\\" + filename + ".update", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                fs.Write(socketPackage.Buffer, 0, socketPackage.Buffer.Length);
                                fs.Flush();
                                fs.Close();
                            }
                        }
                        catch { }
                    }
                });
                tasks[1] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(pdfFile, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(Application.StartupPath + "\\" + UpdateFile.pdfFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                fs.Write(socketPackage.Buffer, 0, socketPackage.Buffer.Length);
                                fs.Flush();
                                fs.Close();
                            }
                        }
                        catch { }
                    }
                });
                Array.ForEach(tasks, x => x.Start());
                Task.WaitAll(tasks);

                FileInfo fi = new FileInfo(Application.StartupPath + "\\" + filename + ".update");
                if (fi.Exists)
                {
                    if (string.Equals(md5, GetPathMD5(fi.FullName)))
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            if (Form1.bServiceFlag) parentForm.ButStart_Click(null, null);
                            parentForm.notifyIcon1.Visible = false;
                        }));
                        using (FileStream fs = File.Create(Application.StartupPath + "\\" + filename + ".md5"))
                        {
                            Byte[] bytes = new UTF8Encoding(true).GetBytes(md5);
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        using (FileStream fs = File.Create(Application.StartupPath + "\\" + ".update.cmd"))
                        {
                            Byte[] byteArray = new UTF8Encoding(true).GetBytes("cd /d %~dp0\r\nchoice /t 3 /d y /n >nul\r\ntaskkill /pid " + Process.GetCurrentProcess().Id + " /f\r\nmove \"" + filename + ".update\" \"" + filename + "\"\r\n\"" + filename + "\"\r\ndel /a/f/q .update.cmd");
                            fs.Write(byteArray, 0, byteArray.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        File.SetAttributes(Application.StartupPath + "\\" + ".update.cmd", FileAttributes.Hidden);
                        using (Process p = new Process())
                        {
                            p.StartInfo.FileName = "cmd.exe";
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.Arguments = "/c \"" + Application.StartupPath + "\\.update.cmd\"";
                            p.Start();
                        }
                        Process.GetCurrentProcess().Kill();
                    }
                    else
                    {
                        fi.Delete();
                    }
                }
                parentForm.Invoke(new Action(() =>
                {
                    MessageBox.Show("下载文件出错，请稍候再试。", Form1.appName + " - 软件更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
            else
            {
                parentForm.Invoke(new Action(() =>
                {
                    if (!autoupdate) MessageBox.Show("软件已经是最新版本。", Form1.appName + " - 软件更新", MessageBoxButtons.OK, MessageBoxIcon.None);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
        }

        internal static Boolean bDownloadEnd;
        public static void Download(string filename)
        {
            string md5 = string.Empty;
            Task[] tasks = new Task[proxys.GetLongLength(0)];
            for (int i = 0; i <= tasks.Length - 1; i++)
            {
                string updateUrl;
                switch (proxys[i, 0])
                {
                    case "proxy":
                        updateUrl = proxys[i, 1] + UpdateFile.updateUrl;
                        break;
                    case "fastgit":
                        updateUrl = proxys[i, 1] + Regex.Replace(UpdateFile.updateUrl, @"https?:\/\/[^//]+/", "");
                        break;
                    default:
                        updateUrl = UpdateFile.updateUrl;
                        break;
                }
                tasks[i] = new Task(() =>
                {
                    SocketPackage socketPackage = ClassWeb.HttpRequest(updateUrl + UpdateFile.exeFile + ".md5", "GET", null, null, true, false, true, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    if (string.IsNullOrEmpty(md5) && Regex.IsMatch(socketPackage.Html, @"^[A-Z0-9]{32}$"))
                    {
                        md5 = socketPackage.Html;
                        Download(filename, updateUrl + filename);
                    }
                });
            }
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            if (string.IsNullOrEmpty(md5)) UpdateFile.bDownloadEnd = true;
        }

        private static void Download(string filename, string url)
        {
            SocketPackage socketPackage = ClassWeb.HttpRequest(url, "GET", null, null, true, false, false, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
            if (string.IsNullOrEmpty(socketPackage.Err) && socketPackage.Buffer.Length > 0 && socketPackage.Headers.Contains(" 200 OK"))
            {
                try
                {
                    using (FileStream fs = new FileStream(Application.StartupPath + "\\" + filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        fs.Write(socketPackage.Buffer, 0, socketPackage.Buffer.Length);
                        fs.Flush();
                        fs.Close();
                    }
                }
                catch { }
            }
            UpdateFile.bDownloadEnd = true;
        }

        public static string GetPathMD5(string path)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }
    }
}