using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Services;

namespace XboxDownload.ViewModels;

public partial class CdnViewModel : ObservableObject
{
    private readonly ServiceViewModel _serviceViewModel;

    [ObservableProperty]
    private string _akamaiCdnIp = string.Empty, _akamaiHost1 = string.Empty, _akamaiHost2 = string.Empty;

    public CdnViewModel(ServiceViewModel serviceViewModel)
    {
        _serviceViewModel = serviceViewModel;

        LoadAkamaiHosts();
    }

    public event Action? RequestFocus;

    [RelayCommand]
    public void FocusText()
    {
        RequestFocus?.Invoke();
    }

    [RelayCommand]
    private async Task SaveAkamaiHostsAsync()
    {
        var invalidEntries = new List<string>();
        var ips = AkamaiCdnIp.Replace('，', ',').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        invalidEntries.AddRange(from ip in ips where !IPAddress.TryParse(ip, out _) select ip);

        if (invalidEntries.Count > 0)
        {
            var message = string.Format(
                ResourceHelper.GetString("Cdn.InvalidIpDialogMessage"),
                string.Join("\n", invalidEntries)
            );
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Cdn.InvalidIpDialogTitle"),
                message,
                Icon.Error);
            FocusText();
            return;
        }

        AkamaiCdnIp = string.Join(", ", ips);
        if (!App.Settings.AkamaiCdnIp.Equals(AkamaiCdnIp))
        {
            App.Settings.AkamaiCdnIp = AkamaiCdnIp;
            SettingsManager.Save(App.Settings);
        }

        AkamaiHost2 = AkamaiHost2.Trim();
        try
        {
            if (string.IsNullOrWhiteSpace(AkamaiHost2))
            {
                if (File.Exists(_serviceViewModel.AkamaiFilePath))
                    File.Delete(_serviceViewModel.AkamaiFilePath);
            }
            else
            {
                AkamaiHost2 += Environment.NewLine;
                await File.WriteAllTextAsync(_serviceViewModel.AkamaiFilePath, AkamaiHost2);

                if (!OperatingSystem.IsWindows())
                    _ = PathHelper.FixOwnershipAsync(_serviceViewModel.AkamaiFilePath);
            }
        }
        catch
        {
            // ignored
        }

        if (_serviceViewModel.IsListening)
        {
            await _serviceViewModel.DnsConnectionListener.LoadHostAndAkamaiMapAsync();
            if (_serviceViewModel.IsSetLocalDnsEnabled) CommandHelper.FlushDns();
        }
    }

    [RelayCommand]
    private void LoadAkamaiHosts()
    {
        AkamaiCdnIp = App.Settings.AkamaiCdnIp;
        using (var stream = AssetLoader.Open(new Uri($"avares://{nameof(XboxDownload)}/Resources/Akamai.txt")))
        {
            using (var reader = new StreamReader(stream))
            {
                AkamaiHost1 = reader.ReadToEnd();
            }
        }
        if (File.Exists(_serviceViewModel.AkamaiFilePath))
            AkamaiHost2 = File.ReadAllText(_serviceViewModel.AkamaiFilePath);
    }
}