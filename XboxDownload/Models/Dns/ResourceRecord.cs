using System;
using System.Collections.Generic;
using System.Net;

namespace XboxDownload.Models.Dns;

public class ResourceRecord : Query
{
    public uint Ttl { get; init; }
    public byte[]? Data { get; init; }

    public ResourceRecord() { }

    internal ResourceRecord(DnsMessage.DnsPacketReader reader) : base(reader)
    {
        Ttl = reader.ReadUInt32();

        var length = reader.ReadUInt16();
        Data = reader.ReadBytes(length);
    }

    public override byte[] ToBytes()
    {
        var list = new List<byte>();

        list.AddRange(base.ToBytes());

        if (Ttl > int.MaxValue)
            throw new InvalidOperationException("TTL exceeds int.MaxValue");

        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Ttl)));

        if (Data != null)
        {
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)Data.Length)));
            list.AddRange(Data);
        }
        else
        {
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)0)));
        }

        return [.. list];
    }
}
