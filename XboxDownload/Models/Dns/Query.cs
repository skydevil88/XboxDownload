using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace XboxDownload.Models.Dns;

public class Query
{
    public string? QueryName { get; init; }
    public QueryType QueryType { get; init; }
    public short QueryClass { get; init; }

    protected Query() { }

    internal Query(DnsMessage.DnsPacketReader reader)
    {
        QueryName = reader.ReadDomainName();
        QueryType = (QueryType)reader.ReadUInt16();
        QueryClass = (short)reader.ReadUInt16();
    }

    public virtual byte[] ToBytes()
    {
        var list = new List<byte>();

        if (!string.IsNullOrEmpty(QueryName))
        {
            var parts = QueryName.TrimEnd('.').Split('.');
            foreach (var part in parts)
            {
                if (part.Length > 63)
                    throw new InvalidOperationException("Label too long for DNS.");

                list.Add((byte)part.Length);
                list.AddRange(Encoding.ASCII.GetBytes(part));
            }
        }

        list.Add(0); // End of domain name

        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)QueryType)));
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(QueryClass)));

        return [.. list];
    }
}
