using System;
using System.Collections.Generic;
using System.Net;

namespace XboxDownload.Models.Dns;

public class ResourceRecord : Query
{
    public uint TTL { get; set; }
    public byte[]? Data { get; set; }

    public ResourceRecord() { }

    public ResourceRecord(Func<int, byte[]> read) : base(read)
    {
        var ttlBytes = read(4);
        if (ttlBytes.Length < 4)
            throw new InvalidOperationException("Invalid TTL length");

        TTL = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ttlBytes, 0));

        var lenBytes = read(2);
        if (lenBytes.Length < 2)
            throw new InvalidOperationException("Invalid length bytes");

        var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(lenBytes, 0));
        if (length < 0)
            throw new InvalidOperationException("Negative RDLENGTH");

        Data = read(length);
    }

    public override byte[] ToBytes()
    {
        var list = new List<byte>();

        list.AddRange(base.ToBytes());

        if (TTL > int.MaxValue)
            throw new InvalidOperationException("TTL exceeds int.MaxValue");

        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)TTL)));

        if (Data != null)
        {
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)Data.Length)));
            list.AddRange(Data);
        }
        else
        {
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)0)));
        }

        return list.ToArray();
    }
}
