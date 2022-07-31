using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XboxDownload
{
    class ClassMbr
    {
        public const string MBR = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123456780000";

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint OPEN_EXISTING = 3;
        const uint FILE_BEGIN = 0;
        const int FILE_SHARE_READ = 0x00000001;
        const int FILE_SHARE_WRITE = 0x00000002;

        [DllImport("Kernel32.dll")]
        extern static IntPtr CreateFile(string fileName, uint accessFlag, uint shareMode, IntPtr security, uint createFlag, uint attributeFlag, IntPtr tempfile);

        [DllImport("Kernel32.dll")]
        extern static bool ReadFile(IntPtr handle, [Out] byte[] buffer, uint bufferLength, ref uint length, IntPtr overLapped);

        [DllImport("kernel32.dll")]
        extern static bool WriteFile(IntPtr handle, byte[] buffer, int bufferLength, ref int length, IntPtr overLapped);

        [DllImport("Kernel32.dll")]
        extern static bool CloseHandle(IntPtr handle);

        [DllImport("Kernel32.dll")]
        extern static uint SetFilePointer(IntPtr handle, int offset, IntPtr distance, uint flag);

        public static byte[] ReadMBR(string sDeviceID)
        {
            IntPtr DiskHandle = CreateFile(sDeviceID, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            byte[] buffer = new byte[512];
            uint length = 0;
            SetFilePointer(DiskHandle, 0, IntPtr.Zero, FILE_BEGIN);
            ReadFile(DiskHandle, buffer, 512, ref length, IntPtr.Zero);
            CloseHandle(DiskHandle);
            return buffer;
        }

        public static bool WriteMBR(string sDeviceID, byte[] MBR)
        {
            IntPtr DiskHandle = CreateFile(sDeviceID, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, (IntPtr)0, OPEN_EXISTING, 0, (IntPtr)0);
            int length = 0;
            SetFilePointer(DiskHandle, 0, IntPtr.Zero, FILE_BEGIN);
            bool b = WriteFile(DiskHandle, MBR, 512, ref length, (IntPtr)0);
            CloseHandle(DiskHandle);
            return b;
        }

        public static byte[] HexToByte(string byteStr)
        {
            byteStr = byteStr.ToUpper().Replace(" ", "");
            int len = byteStr.Length / 2;
            byte[] data = new byte[len];
            for (int i = 0; i < len; i++)
            {
                data[i] = Convert.ToByte(byteStr.Substring(i * 2, 2), 16);
            }
            return data;
        }

        public static string ByteToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            if (bytes != null || bytes.Length > 0)
            {
                foreach (var item in bytes)
                {
                    sb.Append(item.ToString("X2"));
                }
            }
            return sb.ToString();
        }

        public static string ConvertBytes(ulong len)
        {
            double leng = Convert.ToDouble(len);
            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
            int order = 0;
            while (leng >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                leng /= 1024;
            }
            return String.Format("{0:#.00} {1}", leng, sizes[order]);
        }
    }
}
