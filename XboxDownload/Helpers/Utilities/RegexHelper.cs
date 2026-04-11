using System.Text.RegularExpressions;

namespace XboxDownload.Helpers.Utilities;

public static partial class RegexHelper
{
    // These patterns exceed the source generator's implementation limits, so keep them as cached Regex instances.
    private static readonly Regex HostsAndDnsmasq = new(
        @"(" +
        @"^(?<IP>(((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|" +
        @"([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|" +
        @":([0-9a-fA-F]{1,4}:){1,7}|" +
        @"([0-9a-fA-F]{1,4}:){1,6}(:[0-9a-fA-F]{1,4}){1}|" +
        @"([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|" +
        @"([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|" +
        @"([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}))" +
        @"\s+(?<HostName>[^\s#]+)" +
        @"(?:\s+#(?<Comment>.*))?$" +
        @"|" +
        @"^address=/(?<HostName>[^\s/]+)/(?!\1$)" +
        @"(?<IP>(((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|" +
        @"([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|" +
        @":([0-9a-fA-F]{1,4}:){1,7}|" +
        @"([0-9a-fA-F]{1,4}:){1,6}(:[0-9a-fA-F]{1,4}){1}|" +
        @"([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|" +
        @"([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|" +
        @"([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}))" +
        @"(?:\s+#(?<Comment>.*))?$" +
        @")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ExtractProductId = new(
        @"^https?://www\.xbox\.com(/[^/]*)?/games/store/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
        @"^https?://www\.microsoft\.com(/[^/]*)?/p/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
        @"^https?://www\.microsoft\.com/store/productId/(?<productId>[a-zA-Z0-9]{12})|" +
        @"^https?://apps\.microsoft\.com(/store)?/detail(/[^/]+)?/(?<productId>[a-zA-Z0-9]{12})|" +
        @"productid=(?<productId>[a-zA-Z0-9]{12})|" +
        @"^(?<productId>[a-zA-Z0-9]{12})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [GeneratedRegex(@"(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s+\((?<Location>.+)\)")]
    public static partial Regex ExtractIpv4AndLocation();

    [GeneratedRegex(@"\s+")]
    public static partial Regex MultipleSpacesRegex();

    public static Regex HostsAndDnsmasqRegex() => HostsAndDnsmasq;

    [GeneratedRegex(
        @"^(?!-)(?![0-9]+$)(?!.*--)[a-z0-9-]{1,63}(?:\.[a-z0-9-]{1,63})*\.[a-z]{2,}$|^(?!-)(?![0-9]+$)(?!.*--)[a-z0-9-]{1,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    public static partial Regex IsValidDomainOrHostname();

    [GeneratedRegex(
        @"^(?!-)(?![0-9]+$)(?!.*--)(?:[a-z0-9-]{1,63}\.)+[a-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    public static partial Regex IsValidDomain();

    public static Regex ExtractProductIdRegex() => ExtractProductId;

    [GeneratedRegex(@"(\d+\.\d+\.\d+\.\d+)")]
    public static partial Regex GetVersion();

    [GeneratedRegex(@"[^\x00-\xFF]")]
    public static partial Regex NonAsciiRegex();

    [GeneratedRegex(@"^(https?://)?([^/|:]+).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    public static partial Regex ExtractDomainFromUrlRegex();
}
