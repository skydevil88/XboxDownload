using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallRules;

namespace XboxDownload.Helpers.Network;

public static class FirewallHelper
{
    /// <summary>
    /// Ensure a firewall rule with the given name exists and matches the current executable path.
    /// </summary>
    /// <param name="ruleName">The name of the firewall rule to ensure.</param>
    [SupportedOSPlatform("windows")]
    public static void EnsureFirewallRule(string ruleName)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(ruleName))
            return;

        try
        {
            // Get current executable path
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                return;

            var firewall = FirewallManager.Instance;

            if (firewall == null)
                return;

            // Get all rules with matching name (case-insensitive)
            var existingRules = firewall.Rules
                .Where(r => r.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var needUpdate = existingRules.Count switch
            {
                0 => true,
                1 => !IsRuleValid(existingRules[0], exePath),
                _ => true
            };

            if (!needUpdate)
                return;

            // Remove all existing rules
            foreach (var rule in existingRules)
            {
                firewall.Rules.Remove(rule);
            }

            // Add new rule
            CreateFirewallRule(firewall, ruleName, exePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnsureFirewallRule failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks whether the specified firewall rule is valid:
    /// - The application path matches
    /// - The rule is enabled
    /// - All required profiles are present (Domain, Private, Public)
    /// </summary>
    private static bool IsRuleValid(IFirewallRule rule, string exePath)
    {
        const FirewallProfiles requiredProfiles =
            FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public;

        return string.Equals(rule.ApplicationName, exePath, StringComparison.OrdinalIgnoreCase)
               && rule.IsEnable
               && (rule.Profiles & requiredProfiles) == requiredProfiles;
    }

    /// <summary>
    /// Creates a new inbound application firewall rule with full profile coverage.
    /// </summary>
    private static void CreateFirewallRule(IFirewall firewall, string ruleName, string exePath)
    {
        var rule = new FirewallWASRule(
            ruleName,
            exePath,
            FirewallAction.Allow,
            FirewallDirection.Inbound,
            FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public
        )
        {
            IsEnable = true,
            Description = "Allow inbound access for XboxDownload tool"
        };
        firewall.Rules.Add(rule);
    }
}
