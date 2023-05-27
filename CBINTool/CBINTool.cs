using CBINTool;
using System;
using System.Collections.Generic;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: CBINTool [-k key] [-h] [-e] <file_path ...>");
            return;
        }

        bool printHeaderOnly = false;
        bool encryptMode = false;
        string keyString = "00782E7C7007C277E7803C17BE3803E1BBF3C01E0BDF9C01F05DF9E00F85EFCE";
        bool customKey = false;

        if (args[0].ToLower() == "-k")
        {
            customKey = true;
            keyString = args[1];
        }
        else if (args[0].ToLower() == "-h")
        {
            printHeaderOnly = true;
        }
        else if (args[0].ToLower() == "-e")
        {
            encryptMode = true;
        }

        byte[] key = Utils.HexStringToByteArray(keyString);

        for (int i = customKey ? 2 : 0; i < args.Length; i++)
        {
            try
            {
                string filePath = args[i];
                string directory = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileName(filePath);

                if (string.IsNullOrEmpty(directory))
                {
                    // No directory specified, use current directory
                    directory = Directory.GetCurrentDirectory();
                }

                // Get files matching the search pattern
                string[] files = Directory.GetFiles(directory, fileName);
                if (printHeaderOnly) {
                    printCbinheaders(files);
                } else if (encryptMode) {
                    foreach (var file in files)
                    { 
                        var fileText = File.ReadAllText(file);
                        var cbinfile = Parser.FromText(fileText);
                        CBINPrettyPrinter.PrintHeader(cbinfile.Parsed);
                        // if there was a problem with the text version, write the decrypted version. 
                        var outputFilePath = file + "_reconstructed.bin";
                        File.WriteAllBytes(outputFilePath, cbinfile.DecryptedData);
                        var encryptedFile = CBinSerializer.SerializeCBINFile(cbinfile, key);
                        outputFilePath = file + "_encrypted.bin";
                        File.WriteAllBytes(outputFilePath, encryptedFile);
                    }
                } else foreach (string file in files) DecryptToText(key, file);

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }

    private static void printCbinheaders(string[] files)
    {
        List<string> nonCbins = new List<string>();
        foreach (string file in files)
        {

            if (!PrintCBINHeader(file))
            {
                nonCbins.Add(file);
            }
        }
        if (nonCbins.Count > 0)
        {
            Console.WriteLine("The following files were not cbins:");
            var indent = "    ";
            foreach (var filepath in nonCbins)
            {
                Console.WriteLine($"{indent}{filepath}");
            }
        }
    }

    private static bool PrintCBINHeader(string filePath)
    {
        try
        {
            byte[] encryptedData = File.ReadAllBytes(filePath);
            if (! (Parser.IsCBIN(encryptedData))) return false;
            // Strip the first 20 bytes (header)
            byte[] header = new byte[ 20];
            Array.Copy(encryptedData, 0, header, 0, header.Length);


            string headerBytes = BitConverter.ToString(header);
            Console.WriteLine($"{headerBytes} :  {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CBIN header for {filePath}: " + ex.Message);
            return false;
        }
    }



    private static void DecryptToText(byte[] key, string file)
    {
        byte[] encryptedData = File.ReadAllBytes(file);

        if (!Parser.IsCBIN(encryptedData))
        {
            Console.WriteLine($"Not a CBIN file: {file}");
            return;
        }
        var cbinFile = Parser.Parse(encryptedData, key);
        string outputFilePath;


        if ( cbinFile.Parsed.Success)
        {
            // write the text version
            outputFilePath = file + ".txt";
            using (StreamWriter writer = new StreamWriter(outputFilePath, false))
            {
                CBINPrettyPrinter.ToText(cbinFile, writer);
            }
            Console.WriteLine("Decryption/Parsing completed. Text file saved as: " + outputFilePath);
            //return;
        }

        // if there was a problem with the text version, write the decrypted version. 
        outputFilePath = file + "_decrypted.bin";
        File.WriteAllBytes(outputFilePath, cbinFile.DecryptedData);
       // Console.WriteLine("Parsing failed. Decrypted binary file saved as: " + outputFilePath);

    }

}
