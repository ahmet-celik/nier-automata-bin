using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;

namespace nierbin.rite
{
    public static class Magics
    {
        public static readonly byte[] RITE = new byte[] { (byte)'R', (byte)'I', (byte)'T', (byte)'E' };
        public static readonly byte[] MATZ = new byte[] { (byte)'M', (byte)'A', (byte)'T', (byte)'Z' };
        public static readonly byte[] ZERO = new byte[] { (byte)'0', (byte)'0', (byte)'0', (byte)'0' };
        public static readonly byte[] THREE = new byte[] { (byte)'0', (byte)'0', (byte)'0', (byte)'3' };
        public static readonly byte[] IREP = new byte[] { (byte)'I', (byte)'R', (byte)'E', (byte)'P' };
    }
    public class RiteBinary
    {
        public static readonly Int64 CRC_OFFSET = 10;
        public Header header;
        public List<RiteSection> sections;

        public void Read(Stream input)
        {
            header = new RiteBinary.Header();
            header.Read(input);
            sections = new List<RiteSection>();
            while (input.Position < input.Length)
            {
                RiteSection section = new RiteSection();
                section.Read(input);
                sections.Add(section);
            }
        }

        public void Write(Stream output)
        {
            header.Write(output);
            foreach (RiteSection section in sections)
            {
                section.Write(output);
            }
            // update header
            header.size = (UInt32) output.Position;
            output.Position = 0;
            header.Write(output);
            header.crc = MainClass.crc16_citt(output, CRC_OFFSET, header.size - CRC_OFFSET, 0);
            output.Position = 0;
            header.Write(output);
        }

        public class Header
        {


            public byte[] magic;
            public byte[] version;
            public UInt16 crc;
            public UInt32 size;
            public byte[] compilerName;
            public byte[] compilerVersion;

            public void Read(Stream input)
            {
                magic = input.ReadBytes(4);
                version = input.ReadBytes(4);
                crc = input.ReadValueU16(Endian.Big);
                size = input.ReadValueU32(Endian.Big);
                compilerName = input.ReadBytes(4);
                compilerVersion = input.ReadBytes(4);
                if (!MainClass.CheckMagic(magic, Magics.RITE) ||
                    !MainClass.CheckMagic(version, Magics.THREE) ||
                    !MainClass.CheckMagic(compilerName, Magics.MATZ) ||
                    !MainClass.CheckMagic(compilerVersion, Magics.ZERO))
                {
                    Console.WriteLine("Unexpected bin file!");
                    Environment.Exit(1);
                }
            }

            public void Write(Stream output)
            {
                output.WriteBytes(magic);
                output.WriteBytes(version);
                output.WriteValueU16(crc, Endian.Big);
                output.WriteValueU32(size, Endian.Big);
                output.WriteBytes(compilerName);
                output.WriteBytes(compilerVersion);
            }
        }
    }

    public class RiteSection
    {
        public byte[] magic;
        public Int32 size;

        // sections except irep
        public byte[] raw;

        public bool isIREP;
        // irep
        public byte[] irepVersion;
        public RiteIREPRecords irep;

        public void Read(Stream input)
        {
            magic = input.ReadBytes(4);
            size = input.ReadValueS32(Endian.Big);
            if (!MainClass.CheckMagic(magic, Magics.IREP))
            {
                raw = input.ReadBytes(size - 8);
                isIREP = false;
            }
            else
            {
                irepVersion = input.ReadBytes(4);
                if (!MainClass.CheckMagic(irepVersion, Magics.ZERO))
                {
                    Console.WriteLine("Unexpected IREP version!");
                    Environment.Exit(1);
                }
                irep = new RiteIREPRecords();
                irep.Read(input);
                isIREP = true;
            }
        }

        public void Write(Stream output)
        {
            Int64 sectionBegin = output.Position;
            output.WriteBytes(magic);
            output.WriteValueS32(size, Endian.Big);
            if (!isIREP)
            {
                output.WriteBytes(raw);
            }
            else
            {
                output.WriteBytes(irepVersion);
                irep.Write(output);
            }
            size = (Int32) (output.Position - sectionBegin);
            Int64 savePos = output.Position;
            output.Position = sectionBegin + 4;
            output.WriteValueS32(size, Endian.Big);
            output.Position = savePos;
        }
    }

    public class RiteIREPRecord
    {
        public UInt32 recordSize;
        public UInt16 numOfLocals, numOfRegs, numOfChildren;
        public UInt32 iseqLength;
        // skip alignment here
        public UInt32[] iseqCode;
        public UInt32 poolLength;
        public RiteIREPPoolEntry[] pool;
        public UInt32 symbolLength;
        public RiteIREPSymbolEntry[] symbols;

