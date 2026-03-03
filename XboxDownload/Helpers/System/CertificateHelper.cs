using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using XboxDownload.Helpers.IO;

namespace XboxDownload.Helpers.System;

public static class CertificateHelper
{
    public static readonly string RootPfxPath = PathHelper.GetLocalFilePath($"{nameof(XboxDownload)}.pfx");
    public static readonly string RootCrtPath = PathHelper.GetLocalFilePath($"{nameof(XboxDownload)}.crt");

    public static async Task CreateRootCertificate(bool force = false)
    {
        // Skip if not running as root (Windows will also skip)
        if (!Program.UnixUserIsRoot())
            return;

        // Skip if the Root CA already exists and force is not enabled
        if (!force && File.Exists(RootPfxPath) && File.Exists(RootCrtPath))
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

        var pfxOptions = new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Create, Share = FileShare.Read };
        var crtOptions = new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Create, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows())
        {
            // 600: rw-------
            pfxOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            // 644: rw-r--r--
            crtOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        }

        // Export Root PFX (contains private key, only for development)
        var pfxData = caCert.Export(X509ContentType.Pfx);
        await WriteCertAsync(RootPfxPath, pfxOptions, pfxData);

        // Export root certificate in PEM format (public certificate only, safe to distribute)
        var pemBytes = Encoding.UTF8.GetBytes(caCert.ExportCertificatePem());
        await WriteCertAsync(RootCrtPath, crtOptions, pemBytes);

        await PathHelper.FixOwnershipAsync(RootPfxPath);
        await PathHelper.FixOwnershipAsync(RootCrtPath);

        if (OperatingSystem.IsMacOS())
        {
            await CommandHelper.RunCommandAsync("bash",
                $"-c \"while security delete-certificate -c '{nameof(XboxDownload)}' /Library/Keychains/System.keychain 2>/dev/null; do :; done\"");

            var exitCode = await CommandHelper.RunCommandAsync("security",
                $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{RootCrtPath}\"");
            if (exitCode != 0)
            {
                DeleteIfExists(RootPfxPath, RootCrtPath);
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            const string certName = $"{nameof(XboxDownload)}.crt";

            // 1. Debian / Ubuntu / Alpine / OpenSUSE
            var updateCaCertificates = FindCommand("update-ca-certificates");
            if (updateCaCertificates != null)
            {
                var dir = Directory.Exists("/usr/share/pki/trust/anchors")
                    ? "/usr/share/pki/trust/anchors"        // OpenSUSE
                    : "/usr/local/share/ca-certificates";   // Debian / Ubuntu / Alpine

                var certPath = Path.Combine(dir, certName);

                await WriteCertAsync(certPath, crtOptions, pemBytes);

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

                await WriteCertAsync(certPath, crtOptions, pemBytes);

                await CommandHelper.RunCommandAsync(updateCaTrust, "extract");
                return;
            }

            // 3. Arch / Manjaro / p11-kit
            var trust = FindCommand("trust");
            if (trust != null)
            {
                const string dir = "/etc/ca-certificates/trust-source/anchors";
                var certPath = Path.Combine(dir, certName);

                await WriteCertAsync(certPath, crtOptions, pemBytes);

                await CommandHelper.RunCommandAsync(trust, "extract-compat");
            }
        }
    }

    public static async Task DeleteRootCertificateAsync()
    {
        DeleteIfExists(RootPfxPath, RootCrtPath);

        if (OperatingSystem.IsMacOS())
        {
            await CommandHelper.RunCommandAsync("bash",
                $"-c \"while security delete-certificate -c '{nameof(XboxDownload)}' /Library/Keychains/System.keychain 2>/dev/null; do :; done\"");
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
    /// Writes the specified byte array to the given file path asynchronously.
    /// If the parent directory does not exist, it will be created.
    /// The file permissions can be controlled via the provided <see cref="FileStreamOptions"/>.
    /// </summary>
    /// <param name="path">The full path of the file to write.</param>
    /// <param name="options">File stream options controlling access, mode, sharing, and Unix permissions.</param>
    /// <param name="data">The byte array content to write to the file.</param>
    private static async Task WriteCertAsync(string path, FileStreamOptions options, byte[] data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await using var fs = new FileStream(path, options);
            await fs.WriteAsync(data.AsMemory());
        }
        catch
        {
            // Ignore
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