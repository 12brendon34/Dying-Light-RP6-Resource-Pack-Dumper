using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DumpRP6.Util;

namespace DumpRP6
{
    //https://github.com/Microsoft/DirectXTex/blob/main/DirectXTex/DDS.h
    internal class DDS
    {
        //Main DDS Structs
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

        //after DDS_HEADER
        public struct DDS_HEADER_DX10
        {
            public DXGI_FORMAT dxgiFormat;
            public D3D10_RESOURCE_DIMENSION resourceDimension;
            public uint miscFlag;
            public uint arraySize;
            public uint miscFlags2;
        }

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

        //I'll clean this up later
        public static DDS_PIXELFORMAT GetPixelFormat(Util.TextureFormat textureFormat)
        {
            uint DDS_PIXELFORMAT_SIZE = 32;

            DDS_PIXELFORMAT dDS_PIXELFORMAT = new DDS_PIXELFORMAT();
            dDS_PIXELFORMAT.size = DDS_PIXELFORMAT_SIZE;
            //use DX10 for anything suported
            dDS_PIXELFORMAT.fourCC = (uint)FourCCFormat.Dx10;
            dDS_PIXELFORMAT.flags = (int)PixelFormatFlags.FOURCC;

            dDS_PIXELFORMAT.RGBBitCount = 0;
            dDS_PIXELFORMAT.RBitMask = 0;
            dDS_PIXELFORMAT.GBitMask = 0;
            dDS_PIXELFORMAT.BBitMask = 0;
            dDS_PIXELFORMAT.ABitMask = 0;

            //DX9 supported DDS formats
            switch (textureFormat)
            {
                //Tested Functional
                //BitMasks
                case Util.TextureFormat.A4R4G4B4:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGBA;
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 16;
                    dDS_PIXELFORMAT.RBitMask = 0x0F00;
                    dDS_PIXELFORMAT.GBitMask = 0x00F0;
                    dDS_PIXELFORMAT.BBitMask = 0x000F;
                    dDS_PIXELFORMAT.ABitMask = 0xF000;
                    break;

                //Untested
                //BitMasks
                case Util.TextureFormat.R8G8B8:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB;
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 24;
                    dDS_PIXELFORMAT.RBitMask = 0x00FF0000;
                    dDS_PIXELFORMAT.GBitMask = 0x0000FF00;
                    dDS_PIXELFORMAT.BBitMask = 0x000000FF;
                    dDS_PIXELFORMAT.ABitMask = 0x00000000;
                    break;

                case Util.TextureFormat.B8G8R8:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB;
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 24;
                    dDS_PIXELFORMAT.RBitMask = 0x000000FF;
                    dDS_PIXELFORMAT.GBitMask = 0x0000FF00;
                    dDS_PIXELFORMAT.BBitMask = 0x00FF0000;
                    dDS_PIXELFORMAT.ABitMask = 0x00000000;
                    break;

                case Util.TextureFormat.X8L8V8U8:
                case Util.TextureFormat.X8B8G8R8:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB;
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 32;
                    dDS_PIXELFORMAT.RBitMask = 0xFF000000;
                    dDS_PIXELFORMAT.GBitMask = 0x00FF0000;
                    dDS_PIXELFORMAT.BBitMask = 0x0000FF00;
                    dDS_PIXELFORMAT.ABitMask = 0x00000000;
                    break;

                case Util.TextureFormat.L6V5U5:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB;
                    dDS_PIXELFORMAT.RGBBitCount = 16;
                    dDS_PIXELFORMAT.RBitMask = 0xF800;
                    dDS_PIXELFORMAT.GBitMask = 0x07E0;
                    dDS_PIXELFORMAT.BBitMask = 0x001F;
                    dDS_PIXELFORMAT.ABitMask = 0x0000;
                    break;

                case Util.TextureFormat.X4R4G4B4:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB;
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 16;
                    dDS_PIXELFORMAT.RBitMask = 0x0F00;
                    dDS_PIXELFORMAT.GBitMask = 0x00F0;
                    dDS_PIXELFORMAT.BBitMask = 0x000F;
                    dDS_PIXELFORMAT.ABitMask = 0x0000;
                    break;

                case Util.TextureFormat.A4L4:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.LUMINANCEA;
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 8;
                    dDS_PIXELFORMAT.RBitMask = 0x0F;
                    dDS_PIXELFORMAT.GBitMask = 0x00;
                    dDS_PIXELFORMAT.BBitMask = 0x00;
                    dDS_PIXELFORMAT.ABitMask = 0xF0;
                    break;


                case Util.TextureFormat.Q8W8V8U8:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB; // DDPF_RGB
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 32;
                    dDS_PIXELFORMAT.RBitMask = 0xFF000000;
                    dDS_PIXELFORMAT.GBitMask = 0x00FF0000;
                    dDS_PIXELFORMAT.BBitMask = 0x0000FF00;
                    dDS_PIXELFORMAT.ABitMask = 0x000000FF;
                    break;

                case Util.TextureFormat.CxV8U8:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.RGB; // DDPF_RGB
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 16;
                    dDS_PIXELFORMAT.RBitMask = 0xFF00;
                    dDS_PIXELFORMAT.GBitMask = 0x0000;
                    dDS_PIXELFORMAT.BBitMask = 0x0000;
                    dDS_PIXELFORMAT.ABitMask = 0x00FF;
                    break;

                case Util.TextureFormat.DF24:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.LUMINANCE; // DDPF_LUMINANCE
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 24;
                    dDS_PIXELFORMAT.RBitMask = 0xFFFFFF;
                    dDS_PIXELFORMAT.GBitMask = 0x0000;
                    dDS_PIXELFORMAT.BBitMask = 0x0000;
                    dDS_PIXELFORMAT.ABitMask = 0x0000;
                    break;

                case Util.TextureFormat.D24FS8:
                    dDS_PIXELFORMAT.flags = (uint)PixelFormatFlags.LUMINANCE; // DDPF_LUMINANCE
                    dDS_PIXELFORMAT.fourCC = 0;
                    dDS_PIXELFORMAT.RGBBitCount = 32;
                    dDS_PIXELFORMAT.RBitMask = 0xFFFFFF00;
                    dDS_PIXELFORMAT.GBitMask = 0x00000000;
                    dDS_PIXELFORMAT.BBitMask = 0x00000000;
                    dDS_PIXELFORMAT.ABitMask = 0x000000FF;
                    break;

                //Unimplmented
                /*
                case Util.TextureFormat.DF16:

                //xbox formats
                case Util.TextureFormat.XENON_HDR_16FF:
                case Util.TextureFormat.XENON_HDR_16F:
                case Util.TextureFormat.XENON_HDR_16:
                case Util.TextureFormat.XENON_HDR_8:
                case Util.TextureFormat.XENON_HDR_10:
                case Util.TextureFormat.XENON_HDR_11:

                //Compressed Xbox 360 Formats
                //https://fileadmin.cs.lth.se/cs/Personal/Michael_Doggett/talks/x05-xenos-doggett.pdf mentions some info on it under "Texture compression"
                case Util.TextureFormat.DXT3A_1111: //not 100% about
                case Util.TextureFormat.DXT3A
                case Util.TextureFormat.DXT5A
                case Util.TextureFormat.DXN
                case Util.TextureFormat.CTX1

                case Util.TextureFormat.NV_NULL:
                case Util.TextureFormat.A16B16G16R16F_EXPAND:
                case Util.TextureFormat.A2B10G10R10F_EDRAM:
                case Util.TextureFormat.A16L16:
                case Util.TextureFormat.G16R16_EDRAM:
                case Util.TextureFormat.A16B16G16R16_EDRAM:
                case Util.TextureFormat.A8R8G8B8_GAMMA:
                case Util.TextureFormat.A8R8G8B8_GAMMA_AS16:
                case Util.TextureFormat.A2R10G10B10_GAMMA:
                case Util.TextureFormat.A2R10G10B10_GAMMA_AS16:
                case Util.TextureFormat.D16S8:
                */
            }
            return dDS_PIXELFORMAT;
        }

