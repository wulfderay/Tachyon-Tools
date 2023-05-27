using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CBINTool
{

    

    public struct CBINSection
    {
        public string Title;
        public int TokenIndex;
        public int ChildCount;
        public List<CBINKey> keys;
    }
    public struct CBINKey
    {
        public string Title;
        public int TokenIndex;
        public int ChildCount;
        public List<CBINValue> values;
    }

    public struct CBINValue
    {
        public int AsInt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte [] RawBytes;
        public int Type;
        public string AsText;
        public float AsFloat;
    }
   

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct CBINParsed
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public UnencryptedHeader header;
        public int NumberOfSections;
        public List<CBINSection> Sections;
        public object NumberOfEntries { get; internal set; }
        public bool Success;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct UnencryptedHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte [] Identifier;
        public int TextTokenOffset;
        public int Unknown1; // maybe the offset for the values?
        public int TextTokenCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte [] Unknown2;
    }

    public struct CBINFile
    {
        public CBINParsed Parsed;
        public string[] TextTokens;
        public byte[] DecryptedData { get; internal set; }
    }
}
