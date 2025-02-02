using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DumpRP6.Util;

namespace DumpRP6
{
    internal class TextureHeader
    {
        public ushort Width;
        public ushort Height;
        public ushort Depth;
        public ushort Count;
        public ushort MipMapCount;
        public ushort Flags;
        public Util.TextureFormat Format;
        public readonly uint[] MipMapSizes = new uint[16];
        public void Deserialize(Stream input)
        {

            Width = Util.ReadValueU16(input);
            Height = Util.ReadValueU16(input);
            Depth = Util.ReadValueU16(input);
            Count = Util.ReadValueU16(input);
            MipMapCount = Util.ReadValueU16(input);
            Flags = Util.ReadValueU16(input);
            Format = (Util.TextureFormat)Util.ReadByte(input);

            for (uint i = 0; i < 16; i++)
            {
                this.MipMapSizes[i] = Util.ReadValueU32(input);
            }
        }
    }
}