        //DXGI stuff
        public enum D3D10_RESOURCE_DIMENSION : uint
        {
            D3D10_RESOURCE_DIMENSION_UNKNOWN = 0,
            D3D10_RESOURCE_DIMENSION_BUFFER = 1,
            D3D10_RESOURCE_DIMENSION_TEXTURE1D = 2,
            D3D10_RESOURCE_DIMENSION_TEXTURE2D = 3,
            D3D10_RESOURCE_DIMENSION_TEXTURE3D = 4
        };

        public enum DXGI_FORMAT : uint
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,
            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,
            DXGI_FORMAT_FORCE_UINT = 0xffffffff
        };

        //helper
        internal enum FourCCFormat : uint
        {
            Dxt1 = ((uint)'D' | ((uint)'X' << 8) | ((uint)'T' << 16) | ((uint)'1' << 24)),
            Dxt2 = ((uint)'D' | ((uint)'X' << 8) | ((uint)'T' << 16) | ((uint)'2' << 24)),
            Dxt3 = ((uint)'D' | ((uint)'X' << 8) | ((uint)'T' << 16) | ((uint)'3' << 24)),
            Dxt4 = ((uint)'D' | ((uint)'X' << 8) | ((uint)'T' << 16) | ((uint)'4' << 24)),
            Dxt5 = ((uint)'D' | ((uint)'X' << 8) | ((uint)'T' << 16) | ((uint)'5' << 24)),
            Dx10 = ((uint)'D' | ((uint)'X' << 8) | ((uint)'1' << 16) | ((uint)'0' << 24))
        }
        public static uint MakeFourCC(string fourcc)
        {
            return (uint)(fourcc[0] | (fourcc[1] << 8) | (fourcc[2] << 16) | (fourcc[3] << 24));
        }
    }
}