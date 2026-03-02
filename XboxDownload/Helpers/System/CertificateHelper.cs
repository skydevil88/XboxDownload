using System;
using System.IO;
using System.Linq;
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
        // Skip if not running as root (Windows will also skip)
        if (!Program.UnixUserIsRoot())
            return;

        // Skip if the Root CA already exists and force is not enabled
        if (!force && File.Exists(RootPfx) && File.Exists(RootCrt))
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
                DeleteIfExists(RootPfx, RootCrt);
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            var raw = await File.ReadAllBytesAsync(RootCrt);
            var pem = "-----BEGIN CERTIFICATE-----\n"
                      + Convert.ToBase64String(raw, Base64FormattingOptions.InsertLineBreaks)
                      + "\n-----END CERTIFICATE-----\n";

            const string certName = $"{nameof(XboxDownload)}.crt";

            // 1. Debian / Ubuntu / Alpine / OpenSUSE
            var updateCaCertificates = FindCommand("update-ca-certificates");
            if (updateCaCertificates != null)
            {
                var dir = Directory.Exists("/usr/share/pki/trust/anchors")
                    ? "/usr/share/pki/trust/anchors"        // OpenSUSE
                    : "/usr/local/share/ca-certificates";   // Debian / Ubuntu / Alpine

                var certPath = Path.Combine(dir, certName);

                await WriteCertAsync(certPath, pem);

                // Debian/Ubuntu support the --fresh option
                if (Directory.Exists("/usr/local/share/ca-certificates"))
                    await CommandHelper.RunCommandAsync(updateCaCertificates, "--fresh");
                else
                    await CommandHelper.RunCommandAsync(updateCaCertificates);

                return;
            }

            // 2. RHEL / Fedora / CentOS / Rocky / AlmaLinux
            var updateCaTrust = FindCommand("update-ca-trust");
            if (updateCaTrust != null)
            {
                const string dir = "/etc/pki/ca-trust/source/anchors";
                var certPath = Path.Combine(dir, certName);

                await WriteCertAsync(certPath, pem);

                await CommandHelper.RunCommandAsync(updateCaTrust, "extract");
                return;
            }

            // 3. Arch / Manjaro / p11-kit
            var trust = FindCommand("trust");
            if (trust != null)
            {
                const string dir = "/etc/ca-certificates/trust-source/anchors";
                var certPath = Path.Combine(dir, certName);

                await WriteCertAsync(certPath, pem);

                await CommandHelper.RunCommandAsync(trust, "extract-compat");
            }
        }
    }

    public static async Task DeleteRootCertificateAsync()
    {
        DeleteIfExists(RootPfx, RootCrt);

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
            // 1. Debian / Ubuntu / Alpine / OpenSUSE
            var updateCaCertificates = FindCommand("update-ca-certificates");
            if (updateCaCertificates != null)
            {
                DeleteIfExists(
                    $"/usr/local/share/ca-certificates/{nameof(XboxDownload)}.crt",
                    $"/usr/share/pki/trust/anchors/{nameof(XboxDownload)}.crt"
                );

                // Debian/Ubuntu can safely use --fresh
                if (Directory.Exists("/usr/local/share/ca-certificates"))
                    await CommandHelper.RunCommandAsync(updateCaCertificates, "--fresh");
                else
                    await CommandHelper.RunCommandAsync(updateCaCertificates);

                return;
            }

            // 2. RHEL / Fedora / CentOS / Rocky / AlmaLinux
            var updateCaTrust = FindCommand("update-ca-trust");
            if (updateCaTrust != null)
            {
                DeleteIfExists($"/etc/pki/ca-trust/source/anchors/{nameof(XboxDownload)}.crt");
                await CommandHelper.RunCommandAsync(updateCaTrust, "extract");
                return;
            }

            // 3. Arch / Manjaro / p11-kit
            var trust = FindCommand("trust");
            if (trust != null)
            {
                DeleteIfExists($"/etc/ca-certificates/trust-source/anchors/{nameof(XboxDownload)}.crt");
                await CommandHelper.RunCommandAsync(trust, "extract-compat");
            }
        }
    }

     /// <summary>
    /// Writes the certificate file and sets the permission to 644 (rw-r--r--)
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
                // Ignore chmod failure
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LinuxCertificateHelper] Failed to write certificate: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes files if they exist.
    /// </summary>
    private static void DeleteIfExists(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            try
            {
                File.Delete(path);
            }
            catch
            {
                // Ignore
            }
        }
    }
    
    /// <summary>
    /// Finds the full path of a command in PATH.
    /// Returns null if the command cannot be found.
    /// </summary>
    private static string? FindCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        var searchPaths = pathEnv
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Concat(
            [
                "/usr/local/sbin",
                "/usr/local/bin",
                "/usr/sbin",
                "/usr/bin",
                "/sbin",
                "/bin"
            ])
            .Distinct();

        foreach (var dir in searchPaths)
        {
            var fullPath = Path.Combine(dir, command);

            if (File.Exists(fullPath) && !Directory.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}