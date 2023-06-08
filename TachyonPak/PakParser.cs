using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


/*** Tachyon: The Fringe PAK parser
 * 
 *  PAK files are the 3d objects (3dO sections) Packaged together with the textures and LOD details for Tachyon.
 *  The information for this has been cobbled together from various sources including my own investigations.
 *  These files seem to have a spiritual lineage to the 3di files used in other NovaLogic games like Black Hawk Down, but are their own thing.
 *  It's worth noting that BHD came out after Tachyon, so its file format certainly isn't the "parent", 
 *  but it is interesting that they are similar in several respects. BHD has official mod support with tools, so it's useful
 *  to compare the formats to glean information about Novalogic's thinking with respect to format design. 
 *  It is often the case that even if the tools themselves are not reused, the ideas that go into making them often have
 *  staying power in the "culture" of a company.
 *  
 *  It is interesting to me that the PFF format is essentially unchanged between TTF and BHD. If it ain't broke, don't fix it!
 *  
 * As of May 15, here's what I have figured out:
 * 
 * Each PAK file includes model and texture information about a single model, but it includes every LOD for that model.
 * During this stage of 3d games, hardware was still modest enough that effectively managing the polygon count was essential 
 * to performance. I mean, it still is, but we are talking about an order of magnitude fewer polygons, so each one counted.
 * 
 * Each model would therefore be created in up to 4 levels of detail, and the model and textures would be swapped out as the model
 * receded into the background, freeing up precious texture memory and polygons for models closer to the player.
 * 
 * The PAK has several sections, starting with a header that includes information about where to find the textures in the file, 
 * how many LODs there are, the name of the model and several other essential bits (most of which I have not figured out)
 * I called this the Header. It is 96 bytes.
 * 
 * After, there are 8 bytes for each LOD. I don't know what they are for yet. I called them MysteryLODBytes.
 * 
 * Next there is a short section for each of the LODs that includes information about how many subobjects are in each LOD
 * I have called this the LOD Header, and each one is 12 bytes.
 * 
 * After that there are 3DO1 objects which seem to be the actual models. There are 1 for each of the counts in each LOD, I think.
 * I still have to work on their header format as well as the format of the actual models.
 * 
 * Finally, there is the texture section. This is pointed to by a value in the PAK Header. Its "header" is a single integer which
 * contains the total count of all textures. Each texture has a short header (28 bytes) that includes the filename and a 
 * truncated version of the PCX header, including size and a few other essential things. 
 * Following that header is the body of the PCX data, so if you want to export the textures it is necessary 
 * to prepend a generic PCX header with the correct size etc derived from the truncated header.
 * 
 * TODO: work out the format of the 3DO1 objects such that we can convert them to a format we can view in other software.
 * There is some evidence that they are derived from 3dsMax .ASE files.
 * 
*/
public class PAKParser
{
    public PAKFile ParsePAKFile(string filePath)
    {
        PAKFile pakFile = new PAKFile();

        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] headerBytes = new byte[Marshal.SizeOf<PAKHeader>()];
            stream.Read(headerBytes, 0, headerBytes.Length);

            GCHandle headerHandle = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            pakFile.Header = Marshal.PtrToStructure<PAKHeader>(headerHandle.AddrOfPinnedObject());
            headerHandle.Free();

            pakFile.MysteryLODBytes = new byte[pakFile.Header.noOfLOD * 8];
            // Read 8 bytes extra per LOD
            for (int i = 0; i < pakFile.Header.noOfLOD * 8; i++)
            {
                stream.Seek(Marshal.SizeOf<PAKHeader>() + i, SeekOrigin.Begin);
                pakFile.MysteryLODBytes[i] = (byte)stream.ReadByte();
            }

            int infoListOffset = pakFile.Header.infoList; // this is always 0x60, so not sure why we bother.
            int lodEntryListOffset = (pakFile.Header.noOfLOD * 8) +Marshal.SizeOf<PAKHeader>(); // Header size is variable, with 8 bytes per LOD extra at the end.
            int textureListOffset = pakFile.Header.textureStart;
            int textureListEnd = pakFile.Header.textureEnd;

           

