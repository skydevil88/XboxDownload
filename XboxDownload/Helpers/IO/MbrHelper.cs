using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XboxDownload.Helpers.IO;

public static class MbrHelper
{
    public const string Mbr = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123456780000";

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint FileBegin = 0;
    private const int FileShareRead = 0x00000001;
    private const int FileShareWrite = 0x00000002;

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateFile(string fileName, uint accessFlag, uint shareMode, IntPtr security, uint createFlag, uint attributeFlag, IntPtr tempFile);

    [DllImport("Kernel32.dll")]
    static extern bool ReadFile(IntPtr handle, [Out] byte[] buffer, uint bufferLength, ref uint length, IntPtr overLapped);

    [DllImport("kernel32.dll")]
    static extern bool WriteFile(IntPtr handle, byte[] buffer, int bufferLength, ref int length, IntPtr overLapped);

    [DllImport("Kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("Kernel32.dll")]
    static extern uint SetFilePointer(IntPtr handle, int offset, IntPtr distance, uint flag);

    public static byte[] ReadMbr(string deviceId)
    {
        var diskHandle = CreateFile(deviceId, GenericRead, FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (diskHandle == IntPtr.Zero || diskHandle.ToInt64() == -1)
            return [];

        try
        {
            var buffer = new byte[512];
            uint length = 0;
            var pos = SetFilePointer(diskHandle, 0, IntPtr.Zero, FileBegin);
            if (pos == 0xFFFFFFFF || !ReadFile(diskHandle, buffer, (uint)buffer.Length, ref length, IntPtr.Zero) || length != buffer.Length)
                return [];

            return buffer;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading MBR: {ex.Message}");
            return [];
        }
        finally
        {
            CloseHandle(diskHandle);
        }
    }

    public static bool WriteMbr(string deviceId, byte[] mbr)
    {
        var diskHandle = CreateFile(deviceId, GenericWrite, FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (diskHandle == IntPtr.Zero || diskHandle.ToInt64() == -1)
            return false;

        try
        {
            var length = 0;
            var pos = SetFilePointer(diskHandle, 0, IntPtr.Zero, FileBegin);
            if (pos == 0xFFFFFFFF)
                return false;

            return WriteFile(diskHandle, mbr, mbr.Length, ref length, IntPtr.Zero) && length == mbr.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing MBR: {ex.Message}");
            return false;
        }
        finally
        {
            CloseHandle(diskHandle);
        }
    }

    public static byte[] HexToByte(string byteStr)
    {
        byteStr = byteStr.ToUpperInvariant().Replace(" ", "");
        var len = byteStr.Length / 2;
        var data = new byte[len];
        for (var i = 0; i < len; i++)
        {
            data[i] = Convert.ToByte(byteStr.Substring(i * 2, 2), 16);
        }
        return data;
    }

    public static string ByteToHex(byte[] bytes)
    {
        if (bytes.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var item in bytes)
        {
            sb.Append(item.ToString("X2"));
        }
        return sb.ToString();
    }
}