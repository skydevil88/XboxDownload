using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace XboxDownload.Helpers.Network;

public static class FirewallHelper
{
    private const int AllProfiles = 0x7FFFFFFF;
    private const int InboundDirection = 1;
    private const int AllowAction = 1;
    private const int AnyProtocol = 256;

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
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                return;

            var policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (policyType == null || Activator.CreateInstance(policyType) is not { } policyInstance)
                return;

            dynamic policy = policyInstance;
            dynamic rules = policy.Rules;
            var existingRules = GetRulesByName(rules, ruleName);

            var needUpdate = existingRules.Count switch
            {
                0 => true,
                1 => !IsRuleValid(existingRules[0], ruleName, exePath),
                _ => true
            };

            if (!needUpdate)
                return;

            foreach (dynamic rule in existingRules)
            {
                rules.Remove(rule.Name);
            }

            CreateFirewallRule(rules, ruleName, exePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EnsureFirewallRule failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds rules managed by this helper.
    /// </summary>
    private static List<dynamic> GetRulesByName(dynamic rules, string ruleName)
    {
        var matchingRules = new List<dynamic>();

        foreach (dynamic rule in rules)
        {
            if (string.Equals((string?)rule.Name, ruleName, StringComparison.OrdinalIgnoreCase))
            {
                matchingRules.Add(rule);
            }
        }

        return matchingRules;
    }

    private static bool IsRuleValid(dynamic rule, string ruleName, string exePath)
    {
        var profiles = (int)rule.Profiles;

        return string.Equals((string?)rule.Name, ruleName, StringComparison.OrdinalIgnoreCase)
               && string.Equals((string?)rule.ApplicationName, exePath, StringComparison.OrdinalIgnoreCase)
               && rule.Enabled
               && rule.Direction == InboundDirection
               && rule.Protocol == AnyProtocol
               && rule.Action == AllowAction
               && profiles == AllProfiles;
    }

    /// <summary>
    /// Creates a new inbound application firewall rule with full profile coverage.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void CreateFirewallRule(dynamic rules, string ruleName, string exePath)
    {
        var ruleType = Type.GetTypeFromProgID("HNetCfg.FWRule");
        if (ruleType == null || Activator.CreateInstance(ruleType) is not { } ruleInstance)
            return;

        dynamic rule = ruleInstance;
        rule.Name = ruleName;
        rule.ApplicationName = exePath;
        rule.Action = AllowAction;
        rule.Direction = InboundDirection;
        rule.Protocol = AnyProtocol;
        rule.Profiles = AllProfiles;
        rule.Enabled = true;
        rule.Description = "Allow inbound access for XboxDownload tool";

        rules.Add(rule);
    }
}