        public void Read(Stream input)
        {
            // Int64 beginRecord = input.Position;
            recordSize = input.ReadValueU32(Endian.Big);
            numOfLocals = input.ReadValueU16(Endian.Big);
            numOfRegs = input.ReadValueU16(Endian.Big);
            numOfChildren = input.ReadValueU16(Endian.Big);
            iseqLength = input.ReadValueU32(Endian.Big);
            Int64 skipped = MainClass.AlignToSkip(input.Position);
            input.Seek(skipped, SeekOrigin.Current);
            iseqCode = new UInt32[iseqLength];
            for (UInt32 i = 0; i < iseqLength; ++i)
            {
                iseqCode[i] = input.ReadValueU32(Endian.Big);
            }
            poolLength = input.ReadValueU32(Endian.Big);
            pool = new RiteIREPPoolEntry[poolLength];
            for (UInt32 i = 0; i < poolLength; ++i)
            {
                pool[i] = new RiteIREPPoolEntry();
                pool[i].Read(input);
            }
            symbolLength = input.ReadValueU32(Endian.Big);
            symbols = new RiteIREPSymbolEntry[symbolLength];
            for (UInt32 i = 0; i < symbolLength; ++i)
            {
                symbols[i] = new RiteIREPSymbolEntry();
                symbols[i].Read(input);
            }
            // Console.WriteLine("record_size=" + recordSize + " diff=" + (input.Position - beginRecord - skipped + 4));
        }

        public void Write(Stream output)
        {
            Int64 recordSizePos = output.Position;
            output.WriteValueU32(recordSize, Endian.Big);
            output.WriteValueU16(numOfLocals, Endian.Big);
            output.WriteValueU16(numOfRegs, Endian.Big);
            output.WriteValueU16(numOfChildren, Endian.Big);
            output.WriteValueU32(iseqLength, Endian.Big);
            Int64 skipped = MainClass.AlignToSkip(output.Position);
            output.WriteBytes(new byte[skipped]);
            for (UInt32 i = 0; i < iseqLength; ++i)
            {
                output.WriteValueU32(iseqCode[i], Endian.Big);
            }
            output.WriteValueU32(poolLength, Endian.Big);
            for (UInt32 i = 0; i < poolLength; ++i)
            {
                pool[i].Write(output);
            }
            output.WriteValueU32(symbolLength, Endian.Big);
            for (UInt32 i = 0; i < symbolLength; ++i)
            {
                symbols[i].Write(output);
            }
            recordSize = (UInt32) (output.Position - recordSizePos - skipped + 4);
            Int64 savePos = output.Position;
            output.Position = recordSizePos;
            output.WriteValueU32(recordSize, Endian.Big);
            output.Position = savePos;
        }
    }

    public class RiteIREPRecords
    {
        public RiteIREPRecord parent;
        public RiteIREPRecords[] children;

        public void Read(Stream input)
        {
            parent = new RiteIREPRecord();
            parent.Read(input);
            children = new RiteIREPRecords[parent.numOfChildren];
            for (UInt32 i = 0; i < parent.numOfChildren; ++i)
            {
                children[i] = new RiteIREPRecords();
                children[i].Read(input);
            }
        }

        public void Write(Stream output)
        {
            parent.Write(output);
            for (UInt32 i = 0; i < parent.numOfChildren; ++i)
            {
                children[i].Write(output);
            }
        }
    }

    public class RiteIREPPoolEntry
    {
        public IREPPoolType type;
        public UInt16 length;
        public byte[] data;

        public void Read(Stream input)
        {
            type = (IREPPoolType)input.ReadByte();
            length = input.ReadValueU16(Endian.Big);
            data = input.ReadBytes(length);
            if (type == IREPPoolType.IREP_TT_STRING)
            {
                MainClass.lines.Add(System.Text.Encoding.UTF8.GetString(data).Replace("\r","<CR>").Replace("\n", "<LF>"));
                // Console.WriteLine();
            }
        }

        public void Write(Stream output)
        {
            if (type == IREPPoolType.IREP_TT_STRING)
            {
                String line = MainClass.newLines[MainClass.lastLine++];
                line = line.Replace("<CR>", "\r").Replace("<LF>", "\n");
                data = System.Text.Encoding.UTF8.GetBytes(line);
                length = (UInt16) data.Length;
            }
            output.WriteByte((byte)type);
            output.WriteValueU16(length, Endian.Big);
            output.WriteBytes(data);
        }
    }

    public class RiteIREPSymbolEntry
    {
        public UInt16 length;
        public byte[] data;

        public void Read(Stream input)
        {
            length = input.ReadValueU16(Endian.Big);
            if (length != 0xFFFF)
            {
                data = input.ReadBytes(length + 1);
            }
        }

        public void Write(Stream output)
        {
            output.WriteValueU16(length, Endian.Big);
            if (length != 0xFFFF)
            {
                output.WriteBytes(data);
            }
        }
    }

    public enum IREPPoolType
    {
        IREP_TT_STRING,
        IREP_TT_FIXNUM,
        IREP_TT_FLOAT,
    }
}
