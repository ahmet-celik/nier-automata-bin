using System;
using System.Collections.Generic;
using System.IO;
using nierbin.rite;

namespace nierbin
{
    public class MainClass
    {
        public static List<String> lines;
        public static String[] newLines;
        public static int lastLine;
        public static void Main(string[] args)
        {
            Console.WriteLine("--- NieR:Automata bin tool -- celikeins -- 2019.04.14 ---");
            lines = new List<String>();
            if (args.Length != 1)
            {
                Help();
            }
            if (args[0].EndsWith(".bin"))
            {
                using (Stream input = File.OpenRead(args[0]))
                {
                    RiteBinary riteBinary = new RiteBinary();
                    riteBinary.Read(input);
                    //Console.WriteLine("Found " + lines.Count + " strings!");
                    File.WriteAllLines(args[0] + ".txt", lines, System.Text.Encoding.UTF8);
                }
            }
            else if (args[0].EndsWith(".bin.txt"))
            {
                lastLine = 0;
                newLines = File.ReadAllLines(args[0], System.Text.Encoding.UTF8);
                using (Stream input = File.OpenRead(Path.ChangeExtension(args[0], null)),
                              ouput = File.Open(Path.ChangeExtension(args[0], null) + ".NEW", FileMode.Create))
                {
                    RiteBinary riteBinary = new RiteBinary();
                    riteBinary.Read(input);
                    if (lines.Count != newLines.Length)
                    {
                        Console.WriteLine("Expected " + lines.Count + " lines, but found " + newLines.Length + " lines!");
                        Environment.Exit(1);
                    }
                    riteBinary.Write(ouput);
                }
            }
            else
            {
                Help();  
            }
            Console.WriteLine("Done!");
        }

        #region Utils
        public static Int64 AlignToSkip(Int64 offset)
        {
            return -offset & 3;
        }

        public static bool CheckMagic(byte[] magic1, byte[] magic2)
        {
            if (magic1.Length != magic2.Length)
            {
                return false;
            }
            for (int i = 0; i < magic1.Length; ++i)
            {
                if (magic1[i] != magic2[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static readonly UInt32 CRC_16_CCITT = 0x11021; /* x^16+x^12+x^5+1 */
        private static readonly UInt32 CRC_XOR_PATTERN = CRC_16_CCITT << 8;
        private static readonly UInt32 CRC_CARRY_BIT = 0x01000000;
        public static UInt16 crc16_citt(Stream input, Int64 pos, Int64 length, UInt32 seed)
        {
            Int64 savePos = input.Position;
            input.Position = pos;
            UInt32 ibit;
            UInt32 crcwk = seed << 8;
            while (input.Position < pos + length)
            {
                crcwk |= (UInt32) input.ReadByte();
                for (ibit = 0; ibit < 8; ibit++)
                {
                    crcwk <<= 1;
                    if ((crcwk & CRC_CARRY_BIT) != 0)
                    {
                        crcwk ^= CRC_XOR_PATTERN;
                    }
                }
            }
            input.Position = savePos;
            return (UInt16)(crcwk >> 8);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static void Help()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine(" exporting constant pool: " + GetExecutableName() + " p300_33eec348_scp.bin");
            Console.WriteLine("           Each constant will be in a line of p300_33eec348_scp.bin.txt.");
            Console.WriteLine("           <CR> and <LF> are special strings. Do not delete them.");
            Console.WriteLine(" importing constant pool: " + GetExecutableName() + " p300_33eec348_scp.bin.txt");
            Console.WriteLine("           New bin with new constant pool will be p300_33eec348_scp.bin.NEW.");
            Environment.Exit(1);
        }
        #endregion
    }
}
