using System;
using System.Collections.Generic;
using System.Net;

namespace XboxDownload.Models.Dns;

public class DnsMessage
{
    // ===== Header Fields =====
    public ushort ID { get; set; }  // Transaction ID
    public int QR { get; set; }     // 0 = query, 1 = response
    public int Opcode { get; set; } // 0 = standard query
    public int AA { get; set; }     // Authoritative Answer
    public int TC { get; set; }     // Truncation
    public int RD { get; set; }     // Recursion Desired
    public int RA { get; set; }     // Recursion Available
    public int Rcode { get; set; }  // Response code (0 = no error, 3 = name error)

    // ===== Section Counts =====
    public List<Query> Queries { get; set; } = new();
    public List<ResourceRecord> ResourceRecords { get; set; } = new();
    public short AuthorityRecordCount { get; set; }
    public short AdditionalRecordCount { get; set; }

    // ===== Serialization =====
    public byte[] ToBytes()
    {
        var list = new List<byte>();

        // 1. Transaction ID
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ID)));

        // 2. Flags (QR, Opcode, AA, TC, RD | RA, Z=0, Rcode)
        byte b1 = 0;
        b1 = b1.SetBits(QR, 0, 1)
               .SetBits(Opcode, 1, 4)
               .SetBits(AA, 5, 1)
               .SetBits(TC, 6, 1)
               .SetBits(RD, 7, 1);

        byte b2 = 0;
        b2 = b2.SetBits(RA, 0, 1)
               .SetBits(0, 1, 3)      // Z bits = 0
               .SetBits(Rcode, 4, 4);

        list.Add(b1);
        list.Add(b2);

        // 3. Counts
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)Queries.Count)));
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ResourceRecords.Count)));
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(AuthorityRecordCount)));
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(AdditionalRecordCount)));

        // 4. Queries
        foreach (var q in Queries)
            list.AddRange(q.ToBytes());

        // 5. Answers (Resource Records)
        foreach (var r in ResourceRecords)
            list.AddRange(r.ToBytes());

        return list.ToArray();
    }

    // ===== Deserialization =====

    private int index = 0;
    private readonly byte[] package;

    private byte ReadByte() => package[index++];

    private byte[] ReadBytes(int count = 1)
    {
        var bytes = new byte[count];
        for (var i = 0; i < count; i++)
            bytes[i] = ReadByte();
        return bytes;
    }

    public DnsMessage(byte[] buffer, int length)
    {
        package = new byte[length];
        Array.Copy(buffer, package, length);

        // 1. Transaction ID
        ID = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));

        // 2. Flags
        byte b1 = ReadByte();
        byte b2 = ReadByte();

        QR = b1.GetBits(0, 1);
        Opcode = b1.GetBits(1, 4);
        AA = b1.GetBits(5, 1);
        TC = b1.GetBits(6, 1);
        RD = b1.GetBits(7, 1);

        RA = b2.GetBits(0, 1);
        Rcode = b2.GetBits(4, 4);

        // 3. Counts
        var queryCount = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));
        var rrCount = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));
        AuthorityRecordCount = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));
        AdditionalRecordCount = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));

        // 4. Queries
        Queries = new List<Query>();
        for (int i = 0; i < queryCount; i++)
            Queries.Add(new Query(ReadBytes));

        // 5. Answers
        ResourceRecords = new List<ResourceRecord>();
        for (int i = 0; i < rrCount; i++)
            ResourceRecords.Add(new ResourceRecord(ReadBytes));
    }

    // Optional: default constructor for manual build
    public DnsMessage()
    {
        package = Array.Empty<byte>(); // Initialize to an empty array
    }
}
