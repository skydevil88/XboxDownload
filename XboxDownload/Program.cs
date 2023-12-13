using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace XboxDownload
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            //Application.Run(new Form1());

            using Process? instance = RunningInstance();
            if (instance == null)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (args != null && args.Length >= 1 && args[0] == "Startup")
                {
                    Form1.bAutoStartup = true;
                    using (new Form1())
                    {
                        Application.Run();
                    }
                }
                else
                {
                    Application.Run(new Form1());
                }
            }
            else
            {
                HandleRunningInstance(instance);
                Application.Exit();
            }
        }

        private static Process? RunningInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    try
                    {
                        if (current.MainModule?.FileName == process.MainModule?.FileName)
                        {
                            return process;
                        }
                    }
                    catch { }
                    break;
                }
            }
            return null;
        }

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? IpClassName, string? IpWindowName);

        private const int SW_SHOWNOMAL = 1;
        private static void HandleRunningInstance(Process instance)
        {
            IntPtr ihand = instance.MainWindowHandle;
            if (ihand == IntPtr.Zero)
                ihand = Program.FindWindow(null, "Xbox下载助手 v2");
            if (ihand == IntPtr.Zero)
            {
                MessageBox.Show("已经启动了此程序，请不要同时运行多个本程序。", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                ShowWindowAsync(ihand, SW_SHOWNOMAL);
                SetForegroundWindow(ihand);
            }
        }

        [DllImport("kernel32")]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        [Flags]
        private enum ExecutionState : uint
        {
            /// <summary>
            /// Forces the system to be in the working state by resetting the system idle timer.
            /// </summary>
            SystemRequired = 0x01,

            /// <summary>
            /// Forces the display to be on by resetting the display idle timer.
            /// </summary>
            DisplayRequired = 0x02,

            /// <summary>
            /// This value is not supported. If <see cref="UserPresent"/> is combined with other esFlags values, the call will fail and none of the specified states will be set.
            /// </summary>
            [Obsolete("This value is not supported.")]
            UserPresent = 0x04,

            /// <summary>
            /// Enables away mode. This value must be specified with <see cref="Continuous"/>.
            /// <para />
            /// Away mode should be used only by media-recording and media-distribution applications that must perform critical background processing on desktop computers while the computer appears to be sleeping.
            /// </summary>
            AwaymodeRequired = 0x40,

            /// <summary>
            /// Informs the system that the state being set should remain in effect until the next call that uses <see cref="Continuous"/> and one of the other state flags is cleared.
            /// </summary>
            Continuous = 0x80000000,
        }

        /// <summary>
        /// 包含控制屏幕关闭以及系统休眠相关的方法。
        /// </summary>
        public static class SystemSleep
        {
            /// <summary>
            /// 设置此线程此时开始一直将处于运行状态，此时计算机不应该进入睡眠状态。
            /// 此线程退出后，设置将失效。
            /// 如果需要恢复，请调用 <see cref="RestoreForCurrentThread"/> 方法。
            /// </summary>
            /// <param name="keepDisplayOn">
            /// 表示是否应该同时保持屏幕不关闭。
            /// 对于游戏、视频和演示相关的任务需要保持屏幕不关闭；而对于后台服务、下载和监控等任务则不需要。
            /// </param>
            public static void PreventForCurrentThread(bool keepDisplayOn = true)
            {
                SetThreadExecutionState(keepDisplayOn
                    ? ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired
                    : ExecutionState.Continuous | ExecutionState.SystemRequired);
            }

            /// <summary>
            /// 恢复此线程的运行状态，操作系统现在可以正常进入睡眠状态和关闭屏幕。
            /// </summary>
            public static void RestoreForCurrentThread()
            {
                SetThreadExecutionState(ExecutionState.Continuous);
            }

            /// <summary>
            /// 重置系统睡眠或者关闭屏幕的计时器，这样系统睡眠或者屏幕能够继续持续工作设定的超时时间。
            /// </summary>
            /// <param name="keepDisplayOn">
            /// 表示是否应该同时保持屏幕不关闭。
            /// 对于游戏、视频和演示相关的任务需要保持屏幕不关闭；而对于后台服务、下载和监控等任务则不需要。
            /// </param>
            public static void ResetIdle(bool keepDisplayOn = true)
            {
                SetThreadExecutionState(keepDisplayOn
                    ? ExecutionState.SystemRequired | ExecutionState.DisplayRequired
                    : ExecutionState.SystemRequired);
            }
        }

        public static class Utility
        {
            private const int LOGPIXELSX = 88;
            private const int LOGPIXELSY = 90;

            public static int DpiX
            {
                get
                {
                    if (Environment.OSVersion.Version.Major >= 6)
                        SetProcessDPIAware();
                    IntPtr hDC = GetDC(new HandleRef(null, IntPtr.Zero));
                    return GetDeviceCaps(hDC, LOGPIXELSX);
                }
            }

            public static int DpiY
            {
                get
                {
                    if (Environment.OSVersion.Version.Major >= 6)
                        SetProcessDPIAware();
                    IntPtr hDC = GetDC(new HandleRef(null, IntPtr.Zero));
                    return GetDeviceCaps(hDC, LOGPIXELSY);
                }
            }

            [DllImport("user32.dll")]
            private extern static bool SetProcessDPIAware();

            [DllImport("user32.dll")]
            private extern static IntPtr GetDC(HandleRef hWnd);

            [DllImport("gdi32.dll")]
            private extern static int GetDeviceCaps(IntPtr hdc, int nIndex);
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string? key, string? val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public static void FilesIniWrite(string section, string? key, string? value, string path)
        {
            _ = WritePrivateProfileString(section, key, value, path);
        }

        public static string FilesIniRead(string section, string key, string path)
        {
            StringBuilder sb = new(255);
            _ = GetPrivateProfileString(section, key, "", sb, 255, path);
            return sb.ToString();
        }
    }

    class DoubleBufferListView : ListView
    {
        public DoubleBufferListView()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }
}