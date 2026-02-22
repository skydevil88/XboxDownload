using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace XboxDownload.Models.Dns;

public class Query
{
    public string? QueryName { get; set; }
    public QueryType QueryType { get; set; }
    public short QueryClass { get; set; }

    public Query() { }

    public Query(Func<int, byte[]> read)
    {
        byte ReadByte()
        {
            var b = read(1);
            if (b.Length < 1) throw new InvalidOperationException("Unexpected end of stream.");
            return b[0];
        }

        var name = new StringBuilder();
        var length = ReadByte();

        while (length != 0)
        {
            var labelBytes = read(length);
            if (labelBytes.Length != length)
                throw new InvalidOperationException("Label length mismatch.");

            name.Append(Encoding.ASCII.GetString(labelBytes));

            length = ReadByte();
            if (length != 0)
                name.Append('.');
        }

        QueryName = name.ToString();
        QueryType = (QueryType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(read(2), 0));
        QueryClass = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(read(2), 0));
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

        return list.ToArray();
    }
}