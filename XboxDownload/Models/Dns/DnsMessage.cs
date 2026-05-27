using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace XboxDownload.Models.Dns;

public class DnsMessage
{
    // ===== Header Fields =====
    private ushort Id { get; set; } // Transaction ID
    public int Qr { get; set; }     // 0 = query, 1 = response
    public int Opcode { get; set; } // 0 = standard query
    private int Aa { get; set; }    // Authoritative Answer
    private int Tc { get; set; }    // Truncation
    public int Rd { get; set; }     // Recursion Desired
    public int Ra { get; set; }     // Recursion Available
    private int RCode { get; set; } // Response code (0 = no error, 3 = name error)

    // ===== Section Counts =====
    public List<Query> Queries { get; set; }
    public List<ResourceRecord> ResourceRecords { get; set; }
    private short AuthorityRecordCount { get; set; }
    private short AdditionalRecordCount { get; set; }

    // ===== Serialization =====
    public byte[] ToBytes()
    {
        var list = new List<byte>();

        // 1. Transaction ID
        list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)Id)));

        // 2. Flags (QR, Opcode, AA, TC, RD | RA, Z=0, RCode)
        byte b1 = 0;
        b1 = b1.SetBits(Qr, 0, 1)
               .SetBits(Opcode, 1, 4)
               .SetBits(Aa, 5, 1)
               .SetBits(Tc, 6, 1)
               .SetBits(Rd, 7, 1);

        byte b2 = 0;
        b2 = b2.SetBits(Ra, 0, 1)
               .SetBits(0, 1, 3)      // Z bits = 0
               .SetBits(RCode, 4, 4);

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

        return [.. list];
    }

    // ===== Deserialization =====

    public DnsMessage(byte[] buffer, int length)
    {
        if (length < 12)
            throw new InvalidOperationException("DNS message is shorter than header length.");

        var package = new byte[length];
        Array.Copy(buffer, package, length);
        var reader = new DnsPacketReader(package);

        // 1. Transaction ID
        Id = reader.ReadUInt16();

        // 2. Flags
        var b1 = reader.ReadByte();
        var b2 = reader.ReadByte();

        Qr = b1.GetBits(0, 1);
        Opcode = b1.GetBits(1, 4);
        Aa = b1.GetBits(5, 1);
        Tc = b1.GetBits(6, 1);
        Rd = b1.GetBits(7, 1);

        Ra = b2.GetBits(0, 1);
        RCode = b2.GetBits(4, 4);

        // 3. Counts
        var queryCount = reader.ReadUInt16();
        var rrCount = reader.ReadUInt16();
        AuthorityRecordCount = (short)reader.ReadUInt16();
        AdditionalRecordCount = (short)reader.ReadUInt16();

        // 4. Queries
        Queries = [];
        for (var i = 0; i < queryCount; i++)
            Queries.Add(new Query(reader));

        // 5. Answers
        ResourceRecords = [];
        for (var i = 0; i < rrCount; i++)
            ResourceRecords.Add(new ResourceRecord(reader));
    }

    internal sealed class DnsPacketReader(byte[] packet)
    {
        private const int MaxNameJumps = 16;
        private int _offset;

        public byte ReadByte()
        {
            EnsureAvailable(1);
            return packet[_offset++];
        }

        public byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new InvalidOperationException("Negative read length.");

            EnsureAvailable(count);
            var bytes = new byte[count];
            Array.Copy(packet, _offset, bytes, 0, count);
            _offset += count;
            return bytes;
        }

        public ushort ReadUInt16()
        {
            EnsureAvailable(2);
            var value = (ushort)((packet[_offset] << 8) | packet[_offset + 1]);
            _offset += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureAvailable(4);
            var value =
                ((uint)packet[_offset] << 24) |
                ((uint)packet[_offset + 1] << 16) |
                ((uint)packet[_offset + 2] << 8) |
                packet[_offset + 3];
            _offset += 4;
            return value;
        }

        public string ReadDomainName()
        {
            var labels = new List<string>();
            var cursor = _offset;
            var consumedOffset = -1;
            var jumps = 0;

            while (true)
            {
                if (cursor >= packet.Length)
                    throw new InvalidOperationException("DNS name exceeds packet length.");

                var length = packet[cursor++];
                if (length == 0)
                {
                    if (consumedOffset < 0)
                        consumedOffset = cursor;
                    break;
                }

                var labelType = length & 0xC0;
                if (labelType == 0xC0)
                {
                    if (cursor >= packet.Length)
                        throw new InvalidOperationException("DNS compression pointer is truncated.");

                    if (++jumps > MaxNameJumps)
                        throw new InvalidOperationException("DNS compression pointer loop detected.");

                    var pointer = ((length & 0x3F) << 8) | packet[cursor++];
                    if (pointer >= packet.Length)
                        throw new InvalidOperationException("DNS compression pointer exceeds packet length.");

                    if (consumedOffset < 0)
                        consumedOffset = cursor;
                    cursor = pointer;
                    continue;
                }

                if (labelType != 0)
                    throw new InvalidOperationException("Unsupported DNS label type.");

                if (length > 63)
                    throw new InvalidOperationException("DNS label exceeds maximum length.");

                if (cursor + length > packet.Length)
                    throw new InvalidOperationException("DNS label exceeds packet length.");

                labels.Add(Encoding.ASCII.GetString(packet, cursor, length));
                cursor += length;
            }

            _offset = consumedOffset;
            return string.Join('.', labels);
        }

        private void EnsureAvailable(int count)
        {
            if (count < 0 || _offset > packet.Length - count)
                throw new InvalidOperationException("Unexpected end of DNS message.");
        }
    }
}