            int lodEntrySize = Marshal.SizeOf<LODEntry>();
            pakFile.LODs = new LOD[pakFile.Header.noOfLOD];
            int currentoffset = lodEntryListOffset;
            // For each LOD, parse the LOD Info header and then the LOD entries according to the LOD info.
            
            for (int i = 0; i < pakFile.Header.noOfLOD; i++)
            {
                byte[] lodheaderBytes = new byte[Marshal.SizeOf<LODHeader>()];
                stream.Seek(currentoffset, SeekOrigin.Begin);
                stream.Read(lodheaderBytes, 0, lodheaderBytes.Length);
                currentoffset += Marshal.SizeOf<LODHeader>();
                GCHandle lodheaderHandle = GCHandle.Alloc(lodheaderBytes, GCHandleType.Pinned);
                pakFile.LODs[i].Header = Marshal.PtrToStructure<LODHeader>(lodheaderHandle.AddrOfPinnedObject());
                lodheaderHandle.Free();

                pakFile.LODs[i].Entries = new LODEntry[pakFile.LODs[i].Header.Count];
                pakFile.LODs[i]._3DObjects = new _3DObject[pakFile.LODs[i].Header.Count];
                for (int j = 0; j < pakFile.LODs[i].Header.Count; j++)
                {
                    stream.Seek(currentoffset + (j * lodEntrySize), SeekOrigin.Begin);

                    byte[] objectListBytes = new byte[lodEntrySize];
                    stream.Read(objectListBytes, 0, objectListBytes.Length);

                    var objectListHandle = GCHandle.Alloc(objectListBytes, GCHandleType.Pinned);
                    pakFile.LODs[i].Entries[j] = new LODEntry(); 
                    pakFile.LODs[i].Entries[j] = Marshal.PtrToStructure<LODEntry>(objectListHandle.AddrOfPinnedObject());
                    objectListHandle.Free();

                    // parse 3do for this entry

                    pakFile.LODs[i]._3DObjects[j] = Parse3dObject(stream, pakFile.LODs[i].Entries[j].ObjectOffset);

                }
                currentoffset += pakFile.LODs[i].Header.Count * lodEntrySize;

                
            }


           

           

            // Parsing TextureList
            // 1 determine the number of textures
            stream.Seek(textureListOffset, SeekOrigin.Begin);


            int textureCount = stream.ReadByte(); // Read the number of textures
            int textureListSize = Marshal.SizeOf<TextureHeader>();

            List<int> textureOffsets = new List<int>();

            // I haven't found a size for the PCX file, so I am just doing it the hard way
            // and searching for the next PCX file and then backing off.
            while (textureOffsets.Count < textureCount && stream.Position < stream.Length)
            {
                string textureFilename = ReadNullTerminatedString(stream);
                if (textureFilename.EndsWith(".PCX", StringComparison.OrdinalIgnoreCase))
                {
                    textureOffsets.Add((int)stream.Position - textureListOffset - Math.Min(textureFilename.Length +1, 13)); // +1 for null terminated string.
                }
            }

            //2 Read texture headers
            pakFile.Textures = new Texture[textureOffsets.Count];

