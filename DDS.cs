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

        public static UInt32 DDS_FOURCC = 0x00000004; //DDPF_FOURCC
        public static UInt32 DDS_RGB = 0x00000040; //DDPF_RGB
        public static UInt32 DDS_RGBA = 0x00000041; //DDPF_RGB|DDPF_ALPHAPIXELS
        public static UInt32 DDS_LUMINANCE = 0x00020000; //DDPF_LUMINANCE
        public static UInt32 DDS_LUMINANCEA = 0x00020001; //DDPF_LUMINANCE|DDPF_ALPHAPIXELS
        public static UInt32 DDS_ALPHAPIXELS = 0x00000001; //DDPF_ALPHAPIXELS
        public static UInt32 DDS_ALPHA = 0x00000002; //DDPF_ALPHA
        public static UInt32 DDS_PAL8 = 0x00000020; //DDPF_PALETTEINDEXED8
        public static UInt32 DDS_PAL8A = 0x00000021; //DDPF_PALETTEINDEXED8|DDPF_ALPHAPIXELS
        public static UInt32 DDS_BUMPLUMINANCE = 0x00040000; //DDPF_BUMPLUMINANCE
        public static UInt32 DDS_BUMPDUDV = 0x00080000; //DDPF_BUMPDUDV
        public static UInt32 DDS_BUMPDUDVA = 0x00080001; //DDPF_BUMPDUDV|DDPF_ALPHAPIXELS

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

    }
}