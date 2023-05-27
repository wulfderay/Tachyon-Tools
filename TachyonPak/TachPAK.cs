using System;
using System.Linq;
using System.Diagnostics;

namespace TachyonPak
{
    class TachPAK
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            string[] operations = args[0].ToLower().Substring(1).Select(c => "-" + c).ToArray();
            string filePath = args[1];

            var pakparser = new PAKParser();
            var pak = pakparser.ParsePAKFile(filePath);

            foreach (string operation in operations)
            {
                switch (operation)
                {
                    case "-h":
                        PrintPAKHeader(pak.Header);
                        break;
                    case "-l":
                        PrintLODInformation(pak);
                        break;
                    case "-t":
                        PrintTextureInformation(pak);
                        break;
                    case "-e":
                        if (args.Length < 3)
                        {
                            PrintUsage();
                            return;
                        }
                        string outputDirectory = args[2];
                        PAKParser.ExtractTextures(pak, outputDirectory);
                        break;
                    default:
                        WriteLine($"Invalid operation: {operation}");
                        PrintUsage();
                        return;
                }
            }
        }

        public static void PrintUsage()
        {
            WriteLine("Usage: TachyonPak <operation> <input_file> [<output_directory>]");
            WriteLine("Operations:");
            WriteLine("  -h        - Print the PAK header");
            WriteLine("  -l        - Print LOD information");
            WriteLine("  -t        - Print texture information");
            WriteLine("  -e        - Extract textures to the specified output directory");
        }

        public static void PrintPAKHeader(PAKHeader header)
        {
            WriteLine("PAK Header:");
            WriteLine("Identifier: " + header.Identifier);
            WriteLine("Version: 0x" + header.Version.ToString("X"));
            WriteLine("Name: " + header.Name);
            WriteLine("Unknown: 0x" + header.Unknown.ToString("X"));
            WriteLine("Number of LOD: 0x" + header.noOfLOD.ToString("X"));
            WriteLine("Unknown2: 0x" + header.Unknown2.ToString("X"));
            WriteLine("Filler: 0x" + header.Filler.ToString("X"));
            WriteLine("Info List Offset: 0x" + header.infoList.ToString("X"));
            WriteLine("Texture Start Offset: 0x" + header.textureStart.ToString("X"));
            WriteLine("Texture End Offset: 0x" + header.textureEnd.ToString("X"));
            WriteLine("Filler2: 0x" + header.Filler2.ToString("X"));
            WriteLine("Unknown3: 0x" + header.Unknown3.ToString("X"));
            WriteLine("Filler3: " + string.Join(", ", header.Filler3.Select(x => "0x" + x.ToString("X"))));
        }
        public static void PrintLODInformation(PAKFile pak)
        {
            WriteLine("LOD Information:");
            WriteLine("LODWeirdBytes: " + string.Join(", ", pak.MysteryLODBytes.Select(x => "0x" + x.ToString("X"))));
            foreach (var lod in pak.LODs)
            {
                PrintLODHeader(lod.Header);
                foreach (var lodEntry in lod.Entries)
                    PrintLODEntry(lodEntry);
            }
        }

        public static void PrintTextureInformation(PAKFile pak)
        {
            WriteLine("Texture Information:");
            foreach (var texture in pak.Textures)
                PrintTexture(texture);
        }

        public static void PrintLODHeader(LODHeader header)
        {
            WriteLine($"LODHeader: Unknown=0x{header.Unknown:X8}, Count=0x{header.Count:X8}, C=0x{header.C:X8}");
        }


        public static void PrintTexture(Texture texture)
        {
            WriteLine($"Texture - Name: {texture.Header.FileName}, Offset: 0x{texture.Offset:X}, Length: {texture.Length}");
        }

        public static void PrintLODEntry(LODEntry entry)
        {
            WriteLine($"LODEntry: ObjectOffset=0x{entry.ObjectOffset:X8}, Filler1=0x{entry.Filler1:X8}, Flags=0x{entry.Flags:X8}, Unknown=0x{entry.Unknown:X8}, Filler2=[{string.Join(", ", entry.Filler2.Select(x => $"0x{x:X8}"))}]");
        }


        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }

       

    }
}
