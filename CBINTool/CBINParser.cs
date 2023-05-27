using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CBINTool
{
    public class Parser
    {
        public static byte[] CBINMagic = { 0x43, 0x42, 0x49, 0x4e };

        public static bool IsCBIN(byte[] encryptedData)
        {
            byte[] headerBytes = new byte[4];
            Array.Copy(encryptedData, 0, headerBytes, 0, 4);
            return headerBytes.SequenceEqual(CBINMagic);
        }
        public static CBINFile Parse( byte[] encryptedData, byte[] key)
        {
            var cbinFile= new CBINFile();
            cbinFile.Parsed = new CBINParsed();
            cbinFile.Parsed.Success = true; // later code will invalidate this if needed.
            byte[] headerBytes = new byte[Marshal.SizeOf<UnencryptedHeader>()];
            Array.Copy(encryptedData, 0, headerBytes, 0, headerBytes.Length);

            GCHandle headerHandle = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            cbinFile.Parsed.header = Marshal.PtrToStructure<UnencryptedHeader>(headerHandle.AddrOfPinnedObject());
            headerHandle.Free();
            

            // Strip the first 20 bytes (header)
            byte[] strippedData = new byte[encryptedData.Length - 20];
            Array.Copy(encryptedData, 20, strippedData, 0, strippedData.Length);

            cbinFile.DecryptedData = Utils.XorDecrypt(strippedData, key);

            // read in text tokens from the bottom of the file
            // Note: for some reason, the indexes used later in the file to refer to these tokens starts at 1 instead of 0, so I just cheat
            // a bit and move all the items over by 1. This means that entry 0 is null. Oh well. It works.
            cbinFile.TextTokens = new string[cbinFile.Parsed.header.TextTokenCount +1];
            var textOffset = cbinFile.Parsed.header.TextTokenOffset - 20; // 20 bytes are stripped off before decryption so we must account for that.
            for (int i = 1; i < cbinFile.Parsed.header.TextTokenCount+1; i++)
            {
                cbinFile.TextTokens[i] = ReadNullTerminatedString(cbinFile.DecryptedData, textOffset);
                textOffset += cbinFile.TextTokens[i].Length +1; 
            }


            int entryCount = 0; // every entry, including top level ones.
            cbinFile.Parsed.NumberOfSections = BitConverter.ToInt32(cbinFile.DecryptedData, 0);
            List<CBINSection> topLevelSections = new List<CBINSection>();
            var offset = 4; // skip first int as that is the count of text tokens.
            // so we have to make the top level entries.
            for (int i = 0; i < cbinFile.Parsed.NumberOfSections; i++)
            {
                var newEntry = new CBINSection
                {
                    Title = cbinFile.TextTokens[BitConverter.ToInt32(cbinFile.DecryptedData, offset) ],
                    TokenIndex = BitConverter.ToInt32(cbinFile.DecryptedData, offset),
                    ChildCount = BitConverter.ToInt32(cbinFile.DecryptedData, offset + 4)
                };
                topLevelSections.Add(newEntry);
                entryCount++;
                offset += 8;
            }
            Console.WriteLine($"After Sections: {(offset + 20):X}");
            // now fill in the keys.
            for (int i = 0; i < cbinFile.Parsed.NumberOfSections; i++)
            {
                if (topLevelSections[i].ChildCount <= 0) // don't consume keys for sections with no children.
                {
                    var section = topLevelSections[i];
                    section.keys = new List<CBINKey>();
                    topLevelSections[i] = section;
                    continue;
                }
                // Parse the CBIN entries
                List<CBINKey> entries = new List<CBINKey>();
                var hitStopBlock = false; // after the list of entries are 8 bytes of 0. I call these a stop block.
                while (!hitStopBlock)
                {

                    try
                    {
                        var tokenIndex = BitConverter.ToInt32(cbinFile.DecryptedData, offset);
                        var title = "__OUT_OF_BOUNDS__"+tokenIndex;
                        if (tokenIndex >= 0 && tokenIndex < cbinFile.TextTokens.Length)
                            title = cbinFile.TextTokens[tokenIndex];
                        var newEntry = new CBINKey
                        {
                            Title = title,
                            TokenIndex = BitConverter.ToInt32(cbinFile.DecryptedData, offset),
                            ChildCount = BitConverter.ToInt32(cbinFile.DecryptedData, offset + 4),
                            values = new List<CBINValue>()
                        };
                        if (newEntry.TokenIndex == 0)
                        {
                            hitStopBlock = true;
                        }
                        else
                        {
                            entryCount++;
                            entries.Add(newEntry);
                        }
                        offset += 8;
                    }
                    catch (Exception ex)
                    {
                        cbinFile.Parsed.Success = false;
                        Console.WriteLine($"An error occurred while processing CBIN entry: {ex.Message}");
                    }
                }
                var group = topLevelSections[i];
                group.keys = entries;
                topLevelSections[i] = group;
            }
            // Update the CBINFile struct with the parsed data
            cbinFile.Parsed.Sections = topLevelSections;
            cbinFile.Parsed.NumberOfEntries = entryCount;
            Console.WriteLine($"After Keys: {(offset + 20):X}");
            // now we need to go through the value entries and attach them to the right place in the tree.

            foreach (var section in topLevelSections)
            {
                for(int keynum = 0; keynum < section.keys.Count; keynum++)
                {
                    var values = new List<CBINValue>();
                    for(int i = 0; i < section.keys[keynum].ChildCount; i++)
                    {
                        try
                        {
                            var rawBytes = new byte[4];
                            Array.Copy(cbinFile.DecryptedData, offset, rawBytes, 0, rawBytes.Length);
                            var newValue = new CBINValue
                            {
                                RawBytes = rawBytes,
                                AsInt = BitConverter.ToInt32(cbinFile.DecryptedData, offset),
                                Type = BitConverter.ToInt32(cbinFile.DecryptedData, offset + 4)
                            };
                            if (newValue.Type == 4) // this is a text table offset.
                            {
                                if (newValue.AsInt < cbinFile.TextTokens.Length && newValue.AsInt > 0)
                                    newValue.AsText = cbinFile.TextTokens[newValue.AsInt];
                                else
                                    newValue.AsText = "__OUT_OF_BOUNDS__" + newValue.AsInt;
                            }
                            if (newValue.Type == 2) // this is floating point?
                            {
                                newValue.AsFloat = BitConverter.ToSingle(cbinFile.DecryptedData, offset);
                            }
                            values.Add(newValue);
                            offset += 8;
                        }
                        catch (Exception ex)
                        {
                            cbinFile.Parsed.Success = false;
                            Console.WriteLine($"An error occurred while processing CBIN value: {ex.Message}");
                        }

                    }
                    var keyvalues = section.keys[keynum];
                    keyvalues.values = values;
                    section.keys[keynum] = keyvalues;
                }
                
            }
            Console.WriteLine($"After Values: {(offset + 20):X}");
            return cbinFile;
        }

        public static string ReadNullTerminatedString(byte[] data, int offset)
        {
            int length = Array.IndexOf<byte>(data, 0, offset) - offset;
            if (length < 0)
                length = data.Length - offset;

            return Encoding.ASCII.GetString(data, offset, length);
        }

        public static CBINFile FromText(string text)
        {
            CBINFile cbinFile = new CBINFile();
            cbinFile.Parsed = new CBINParsed();
            cbinFile.Parsed.Success = true;
            
            List<CBINSection> sections = new List<CBINSection>();
            CBINSection currentSection = new CBINSection();
            List<CBINKey> currentKeys = new List<CBINKey>();
            List<string> tokens = new List<string>();
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    // Section header
                    string sectionTitle = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    currentSection = new CBINSection
                    {
                        Title = sectionTitle,
                        keys = new List<CBINKey>()
                    };
                    sections.Add(currentSection);
                }
                else
                {
                    // Key-value pair
                    string[] parts = trimmedLine.Split('=');
                    if (parts.Length >= 2)
                    {
                        string key = parts[0].Trim();
                        string[] valueTokens = parts[1].Split(',');

                        CBINKey cbinKey = new CBINKey
                        {
                            Title = key.Trim(),
                            TokenIndex = -1,
                            ChildCount = valueTokens.Length,
                            values = new List<CBINValue>()
                        };


                        for (int i = 0; i < valueTokens.Length; i++)
                        {
                            string valueToken = valueTokens[i].Trim();
                            CBINValue cbinValue = new CBINValue
                            {
                                RawBytes = new byte[4]
                            };

                            if (int.TryParse(valueToken, out int intValue))
                            {
                                cbinValue.AsInt = intValue;
                                cbinValue.Type = 1; // Int type

                            }
                            else if (float.TryParse(valueToken, out float floatValue))
                            {
                                cbinValue.AsFloat = floatValue;
                                cbinValue.Type = 2; // Float type

                            }
                            else
                            {
                                cbinValue.AsText = valueToken;
                                cbinValue.Type = 4; // Text type

                            }

                            cbinKey.values.Add(cbinValue);

                        }
                        currentSection.keys.Add(cbinKey);

                        
                    }
                }
            }

            var tokenIndexDict = ExtractTextTokens(sections);
            cbinFile = SetTokenIndexes(cbinFile, sections, tokenIndexDict);
            cbinFile.Parsed.Sections = sections;
            cbinFile.Parsed.NumberOfSections = sections.Count;
            cbinFile.Parsed.NumberOfEntries = sections.Sum(section => section.keys?.Count ?? 0);
            cbinFile = CBinSerializer.SerializeToBytes(cbinFile); // create the 

            return cbinFile;
        }


        private static Dictionary<string, int> ExtractTextTokens(List<CBINSection> sections)
        {
            Dictionary<string, int> tokenIndexDict = new Dictionary<string, int>(); // Keeps track of unique string tokens and their indexes
            // we need the tokens to appear in the same order that they did in the original file, so we have to go through the structure breadth-first.

            int tokenIndex = 1; // the index starts from 1 in this format. Don't ask me, ask Novalogic!

            //1) section titles
            var keys = new List<CBINKey>();
            var values = new List<CBINValue>();
            foreach ( var section in sections)
            {
                tokenIndexDict.Add(section.Title, tokenIndex);
                tokenIndex++;
                if (section.keys == null) continue;
                foreach (var key in section.keys)
                {
                    keys.Add(key); // for later processing.
                }
                
            }

            //2) Keys from every section.
            foreach( var key in keys)
            {
                if ( !tokenIndexDict.ContainsKey(key.Title))
                {
                    tokenIndexDict.Add(key.Title, tokenIndex);
                    tokenIndex++;
                }
                foreach(var value in key.values)
                {
                    if (value.Type == 4) // only process ones with a text value.
                        values.Add(value);
                }

            }
            foreach (var value in values)
            {
                if (!tokenIndexDict.ContainsKey(value.AsText))
                {
                    tokenIndexDict.Add(value.AsText, tokenIndex);
                    tokenIndex++;
                }
            }
            return tokenIndexDict;
        }

        private static CBINFile SetTokenIndexes(CBINFile cbinFile, List<CBINSection> sections, Dictionary<string, int> tokenIndexDict)
        {
            // Create the TextTokens array and populate it with unique string tokens
            cbinFile.TextTokens = new string[tokenIndexDict.Count + 1]; // Add 1 to account for the 1-based indexing
            foreach (var kvp in tokenIndexDict)
            {
                int tokenIndex = kvp.Value;
                cbinFile.TextTokens[tokenIndex] = kvp.Key;
            }

            // Set the correct token indexes in the CBIN structs
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                section.TokenIndex = tokenIndexDict[section.Title];
                sections[i] = section;

                for (int j = 0; j < section.keys.Count; j++)
                {
                    var key = section.keys[j];
                    key.TokenIndex = tokenIndexDict[key.Title];
                    section.keys[j] = key;

                    for (int k = 0; k < key.values.Count; k++)
                    {

                        var value = key.values[k];
                        if (value.Type != 4) continue;
                        value.AsInt = tokenIndexDict[value.AsText];
                        key.values[k] = value;
                    }
                }
            }

            return cbinFile;
        }
    }
}
