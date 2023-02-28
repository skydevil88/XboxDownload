using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    internal class UpdateFile
    {
        public const string homePage = "https://xbox.skydevil.xyz";
        public const string updateUrl = "https://github.com/skydevil88/XboxDownload/releases/";
        public const string filePath = "download/v1/";
        private const string testFile = "XboxDownload.exe.md5";
        public const string pdfFile = "ProductManual.pdf";
        public const string dataFile = "XboxGame.json";
        private static readonly string[,] proxys = {
            { "proxy", "https://ghproxy.com/" },
            { "proxy", "https://gh.api.99988866.xyz/" },
            { "proxy", "https://github.91chi.fun/" },
            { "proxy", "https://ghps.cc/" },
            { "proxy", "https://proxy.zyun.vip/" },
            //{ "mirror", "https://cdn.githubjs.cf/" },   //失效
            //{ "mirror", "https://hub.fastgit.xyz/" },   //失效
            { "direct", "" }
        };

        public static void Start(bool autoupdate, Form1 parentForm)
        {
            Properties.Settings.Default.NextUpdate = DateTime.Now.AddDays(7).Ticks;
            Properties.Settings.Default.Save();

            string md5 = string.Empty;
            Task[] tasks = new Task[proxys.GetLongLength(0)];
            for (int i = 0; i <= tasks.Length - 1; i++)
            {
                string updateUrl = proxys[i, 0] switch
                {
                    "proxy" => proxys[i, 1] + UpdateFile.updateUrl,
                    "mirror" => proxys[i, 1] + Regex.Replace(UpdateFile.updateUrl, @"^https?://[^/]+/", ""),
                    _ => UpdateFile.updateUrl
                };
                tasks[i] = new Task(() =>
                {
                    string html = ClassWeb.HttpResponseContent(updateUrl + UpdateFile.filePath + UpdateFile.testFile, "GET", null, null, null, 6000);
                    if (string.IsNullOrEmpty(md5) && Regex.IsMatch(html, @"^[A-Z0-9]{32}$"))
                    {
                        md5 = html;
                        Update(autoupdate, updateUrl, parentForm);
                    }
                });
            }
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            if (string.IsNullOrEmpty(md5) && !autoupdate)
            {
                parentForm.Invoke(new Action(() =>
                {
                    MessageBox.Show("检查更新出错，请稍候再试。", "软件更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    parentForm.tsmUpdate.Enabled = true;
                }));
            }
        }

        private static void Update(bool autoupdate, string updateUrl, Form1 parentForm)
        {
            string? url = null;
            using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(updateUrl+ "latest", "HEAD");
            if (response != null && response.IsSuccessStatusCode)
            {
                url = response.RequestMessage?.RequestUri?.ToString();
            }
            if (url != null)
            {
                bool isUpdate = false;
                Match result = Regex.Match(url, @"(?<version>\d+(\.\d+){2,3})$");
                if (result.Success)
                {
                    Version version1 = new(result.Groups["version"].Value);
                    Version version2 = new((Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version) ?? string.Empty);
                    if (version1 > version2 && version1.Major == 2)
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            isUpdate = MessageBox.Show("已检测到新版本，是否立即更新？", "软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes;
                            if (!isUpdate) parentForm.tsmUpdate.Enabled = true;
                        }));
                        if (!isUpdate) return;
                    }
                    else
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            if (!autoupdate) MessageBox.Show("软件已经是最新版本。", "软件更新", MessageBoxButtons.OK, MessageBoxIcon.None);
                            parentForm.tsmUpdate.Enabled = true;
                        }));
                        return;
                    }
                }
                if (isUpdate)
                {
                    string download = (url.Replace("tag", "download") + "/XboxDownload.zip");
                    using HttpResponseMessage? response2 = ClassWeb.HttpResponseMessage(download, "GET", null, null, null, 180000);
                    if (response2 != null && response2.IsSuccessStatusCode)
                    {
                        if (!Directory.Exists(Form1.resourcePath))
                            Directory.CreateDirectory(Form1.resourcePath);
                        byte[] buffer = response2.Content.ReadAsByteArrayAsync().Result;
                        if (buffer.Length > 0)
                        {
                            using FileStream fs = new(Form1.resourcePath + "\\" + "XboxDownload.zip", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                            fs.Write(buffer, 0, buffer.Length);
                            fs.Flush();
                            fs.Close();
                            string tempDir = Form1.resourcePath + @"\Temp";
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                            ZipFile.ExtractToDirectory(Form1.resourcePath + @"\XboxDownload.zip", tempDir, Encoding.GetEncoding("GBK"), true);
                            foreach (DirectoryInfo di in new DirectoryInfo(tempDir).GetDirectories())
                            {
                                if (File.Exists(di.FullName + @"\XboxDownload.exe"))
                                {
                                    parentForm.Invoke(new Action(() =>
                                    {
                                        if (Form1.bServiceFlag) parentForm.ButStart_Click(null, null);
                                        parentForm.notifyIcon1.Visible = false;
                                    }));
                                    string cmd = "choice /t 3 /d y /n >nul\r\nxcopy \"" + di.FullName + "\" \"" + Path.GetDirectoryName(Application.ExecutablePath) + "\" /s /e /y\r\ndel /a/f/q " + Form1.resourcePath + "\\XboxDownload.zip\r\n\"" + Application.ExecutablePath + "\"\r\nrd /s/q " + tempDir;
                                    File.WriteAllText(tempDir + "\\" + ".update.cmd", cmd, Encoding.GetEncoding(0));
                                    using (Process p = new())
                                    {
                                        p.StartInfo.FileName = "cmd.exe";
                                        p.StartInfo.UseShellExecute = false;
                                        p.StartInfo.CreateNoWindow = true;
                                        p.StartInfo.Arguments = "/c \"" + tempDir + "\\.update.cmd\"";
                                        p.Start();
                                    }
                                    Process.GetCurrentProcess().Kill();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            parentForm.Invoke(new Action(() =>
            {
                if (!autoupdate) MessageBox.Show("下载文件出错，请稍候再试。", "软件更新", MessageBoxButtons.OK, MessageBoxIcon.Error);
                parentForm.tsmUpdate.Enabled = true;
            }));
        }

        public static async Task Download(FileInfo fi)
        {
            string md5 = string.Empty;
            Task[] tasks = new Task[proxys.GetLongLength(0)];
            for (int i = 0; i <= tasks.Length - 1; i++)
            {
                string updateUrl = proxys[i, 0] switch
                {
                    "proxy" => proxys[i, 1] + UpdateFile.updateUrl,
                    "mirror" => proxys[i, 1] + Regex.Replace(UpdateFile.updateUrl, @"^https?://[^/]+/", ""),
                    _ => UpdateFile.updateUrl
                };
                tasks[i] = new Task(() =>
                {
                    string html = ClassWeb.HttpResponseContent(updateUrl + UpdateFile.filePath + UpdateFile.testFile, "GET", null, null, null, 6000);
                    if (string.IsNullOrEmpty(md5) && Regex.IsMatch(html, @"^[A-Z0-9]{32}$"))
                    {
                        md5 = html;
                        using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(updateUrl + UpdateFile.filePath + fi.Name, "GET", null, null, null, 60000);
                        if (response != null && response.IsSuccessStatusCode)
                        {
                            /*
                            using Stream stream = response.Content.ReadAsStreamAsync().Result;
                            if (stream.Length > 0)
                            {
                                if (fi.DirectoryName != null && !Directory.Exists(fi.DirectoryName))
                                    Directory.CreateDirectory(fi.DirectoryName);
                                using FileStream fileStream = fi.Create();
                                stream.CopyToAsync(fileStream);
                                fi.Refresh();
                            }
                            */
                            byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                            if (buffer.Length > 0)
                            {
                                if (fi.DirectoryName != null && !Directory.Exists(fi.DirectoryName))
                                    Directory.CreateDirectory(fi.DirectoryName);
                                using FileStream fs = new(fi.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                                fs.Write(buffer, 0, buffer.Length);
                                fs.Flush();
                                fs.Close();
                                fi.Refresh();
                            }
                        }
                    }
                });
            }
            Array.ForEach(tasks, x => x.Start());
            await Task.WhenAll(tasks);
        }
    }
}
