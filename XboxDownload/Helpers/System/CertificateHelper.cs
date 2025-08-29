using System;
using System.IO;
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
        caReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // Server Authentication

        var utcNow = DateTimeOffset.UtcNow;
        var caCert = caReq.CreateSelfSigned(utcNow, utcNow.AddYears(10));

        // Export Root PFX (contains private key, only for development)
        await File.WriteAllBytesAsync(RootPfx, caCert.Export(X509ContentType.Pfx));
        
        // Export Root CRT (public key only, can be distributed to other devices)
        await File.WriteAllBytesAsync(RootCrt, caCert.Export(X509ContentType.Cert));
        
        if (!OperatingSystem.IsWindows())
        {
            await PathHelper.FixOwnershipAsync(RootPfx);
            await PathHelper.FixOwnershipAsync(RootCrt);
        }

        if (OperatingSystem.IsWindows())
        {
            //using var cert = new X509Certificate2(RootCrt);
            using var cert = new X509Certificate2(RootCrt, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var existing = store.Certificates.Find(X509FindType.FindBySubjectName, nameof(XboxDownload), false);
            if (existing.Count > 0) store.RemoveRange(existing);
            store.Add(cert);
        }
        else if (OperatingSystem.IsMacOS())
        {
            try
            {
                await CommandHelper.RunCommandAsync("sudo", $"security delete-certificate -c \"{nameof(XboxDownload)}\" /Library/Keychains/System.keychain");
            }
            catch
            {
                // ignored
            }
            
            var exitCode = await CommandHelper.RunCommandAsync2("security", $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{RootCrt}\"");
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

                if (File.Exists("/etc/os-release"))
                {
                    var osRelease = await File.ReadAllTextAsync("/etc/os-release");

                    if (osRelease.Contains("ID_LIKE=debian") || osRelease.Contains("ID=debian") || osRelease.Contains("ID=ubuntu"))
                    {
                        // Debian/Ubuntu
                        var certPath = $"/usr/local/share/ca-certificates/{nameof(XboxDownload)}.crt";
                        await File.WriteAllTextAsync(certPath, pem);
                        await CommandHelper.RunCommandAsync("update-ca-certificates", "");
                    }
                    else if (osRelease.Contains("ID_LIKE=\"rhel fedora\"") || osRelease.Contains("ID=fedora") || osRelease.Contains("ID=centos") || osRelease.Contains("ID=rhel"))
                    {
                        // RHEL/CentOS/Fedora
                        var certPath = $"/etc/pki/ca-trust/source/anchors/{nameof(XboxDownload)}.crt";
                        await File.WriteAllTextAsync(certPath, pem);
                        await CommandHelper.RunCommandAsync("update-ca-trust", "extract");
                    }
                }
            }
            catch
            {
                // ignored
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
            try
            {
                await CommandHelper.RunCommandAsync("sudo", $"security delete-certificate -c \"{nameof(XboxDownload)}\" /Library/Keychains/System.keychain");
            }
            catch
            {
                // ignored
            }
            
            var user = Environment.GetEnvironmentVariable("SUDO_USER") ?? Environment.UserName;
            var home = $"/Users/{user}";
            var loginKeychain = Path.Combine(home, "Library/Keychains/login.keychain-db");
                
            var pipeline = $"security find-certificate -c \"{nameof(XboxDownload)}\" -a -Z \"{loginKeychain}\" | grep \"SHA-1\" | awk '{{print $NF}}' | xargs -I {{}} security delete-certificate -Z {{}} \"{loginKeychain}\"";
            await CommandHelper.RunCommandAsync("bash", $"-c \"{pipeline}\"");
        }
        else if (OperatingSystem.IsLinux())
        {
            var certPath = $"/usr/local/share/ca-certificates/{nameof(XboxDownload)}.crt";
            try
            {
                if (File.Exists(certPath))
                    File.Delete(certPath);
                
                await CommandHelper.RunCommandAsync("update-ca-certificates", "");
            }
            catch
            {
                // ignored
            }
        }
    }
}