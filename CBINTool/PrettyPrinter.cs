using System;
using System.IO;
using System.Linq;

namespace CBINTool
{
    public static class CBINPrettyPrinter
    {
        public static void PrintHeader(CBINParsed header)
        {
            Console.WriteLine("=== CBIN Header ===");
           // Console.WriteLine($"Unencrypted Header Bytes: {BitConverter.ToString(header.UnencryptedHeaderBytes)}");
            Console.WriteLine($"Number of Groups: {header.NumberOfSections}");
            Console.WriteLine($"Number of Entries: {header.NumberOfEntries}");
            Console.WriteLine($"Number of Text Tokens: {header.header.TextTokenCount}");
            Console.WriteLine("--- As Text ---");
            foreach(var section in header.Sections)
            {
                PrintCBINSection(section, 0);
                Console.WriteLine();
            }
            Console.WriteLine("===================");
        }

        public static void PrintCBINSection(CBINSection section, int indentLevel = 0, StreamWriter writer = null)
        {
            string indent = new string(' ', indentLevel * 4);
            WriteToOutput($"{indent}[{section.Title}]\n", writer);

            foreach (CBINKey key in section.keys)
            {
                indent = new string(' ', (indentLevel) * 4);
                WriteToOutput($"{indent}{key.Title} =", writer);

                for (int i = 0; i < key.values.Count; i++)
                {
                    CBINValue value = key.values[i];

                    switch (value.Type)
                    {
                        case 1: // int 
                            WriteToOutput($" {value.AsInt}", writer);
                            break;
                        case 2: // Float
                            WriteToOutput($" {value.AsFloat}", writer);
                            break;
                        case 4: // text index
                            WriteToOutput($" {value.AsText}", writer);
                            break;
                        default:
                            WriteToOutput($"{indent} Type: {value.Type} RawBytes: {string.Join(", ", value.RawBytes.Select(x => "0x" + x.ToString("X")))}", writer);
                            break;
                    }

                    if (i < key.values.Count - 1)
                        WriteToOutput(",", writer);
                    else
                        WriteToOutput("\n", writer);
                }
            }
        }

        private static void WriteToOutput(string text, StreamWriter writer)
        {
            if (writer != null)
                writer.Write(text);
            else
                Console.Write(text);
        }

        public static void ToText(CBINFile cbinFile, StreamWriter writer)
        {
            foreach (var section in cbinFile.Parsed.Sections)
            {
                PrintCBINSection(section, 0, writer);
                writer.WriteLine();
            }
        }

    }
}
