using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XboxDownload.Helpers.Network;

public static class NetworkAdapterHelper
{
    // List of keywords to identify virtual network adapters
    private static readonly string[] VirtualKeywords =
    [
        //"virtual", "vmware", "hyper-v", "loopback", "tunneling", "pseudo", "container", "tap", "bridge", "npcap"
        "virtual", "bridge"
    ];

    // Determines whether the specified network interface is virtual
    private static bool IsVirtualAdapter(NetworkInterface nic)
    {
        return VirtualKeywords.Any(v =>
            nic.Description.Contains(v, StringComparison.OrdinalIgnoreCase) ||
            nic.Name.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    // Determines whether the specified network adapter is valid:
    // - Not a loopback adapter
    // - Operational status is Up
    // - Is either Ethernet or Wireless80211
    // - Is not virtual (based on keywords)
    // - Has at least one IPv4 unicast address
    private static bool IsValidAdapter(NetworkInterface nic)
    {
        return nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
               && nic is { OperationalStatus: OperationalStatus.Up, NetworkInterfaceType: NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 }
               && !IsVirtualAdapter(nic)
               && nic.GetIPProperties().UnicastAddresses.Any(ip =>
                   ip.Address.AddressFamily == AddressFamily.InterNetwork);
    }

    // Retrieves all valid network adapters based on the criteria defined in IsValidAdapter.
    // If no valid adapters are found, relaxes filtering by ignoring virtual adapter detection,
    // and returns all non-loopback, up adapters with an IPv4 address.
    public static NetworkInterface[] GetValidAdapters()
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(IsValidAdapter)
            .ToArray();

        // If no adapters found, relax filtering criteria
        if (adapters.Length == 0)
        {
            adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                    && nic.OperationalStatus == OperationalStatus.Up
                    && nic.GetIPProperties().UnicastAddresses.Any(ip =>
                        ip.Address.AddressFamily == AddressFamily.InterNetwork))
                .ToArray();
        }

        return adapters;
    }
}