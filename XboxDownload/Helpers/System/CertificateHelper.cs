using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using XboxDownload.Helpers.IO;

namespace XboxDownload.Helpers.System;

public static class CertificateHelper
{
    public static readonly string RootPfx = PathHelper.GetLocalFilePath($"{nameof(XboxDownload)}.pfx");
    public static readonly string RootCrt = PathHelper.GetLocalFilePath($"{nameof(XboxDownload)}.crt");

    public static async Task CreateRootCertificate(bool force = false)
    {
        if (!force && File.Exists(RootPfx) && File.Exists(RootCrt))
            return; // Root CA already exists, skip unless force = true

        if (!OperatingSystem.IsWindows() && !Program.UnixUserIsRoot())
            return;

        using var rsa = RSA.Create(4096);
        var caReq = new CertificateRequest($"CN={nameof(XboxDownload)}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Basic CA extensions
        caReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        caReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caReq.PublicKey, false));
        caReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false)); // Server Authentication

        var utcNow = DateTimeOffset.UtcNow;
        var caCert = caReq.CreateSelfSigned(utcNow, utcNow.AddYears(10));

        // Export Root PFX (contains private key, only for development)
        await File.WriteAllBytesAsync(RootPfx, caCert.Export(X509ContentType.Pfx));

        // Export Root CRT (public key only, can be distributed to other devices)
        await File.WriteAllBytesAsync(RootCrt, caCert.Export(X509ContentType.Cert));

        if (OperatingSystem.IsWindows())
        {
            using var cert = X509CertificateLoader.LoadPkcs12FromFile(RootCrt, password: null);
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var existing = store.Certificates.Find(X509FindType.FindBySubjectName, nameof(XboxDownload), false);
            if (existing.Count > 0) store.RemoveRange(existing);
            store.Add(cert);
        }
        else
        {
            await PathHelper.FixOwnershipAsync(RootPfx);
            await PathHelper.FixOwnershipAsync(RootCrt);

            if (OperatingSystem.IsMacOS())
            {
                await CommandHelper.RunCommandAsync("bash", 
                    $"-c \"while security delete-certificate -c '{nameof(XboxDownload)}' /Library/Keychains/System.keychain 2>/dev/null; do :; done\"");
                
                var exitCode = await CommandHelper.RunCommandAsync("security",
                    $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{RootCrt}\"");
                if (exitCode != 0)
                {
                    File.Delete(RootPfx);
                    File.Delete(RootCrt);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    var raw = await File.ReadAllBytesAsync(RootCrt);
                    var pem = "-----BEGIN CERTIFICATE-----\n"
                              + Convert.ToBase64String(raw, Base64FormattingOptions.InsertLineBreaks)
                              + "\n-----END CERTIFICATE-----\n";

                    await InstallCertificateAsync(pem);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    public static async Task DeleteRootCertificateAsync()
    {
        if (File.Exists(RootPfx))
            File.Delete(RootPfx);
        if (File.Exists(RootCrt))
            File.Delete(RootCrt);

        if (OperatingSystem.IsWindows())
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, nameof(XboxDownload), false);
            if (certificates.Count > 0) store.RemoveRange(certificates);
        }
        else if (OperatingSystem.IsMacOS())
        {
            await CommandHelper.RunCommandAsync("bash", 
                $"-c \"while security delete-certificate -c '{nameof(XboxDownload)}' /Library/Keychains/System.keychain 2>/dev/null; do :; done\"");
            /*
            var user = Environment.GetEnvironmentVariable("SUDO_USER") ?? Environment.UserName;
            var home = $"/Users/{user}";
            var loginKeychain = Path.Combine(home, "Library/Keychains/login.keychain-db");
            await CommandHelper.RunCommandAsync("bash",
                $"-c \"while security delete-certificate -c '{nameof(XboxDownload)}' {loginKeychain} 2>/dev/null; do :; done\"");
            */
        }
        else if (OperatingSystem.IsLinux())
        {
            await RemoveCertificateAsync();
        }
    }
    
    /// <summary>
    /// 安装系统 CA 证书
    /// </summary>
    /// <param name="pem">证书内容（PEM 格式）</param>
    [SupportedOSPlatform("linux")]
    private static async Task InstallCertificateAsync(string pem)
    {
        // 1. Debian / Ubuntu / Alpine / OpenSUSE
        if (CommandExists("update-ca-certificates"))
        {
            var dir = Directory.Exists("/usr/share/pki/trust/anchors")
                ? "/usr/share/pki/trust/anchors"        // OpenSUSE
                : "/usr/local/share/ca-certificates";   // Debian / Ubuntu / Alpine

            var certPath = Path.Combine(dir, $"{nameof(XboxDownload)}.crt");

            await WriteCertAsync(certPath, pem);

            // Debian/Ubuntu 下可安全使用 --fresh
            var args = Directory.Exists("/usr/local/share/ca-certificates") ? "--fresh" : "";
            await CommandHelper.RunCommandAsync("update-ca-certificates", args);
            return;
        }

        // 2. RHEL / Fedora / CentOS / Rocky / AlmaLinux
        if (CommandExists("update-ca-trust"))
        {
            const string dir = "/etc/pki/ca-trust/source/anchors";
            var certPath = Path.Combine(dir, $"{nameof(XboxDownload)}.crt");

            await WriteCertAsync(certPath, pem);

            await CommandHelper.RunCommandAsync("update-ca-trust", "extract");
            return;
        }

        // 3. Arch / Manjaro / p11-kit
        if (CommandExists("trust"))
        {
            const string dir = "/etc/ca-certificates/trust-source/anchors";
            var certPath = Path.Combine(dir, $"{nameof(XboxDownload)}.crt");

            await WriteCertAsync(certPath, pem);

            await CommandHelper.RunCommandAsync("trust", "extract-compat");
        }
    }

    /// <summary>
    /// 卸载系统 CA 证书
    /// </summary>
    [SupportedOSPlatform("linux")]
    private static async Task RemoveCertificateAsync()
    {
        // 1. Debian / Ubuntu / Alpine / OpenSUSE
        if (CommandExists("update-ca-certificates"))
        {
            DeleteIfExists(
                $"/usr/local/share/ca-certificates/{nameof(XboxDownload)}.crt",
                $"/usr/share/pki/trust/anchors/{nameof(XboxDownload)}.crt"
            );

            var args = Directory.Exists("/usr/local/share/ca-certificates") ? "--fresh" : "";
            await CommandHelper.RunCommandAsync("update-ca-certificates", args);
            return;
        }

        // 2. RHEL / Fedora / CentOS / Rocky / AlmaLinux
        if (CommandExists("update-ca-trust"))
        {
            DeleteIfExists($"/etc/pki/ca-trust/source/anchors/{nameof(XboxDownload)}.crt");
            await CommandHelper.RunCommandAsync("update-ca-trust", "extract");
            return;
        }

        // 3. Arch / Manjaro / p11-kit
        if (CommandExists("trust"))
        {
            DeleteIfExists($"/etc/ca-certificates/trust-source/anchors/{nameof(XboxDownload)}.crt");
            await CommandHelper.RunCommandAsync("trust", "extract-compat");
        }
    }

    #region Helpers

    /// <summary>
    /// 写入证书文件并设置权限 644
    /// </summary>
    private static async Task WriteCertAsync(string path, string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content);

            try
            {
                await CommandHelper.RunCommandAsync("chmod", $"644 {path}");
            }
            catch
            {
                // 忽略 chmod 失败
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LinuxCertificateHelper] 写入证书失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除文件，如果不存在或失败则忽略
    /// </summary>
    private static void DeleteIfExists(params string[] paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LinuxCertificateHelper] 删除证书失败: {path} - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 检查命令是否存在
    /// </summary>
    private static bool CommandExists(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;

        // 1. 使用 which 检查 (最快最准确，能处理别名和符号链接)
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
            if (process?.ExitCode == 0) return true;
        }
        catch
        {
            // 忽略，降级到手动 PATH 检查
        }

        // 2. PATH 手动检查
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    
        // 即使 PATH 环境变量缺失，ROOT 权限下也要尝试搜索这些核心系统目录
        var searchPaths = pathEnv.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        var systemDirs = new[] { "/usr/sbin", "/usr/bin", "/sbin", "/bin", "/usr/local/sbin" };
    
        foreach (var dir in systemDirs)
        {
            if (!searchPaths.Contains(dir)) searchPaths.Add(dir);
        }

        return searchPaths
            .Select(dir => Path.Combine(dir, command))
            .Any(File.Exists);
    }

    #endregion
}