            for (int i = 0; i < textureOffsets.Count; i++)
            {
                int textureHeaderOffset = textureListOffset + textureOffsets[i];
                stream.Seek(textureHeaderOffset, SeekOrigin.Begin);

                byte[] textureHeaderBytes = new byte[textureListSize];
                stream.Read(textureHeaderBytes, 0, textureHeaderBytes.Length);

                GCHandle textureHeaderHandle = GCHandle.Alloc(textureHeaderBytes, GCHandleType.Pinned);
                pakFile.Textures[i].Header = Marshal.PtrToStructure<TextureHeader>(textureHeaderHandle.AddrOfPinnedObject());
                pakFile.Textures[i].Offset = textureOffsets[i] + textureListOffset;
                pakFile.Textures[i].Length = i < textureOffsets.Count -1 ? textureOffsets[i + 1] - textureOffsets[i] : pakFile.Header.textureEnd - pakFile.Header.textureStart - textureOffsets[i];
                textureHeaderHandle.Free();

                //3 Read texture data
                int textureDataOffset = pakFile.Textures[i].Offset + Marshal.SizeOf<TextureHeader>();
                int textureDataLength = pakFile.Textures[i].Length - Marshal.SizeOf<TextureHeader>();
                pakFile.Textures[i].Data = new byte[textureDataLength];

                stream.Seek(textureDataOffset, SeekOrigin.Begin);
                stream.Read(pakFile.Textures[i].Data, 0, textureDataLength);
            }
        }

        return pakFile;
    }

    private _3DObject Parse3dObject(FileStream stream, int objectOffset)
    {

        int currentOffset = objectOffset;
        var result = new _3DObject();

        stream.Seek(objectOffset, SeekOrigin.Begin);
        var objectListBytes = new byte[Marshal.SizeOf<_3DOHeader>()];
        stream.Read(objectListBytes, 0, objectListBytes.Length);
        var objectListHandle = GCHandle.Alloc(objectListBytes, GCHandleType.Pinned);
        result.header = new _3DOHeader();
        result.header = Marshal.PtrToStructure<_3DOHeader>(objectListHandle.AddrOfPinnedObject());
        objectListHandle.Free();

        if (result.header.Identifier != "3DO") // stop processing.
            return result;

        currentOffset += Marshal.SizeOf<_3DOHeader>();

        // texture entries
        result.textures = new TextureEntry[result.header.numTextures];
        for (int i = 0; i < result.header.numTextures; i++)
        {
            stream.Seek(currentOffset , SeekOrigin.Begin);
            var textureEntryBytes = new byte[Marshal.SizeOf<TextureEntry>()];
            stream.Read(textureEntryBytes, 0, textureEntryBytes.Length);
            var textureEntryHandle = GCHandle.Alloc(textureEntryBytes, GCHandleType.Pinned);
            result.textures[i] = new TextureEntry();
            result.textures[i] = Marshal.PtrToStructure<TextureEntry>(textureEntryHandle.AddrOfPinnedObject());
            textureEntryHandle.Free();
            currentOffset += Marshal.SizeOf<TextureEntry>();
        }

        // verts
        result.vertices = new Vertex[result.header.numVertices];
        for (int i = 0; i < result.header.numVertices; i++)
        {
            stream.Seek(currentOffset, SeekOrigin.Begin);
            var vertexBytes = new byte[Marshal.SizeOf<Vertex>()];
            stream.Read(vertexBytes, 0, vertexBytes.Length);
            var vertexHandle = GCHandle.Alloc(vertexBytes, GCHandleType.Pinned);
            result.vertices[i] = new Vertex();
            result.vertices[i] = Marshal.PtrToStructure<Vertex>(vertexHandle.AddrOfPinnedObject());
            vertexHandle.Free();
            currentOffset += Marshal.SizeOf<Vertex>();
        }

        // tris
        result.tris = new Triangle[result.header.numTriangles];
        for (int i = 0; i < result.header.numTriangles; i++)
        {
            stream.Seek(currentOffset, SeekOrigin.Begin);
            var triBytes = new byte[Marshal.SizeOf<Triangle>()];
            stream.Read(triBytes, 0, triBytes.Length);
            var triHandle = GCHandle.Alloc(triBytes, GCHandleType.Pinned);
            result.tris[i] = new Triangle();
            result.tris[i] = Marshal.PtrToStructure<Triangle>(triHandle.AddrOfPinnedObject());
            triHandle.Free();
            currentOffset += Marshal.SizeOf<Triangle>();
        }

        // normals or whatever
        result.normals_or_whatever = new Normal[result.header.numNormals];
        for (int i = 0; i < result.header.numNormals; i++)
        {
            stream.Seek(currentOffset, SeekOrigin.Begin);
            var normalBytes = new byte[Marshal.SizeOf<Normal>()];
            stream.Read(normalBytes, 0, normalBytes.Length);
            var normalHandle = GCHandle.Alloc(normalBytes, GCHandleType.Pinned);
            result.normals_or_whatever[i] = new Normal();
            result.normals_or_whatever[i] = Marshal.PtrToStructure<Normal>(normalHandle.AddrOfPinnedObject());
            normalHandle.Free();
            currentOffset += Marshal.SizeOf<Normal>();
        }

        return result;
    }


    /** The PCX files in the texture section have their headers stripped off, 
     * so we must reconstruct the header and prepend it.
     * Note that the palette data is in the "optional" 768 byte extended palette footer of the PCX.
     * That's why we can get away with filling the EGA palette in the header with 0s.
     * Interestingly GIMP opens these up no problem, but InfranView sees them as Greyscale.
     * 
    */
    public static void ExtractTextures(PAKFile pakFile, string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        for (int i = 0; i < pakFile.Textures.Length; i++)
        {
            Texture texture = pakFile.Textures[i];
            string fileName = Path.Combine(outputDirectory, texture.Header.FileName);

            // Create the PCX header
            PCXHeader pcxHeader = new PCXHeader();
            pcxHeader.Manufacturer = 10; // Set manufacturer to 10 (ZSoft Corporation)
            pcxHeader.Version = 5; // Set version to 5 (PC Paintbrush 3.0)
            pcxHeader.Encoding = 1; // Set encoding to 1 (Run-Length Encoding)
            pcxHeader.BitsPerPixel = 8; // Set bits per pixel to 8 (256 colors)
            pcxHeader.XMin = 0;
            pcxHeader.YMin = 0;
            pcxHeader.XMax = (short)(texture.Header.Width - 1);
            pcxHeader.YMax = (short)(texture.Header.Height - 1);
            pcxHeader.HRes = 100; // Set horizontal resolution to 100 dpi
            pcxHeader.VRes = 100; // Set vertical resolution to 100 dpi
            pcxHeader.Palette = new byte[48]; // Fill the palette with 0 (black)
            pcxHeader.Reserved = 0;
            pcxHeader.ColorPlanes = 1; // Set number of color planes to 1
            pcxHeader.BytesPerLine = (short)(texture.Header.Width); // Set bytes per line
            pcxHeader.PaletteInfo = 1; // Set palette info to 1 (Color/BW)
            pcxHeader.Filler = new byte[58]; // Fill the filler with 0

            // Marshal the PCX header to a byte array
            int headerSize = Marshal.SizeOf<PCXHeader>();
            byte[] pcxHeaderBytes = new byte[headerSize];
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            Marshal.StructureToPtr(pcxHeader, headerPtr, false);
            Marshal.Copy(headerPtr, pcxHeaderBytes, 0, headerSize);
            Marshal.FreeHGlobal(headerPtr);

            // Combine the PCX header and texture data
            byte[] pcxData = new byte[headerSize + texture.Data.Length];
            Array.Copy(pcxHeaderBytes, pcxData, headerSize);
            Array.Copy(texture.Data, 0, pcxData, headerSize, texture.Data.Length);

            // Write the PCX data to the file
            File.WriteAllBytes(fileName, pcxData);
        }
    }


    private static string ReadNullTerminatedString(Stream stream)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            bool isStartValid = false;

            int value;
            while ((value = stream.ReadByte()) != 0 && (char.IsLetterOrDigit((char)value) || value == '.' || value == '_'))
            {
                if (!isStartValid)
                {
                    // Skip characters until a letter or underscore is found
                    if ((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z') || value == '_')
                    {
                        isStartValid = true;

                    } else continue;
                }
                memoryStream.WriteByte((byte)value);
            }

            return Encoding.ASCII.GetString(memoryStream.ToArray());
        }
    }




}
