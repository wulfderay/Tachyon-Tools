using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct PAKFile
{
    public PAKHeader Header;
    public byte[] MysteryLODBytes;
    public LOD[] LODs;
    public Texture[] Textures;

}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct PAKHeader
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
    public string Identifier;
    public int Version;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string Name;
    public int Unknown;
    public int noOfLOD;  // Number of Levels of Detail (LOD)
    public int Unknown2;
    public int Filler;
    public int infoList;
    public int textureStart;
    public int textureEnd;
    public int Filler2;
    public int Unknown3;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Filler3;
    // the header can actually be larger or smaller based on the LOD. for each LOD there is an additional 8 bytes.
    // so a 1LOD header is 74h (116 bytes) but a 4LOD header is 8ch (140 bytes)
    // this header should capture the basic header (0LOD), then we should skip (LOD) * 8  more bytes.
}

public struct LOD
{
    public LODHeader Header;
    public LODEntry[] Entries;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LODHeader
{
    public int Unknown;
    public int Count;
    public int C; // this always seems to be 0xch. This is used as a guard value in many places in this format.
    // The PCX format specifies that 0xch be used to indicate that there exists a palette footer, but
    // hilariously the textures do NOT use it in that place. :shrugs:
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LODEntry
{
    public int ObjectOffset; 
    public int Filler1;
    public int Flags;
    public int Unknown;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    public int[] Filler2;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct Texture
{
    public int Offset;
    public int Length;
    public TextureHeader Header;
    public byte[] Data;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct TextureHeader
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
    public string FileName;
    public byte Encoding; //RLE or not. Not 100% certain that this is what this is.
    public byte Bitsperplane;
    public byte Reserved1; // and by reserved I mean I have no idea.
    public int Width;
    public int Height;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Filler; // not sure what this is. Sometimes there is info here.
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PCXHeader // just a generic PCX header, for use in reconstructing the textures. Not specific to TTF
{
    public byte Manufacturer;
    public byte Version;
    public byte Encoding;
    public byte BitsPerPixel;
    public short XMin;
    public short YMin;
    public short XMax;
    public short YMax;
    public short HRes;
    public short VRes;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
    public byte[] Palette;
    public byte Reserved;
    public byte ColorPlanes;
    public short BytesPerLine;
    public short PaletteInfo;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 58)]
    public byte[] Filler;
}

