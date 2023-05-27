using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBINTool
{
    /***
     * Takes a CBINFile and returns an array of bytes ready to be written to disk.
     * Performs serialization into the CBIN format and encrypts with a provided key.
    */
    public class CBinSerializer
    {
        
        public static CBINFile SerializeToBytes(CBINFile cbinFile)
        {
            List<byte> serializedBytes = new List<byte>();
            // 1) create header bytes
            cbinFile.Parsed.header.Identifier = new byte[4];
            cbinFile.Parsed.header.Identifier[0] = 0x43;
            cbinFile.Parsed.header.Identifier[1] = 0x42;
            cbinFile.Parsed.header.Identifier[2] = 0x49;
            cbinFile.Parsed.header.Identifier[3] = 0x4e;
            cbinFile.Parsed.header.TextTokenCount = cbinFile.TextTokens.Length - 1; //even though the table starts at 1, it still has the correct count.
            cbinFile.Parsed.header.TextTokenOffset = 20 + 4; //20 for header, 4 for section count, + sections * 8 + keys * 8 + values * 8 + 8 for every double stop.;

            cbinFile.Parsed.header.Unknown1 = 0x8bf; // not sure what this is, but this will work for "blade.des".
            // The following bytes are always the same in cbins, so let's just fake it.
            cbinFile.Parsed.header.Unknown2 = new byte[4];
            cbinFile.Parsed.header.Unknown2[0] = 0xCE;
            cbinFile.Parsed.header.Unknown2[1] = 0x77;
            cbinFile.Parsed.header.Unknown2[2] = 0xE1;
            cbinFile.Parsed.header.Unknown2[3] = 0x01;

            List<CBINSection> sections = new List<CBINSection>();
            List<CBINKey> keys = new List<CBINKey>();
            List<CBINValue> values = new List<CBINValue>();
            for (int i = 0; i < cbinFile.Parsed.Sections.Count; i++)
            {
                cbinFile.Parsed.header.TextTokenOffset += 8;
                sections.Add(cbinFile.Parsed.Sections[i]);
                if (cbinFile.Parsed.Sections[i].keys == null || cbinFile.Parsed.Sections[i].keys.Count == 0) continue;
                for (int j = 0; j < cbinFile.Parsed.Sections[i].keys.Count; j++)
                {
                    keys.Add(cbinFile.Parsed.Sections[i].keys[j]);
                    cbinFile.Parsed.header.TextTokenOffset += 8;
                    for ( int k = 0; k < cbinFile.Parsed.Sections[i].keys[j].values.Count; k++)
                    {
                        cbinFile.Parsed.header.TextTokenOffset += 8;
                        values.Add(cbinFile.Parsed.Sections[i].keys[j].values[k]);
                    }
                }
                cbinFile.Parsed.header.TextTokenOffset += 8; // for the stop block at the end of this set of keys.
                keys.Add(new CBINKey()); // this is the stop block.
            }

            // actually, the decryptedBytes don't have the full header.
            //serializedBytes.AddRange(Utils.StructureToByteArray(cbinFile.Parsed.header));

            // 2) create section bytes
            serializedBytes.AddRange(BitConverter.GetBytes(sections.Count)); // section count is 4 bytes.
            foreach( var section in sections)
            {
                serializedBytes.AddRange(BitConverter.GetBytes(section.TokenIndex));
                serializedBytes.AddRange(BitConverter.GetBytes(section.keys != null? section.keys.Count : 0));
            }

            // 3) create key bytes
            foreach (var key in keys)
            {
                serializedBytes.AddRange(BitConverter.GetBytes(key.TokenIndex));
                serializedBytes.AddRange(BitConverter.GetBytes(key.ChildCount));
            } // note the stop blocks should be added in automatically.

            // 4) create value bytes

            foreach( var value in values)
            {
                if ( value.Type == 2) // special case for floats
                    serializedBytes.AddRange(BitConverter.GetBytes(value.AsFloat));
                else
                    serializedBytes.AddRange(BitConverter.GetBytes(value.AsInt)); // this also works for text.
                serializedBytes.AddRange(BitConverter.GetBytes(value.Type));
            }
            // 5) create text table

            for( int i = 1; i < cbinFile.TextTokens.Length; i++)
            {
                serializedBytes.AddRange(Encoding.ASCII.GetBytes(cbinFile.TextTokens[i] + '\0'));
            }
            cbinFile.DecryptedData = serializedBytes.ToArray();
            return cbinFile;
        }

        public static byte[] SerializeCBINFile(CBINFile cbinFile, byte[] key)
        {
            byte[] headerBytes = Utils.StructureToByteArray(cbinFile.Parsed.header);

            // Combine the decrypted header bytes with the decrypted data
            byte[] encryptedData = Utils.XorEncrypt(cbinFile.DecryptedData, key);
            byte[] serializedData = new byte[headerBytes.Length + encryptedData.Length];
            Array.Copy(headerBytes, 0, serializedData, 0, headerBytes.Length);
            Array.Copy(encryptedData, 0, serializedData, headerBytes.Length, encryptedData.Length);
            return serializedData;
        }
        
    }
}
