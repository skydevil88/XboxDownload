using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using XboxDownload.Helpers.IO;

namespace XboxDownload.Helpers.Security;

public static class MsalTokenCacheHelper
{
    private static readonly Lock FileLock = new();

    private static readonly string CachePath =
        PathHelper.GetLocalFilePath("msal_cache.bin");

    private static readonly string KeyPath =
        PathHelper.GetLocalFilePath("msal_cache.key");

    // ================= Public =================

    public static void EnableSerialization(
        ITokenCache tokenCache)
    {

        tokenCache.SetBeforeAccess(OnBeforeAccess);
        tokenCache.SetAfterAccess(OnAfterAccess);
    }

    // ================= Cache Read =================

    private static void OnBeforeAccess(TokenCacheNotificationArgs args)
    {
        lock (FileLock)
        {
            if (!File.Exists(CachePath))
                return;

            try
            {
                var data = File.ReadAllBytes(CachePath);

                var plain = Decrypt(data);

                args.TokenCache.DeserializeMsalV3(plain);
            }
            catch
            {
                SafeDelete(CachePath);
                SafeDelete(KeyPath);
            }
        }
    }

    // ================= Cache Write =================

    private static void OnAfterAccess(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged)
            return;

        lock (FileLock)
        {
            var plain = args.TokenCache.SerializeMsalV3();

            var data = Encrypt(plain);

            File.WriteAllBytes(CachePath, data);

            if (!OperatingSystem.IsWindows())
                _ = PathHelper.FixOwnershipAsync(CachePath);
        }
    }

    // ================= Encryption =================

    private static byte[] Encrypt(byte[] data)
    {
        var key = DeriveKey(GetOrCreateKey());
        return EncryptAesGcm(data, key);
    }

    private static byte[] Decrypt(byte[] data)
    {
        var key = DeriveKey(GetOrCreateKey());
        return DecryptAesGcm(data, key);
    }

    // ================= AES-GCM =================

    private static byte[] EncryptAesGcm(byte[] plain, byte[] key)
    {
        using var aes = new AesGcm(key, tagSizeInBytes: 16);

        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plain, cipher, tag);

        return nonce
            .Concat(tag)
            .Concat(cipher)
            .ToArray();
    }

    private static byte[] DecryptAesGcm(byte[] data, byte[] key)
    {
        if (data.Length < 28)
            throw new CryptographicException("Invalid encrypted data.");

        var nonce = data[..12];
        var tag = data[12..28];
        var cipher = data[28..];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        var plain = new byte[cipher.Length];

        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    // ================= Key =================

    private static byte[] GetOrCreateKey()
    {
        if (File.Exists(KeyPath))
            return File.ReadAllBytes(KeyPath);

        var key = RandomNumberGenerator.GetBytes(32); // 256-bit
        File.WriteAllBytes(KeyPath, key);

        if (!OperatingSystem.IsWindows())
            _ = PathHelper.FixOwnershipAsync(CachePath);

        return key;
    }

    // ================= Utils =================

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }

    // ================= Key Derivation =================

    private static byte[] DeriveKey(byte[] masterKey)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            masterKey,
            GetMachineSalt(),
            100_000,
            HashAlgorithmName.SHA256,
            32);
    }

    private static byte[] GetMachineSalt()
    {
        var raw = string.Join("|",
            Environment.MachineName,
            Environment.ProcessorCount,
            GetTotalMemoryMb(),
            RuntimeInformation.ProcessArchitecture,
            OperatingSystem.IsWindows() ? "win" :
            OperatingSystem.IsMacOS() ? "mac" :
            OperatingSystem.IsLinux() ? "linux" : "unknown");

        return SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    private static long GetTotalMemoryMb()
    {
        try
        {
            var bytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (bytes <= 0)
                return 0;

            return bytes / (1024 * 1024); // MB
        }
        catch
        {
            return 0;
        }
    }
}
