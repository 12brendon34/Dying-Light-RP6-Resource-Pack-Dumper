using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpRP6
{
    //https://github.com/Microsoft/DirectXTex/blob/main/DirectXTex/DDS.h
    internal class DDS
    {
        public struct DDS_PIXELFORMAT
        {
            public UInt32 size;
            public UInt32 flags;
            public UInt32 fourCC;
            public UInt32 RGBBitCount;
            public UInt32 RBitMask;
            public UInt32 GBitMask;
            public UInt32 BBitMask;
            public UInt32 ABitMask;
        };

        public struct DDS_HEADER
        {
            public UInt32 size;
            public UInt32 flags;
            public UInt32 height;
            public UInt32 width;
            public UInt32 pitchOrLinearSize;
            public UInt32 depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
            public UInt32 mipMapCount;
            public UInt32[] reserved1;
            public DDS_PIXELFORMAT ddspf;
            public UInt32 caps;
            public UInt32 caps2;
            public UInt32 caps3;
            public UInt32 caps4;
            public UInt32 reserved2;
        };
        internal enum PixelFormatFlags
        {
            FOURCC = 0x00000004, //DDPF_FOURCC
            RGB = 0x00000040, //DDPF_RGB
            RGBA = 0x00000041, //DDPF_RGB|DDPF_ALPHAPIXELS
            LUMINANCE = 0x00020000, //DDPF_LUMINANCE
            LUMINANCEA = 0x00020001, //DDPF_LUMINANCE|DDPF_ALPHAPIXELS
            ALPHAPIXELS = 0x00000001, //DDPF_ALPHAPIXELS
            ALPHA = 0x00000002, //DDPF_ALPHA
            PAL8 = 0x00000020, //DDPF_PALETTEINDEXED8
            PAL8A = 0x00000021, //DDPF_PALETTEINDEXED8|DDPF_ALPHAPIXELS
            BUMPLUMINANCE = 0x00040000, //DDPF_BUMPLUMINANCE
            BUMPDUDV = 0x00080000, //DDPF_BUMPDUDV
            BUMPDUDVA = 0x00080001 //DDPF_BUMPDUDV|DDPF_ALPHAPIXELS
    }
        internal enum FourCCFormat
        {
            Dxt1 = 0x31545844,
            Dxt3 = 0x33545844,
            Dxt5 = 0x35545844
        }

        public static DDS_PIXELFORMAT GetPixelFormat(Util.TextureFormat textureFormat)
        {
            uint DDS_PIXELFORMAT_SIZE = 32;

            DDS_PIXELFORMAT dDS_PIXELFORMAT = new DDS_PIXELFORMAT();
            dDS_PIXELFORMAT.size = DDS_PIXELFORMAT_SIZE;
            dDS_PIXELFORMAT.fourCC = 0;

            switch (textureFormat)
            {
                case Util.TextureFormat.DXT1:
                    dDS_PIXELFORMAT.fourCC = (uint)FourCCFormat.Dxt1;
                    break;
                case Util.TextureFormat.DXT3:
                    dDS_PIXELFORMAT.fourCC = (uint)FourCCFormat.Dxt3;
                    break;
                case Util.TextureFormat.DXT5:
                    dDS_PIXELFORMAT.fourCC = (uint)FourCCFormat.Dxt5;
                    break;
            }

            switch (textureFormat)
            {
                case Util.TextureFormat.DXT1:
                case Util.TextureFormat.DXT3:
                case Util.TextureFormat.DXT5:
                    dDS_PIXELFORMAT.flags = (int)PixelFormatFlags.FOURCC;
                    dDS_PIXELFORMAT.RGBBitCount = 0;

                    dDS_PIXELFORMAT.RBitMask = 0;
                    dDS_PIXELFORMAT.GBitMask = 0;
                    dDS_PIXELFORMAT.BBitMask = 0;
                    dDS_PIXELFORMAT.ABitMask = 0;
                    break;
                case Util.TextureFormat.A8R8G8B8:
                    dDS_PIXELFORMAT.flags = (int)PixelFormatFlags.RGBA;
                    dDS_PIXELFORMAT.RGBBitCount = 32;

                    dDS_PIXELFORMAT.RBitMask = 0x00ff0000;
                    dDS_PIXELFORMAT.GBitMask = 0x0000ff00;
                    dDS_PIXELFORMAT.BBitMask = 0x000000ff;
                    dDS_PIXELFORMAT.ABitMask = 0xff000000;
                    break;

            }
            return dDS_PIXELFORMAT;
        }
    }
}