using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.LZMA;
using SharpCompress.IO;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;
using static DumpRP6.DDS;
using static DumpRP6.Util;

namespace DumpRP6
{
    internal class Program
    {

        static void Main(string[] args)
        {
            List<string> arguments = new List<string>(args);

            if (arguments.Count < 1 || arguments.Count > 2)
            {
                Console.WriteLine("Usage: {0} input_file.rpack", System.AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine();
                return;
            }

            string inputfile = arguments[0];

            using (var input = File.OpenRead(inputfile))
            {
                var m_MainHeader = new MainHeader();
                m_MainHeader.Deserialize(input);
                //m_LogEntries
                //m_Names

                var m_DefinedTypes = new ResourceTypeHeader[m_MainHeader.m_PhysResTypeCount];
                var m_PhysEntries = new ResourceEntryHeader[m_MainHeader.m_PhysResCount];
                var m_LogHeaderEntries = new LogicalResourceEntryHeader[m_MainHeader.m_ResourceNamesCount];

                //m_DefinedTypes
                for (int i = 0; i < m_MainHeader.m_PhysResTypeCount; i++)
                {
                    var typeHeader = new ResourceTypeHeader();
                    typeHeader.Deserialize(input);
                    m_DefinedTypes[i] = typeHeader;
                }

                //m_PhysEntries
                for (int i = 0; i < m_MainHeader.m_PhysResCount; i++)
                {
                    var physHeader = new ResourceEntryHeader();
                    physHeader.Deserialize(input);
                    m_PhysEntries[i] = physHeader;
                }

                //m_LogHeaderEntries
                for (int i = 0; i < m_MainHeader.m_ResourceNamesCount; i++)
                {
                    var logHeader = new LogicalResourceEntryHeader();
                    logHeader.Deserialize(input);
                    m_LogHeaderEntries[i] = logHeader;
                }

                /*
                    technically it's like this
             
                    struct {
                        uint m_NamesBlockOffset;
                    } m_Names[m_MainHeader.m_ResourceNamesCount];

                    But this is fine
                 */

                var m_Names = new uint[m_MainHeader.m_ResourceNamesCount];
                for (int i = 0; i < m_MainHeader.m_ResourceNamesCount; i++)
                {
                    m_Names[i] = Util.ReadValueU32(input);
                }
                var m_NamesBuffer = Util.ReadString(input, Encoding.ASCII, (int)m_MainHeader.m_ResourceNamesBlockSize);

                Console.WriteLine("MagicID : " + m_MainHeader.MagicID);
                Console.WriteLine("m_Version : " + m_MainHeader.m_Version);

                Console.WriteLine("m_NamesBuffer :" + m_NamesBuffer);
                Console.WriteLine();

                List<byte[]> DecompressedSections = new List<byte[]>();
                for (int i = 0; i < m_MainHeader.m_PhysResTypeCount; i++)
                {
                    var packedSize = m_DefinedTypes[i].m_CompressedByteSize;

                    if (packedSize > 0)
                    {
                        Console.WriteLine("Compressed");
                        //var compressedSize = m_DefinedTypes[i].m_CompressedByteSize;
                        var byteSize = m_DefinedTypes[i].m_DataByteSize;
                        var offset = m_DefinedTypes[i].m_DataFileOffset;
                        Console.WriteLine(offset);

                        
                        //jump to data section
                        input.Seek(offset, SeekOrigin.Begin);

                        byte[] compressionMagic = new byte[2];
                        input.Read(compressionMagic, 0, 2);

                        //jump back to compressed section
                        input.Seek(offset, SeekOrigin.Begin);

                        byte[] bytes = new byte[byteSize];
                        if (compressionMagic[0] == 120) //hex 78 or first of zlib
                        {
                            var zlibStream = new ZlibStream(input, SharpCompress.Compressors.CompressionMode.Decompress);
                            zlibStream.Read(bytes, 0, (int)byteSize);
                        }
                        else
                        {
                            var decoder = new Lzma.DecoderStream(input);
                            decoder.Initialize(Lzma.DecoderProperties.Default);
                            decoder.Read(bytes, 0, (int)byteSize);
                        }

                        DecompressedSections.Add(bytes);
                    }
                    else
                    {
                        Console.WriteLine("Not Compressed Section");
                        var offset = m_DefinedTypes[i].m_DataFileOffset;
                        Console.WriteLine(offset);
                    }
                }

                for (int i = 0; i < m_MainHeader.m_ResourceNamesCount; i++)
                {
                    uint filetype = (m_LogHeaderEntries[i].m_Bitfields >> 16) & 0xFF;

                    //splits the name list by its m_Names offset
                    string fullText = m_NamesBuffer.Substring((int)(m_Names[i]));
                    string firstWord = fullText.Split('\0')[0];
                    //prints the section's name
                    Console.WriteLine();
                    Console.WriteLine(firstWord);

                    //prints the sections type
                    string type = Util.GetResourceName((int)filetype);
                    Console.WriteLine($"Detected Type: {type}");

                    var currentResource = m_LogHeaderEntries[i].m_FirstResource;
                    var entryCount = ((m_LogHeaderEntries[i].m_Bitfields) & 0xFF);

                    List<byte[]> fileParts = new List<byte[]>();
                    for (int p = 0; p <= entryCount - 1; p++)
                    {
                        var currentSection = m_PhysEntries[currentResource].m_Bitfields & 0xFF;

                        var dataSize = m_PhysEntries[currentResource].m_DataByteSize;
                        var offset = m_DefinedTypes[currentSection].m_DataFileOffset + m_PhysEntries[currentResource].m_DataOffset;
                        byte[] bytes = new byte[dataSize];

                        //if the current section is/was compressed we extract from this instead of directly
                        if ((int)currentSection >= 0 && (int)currentSection < DecompressedSections.Count) //basically if currentSection is a number in DecompressedSections as some sections might not be compressed
                        {
                            offset = m_PhysEntries[currentResource].m_DataOffset;
                            MemoryStream decompressedStream = new MemoryStream(DecompressedSections[(int)currentSection]);

                            decompressedStream.Seek(offset, SeekOrigin.Begin);
                            decompressedStream.Read(bytes, 0, (int)dataSize);
                        }
                        else
                        {
                            // Seek to the offset of each file part
                            input.Seek(offset, SeekOrigin.Begin);
                            input.Read(bytes, 0, (int)dataSize);
                        }

                        //add to list
                        fileParts.Add(bytes);
                        Console.WriteLine(offset);
                        
                        currentResource++;
                    }

                    //quit current loop if nothing to do
                    if (fileParts.Count == 0)
                        continue;

                    //folder/type/file.anm2
                    if (filetype == (int)Util.ResourceType.Animation)
                    {
                        //nothing too special for anm2
                        string outputDir = Path.Combine(Path.GetFileNameWithoutExtension(inputfile), type);

                        string Extention = ".anm2";

                        Directory.CreateDirectory(outputDir);

                        for (int f = 0; f < fileParts.Count; f++)
                        {
                            if (fileParts[0][3] == 49)
                                Extention = ".anm1";

                            var part = fileParts[f];
                            string outputFile = Path.Combine(outputDir, firstWord + Extention);

                            if (f >= 1)
                            {
                                Console.WriteLine("Don't think that's supposted to happen, MULTIPLE FILES");
                                Debugger.Break();
                                outputFile = Path.Combine(outputDir, firstWord + "_Part_" + f.ToString() + ".anm2");
                            }

                            using (var output = File.OpenWrite(outputFile))
                            {
                                output.Write(part, 0, part.Length);
                                output.Close();
                            }
                        }
                    }
                    else if (filetype == (int)Util.ResourceType.Fx)
                    {
                        string outputDir = Path.Combine(Path.GetFileNameWithoutExtension(inputfile), type);
                        string outputFile = Path.Combine(outputDir, firstWord + ".fx");
                        Directory.CreateDirectory(outputDir);

                        for (int f = 0; f < fileParts.Count; f++)
                        {
                            var part = fileParts[f];
                            if (f >= 1)
                            {
                                Console.WriteLine("Don't think that's supposted to happen, MULTIPLE FILES");
                                Debugger.Break();
                                outputFile = Path.Combine(outputDir, firstWord + "_Part_" + f.ToString() + ".fx");
                            }

                            string fullString = Encoding.UTF8.GetString(part);
                            string[] lines = fullString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                            StringBuilder formated = Util.FormatFX(lines);

                            File.WriteAllText(outputFile, formated.ToString());
                        }
                    }
                    else if (filetype == (int)Util.ResourceType.BuilderInformation)
                    {
                        string outputDir = Path.Combine(Path.GetFileNameWithoutExtension(inputfile), type);
                        string outputFile = Path.Combine(outputDir, firstWord + ".txt");
                        Directory.CreateDirectory(outputDir);

                        for (int f = 0; f < fileParts.Count; f++)
                        {
                            var part = fileParts[f];
                            if (f >= 1)
                            {
                                Console.WriteLine("Don't think that's supposted to happen, MULTIPLE FILES");
                                Debugger.Break();
                                outputFile = Path.Combine(outputDir, firstWord + "_Part_" + f.ToString() + ".fx");
                            }

                            using (var output = File.OpenWrite(outputFile))
                            {
                                output.Write(part, 0, part.Length);
                                output.Close();
                            }
                        }
                    }
                    else if (filetype == (int)Util.ResourceType.Texture)
                    {
                        string outputDir = Path.Combine(Path.GetFileNameWithoutExtension(inputfile), type);
                        string outputFile = Path.Combine(outputDir, firstWord + ".dds");
                        Directory.CreateDirectory(outputDir);

                        MemoryStream textureHeader = new MemoryStream(fileParts[0]);

                        var m_TextureHeader = new TextureHeader();
                        m_TextureHeader.Deserialize(textureHeader);

                        Console.WriteLine($"m_TextureHeader Width: {m_TextureHeader.Width}");
                        Console.WriteLine($"m_TextureHeader Height: {m_TextureHeader.Height}");


                        int pixelWidth = 0;
                        int blockCount = (m_TextureHeader.Width + 3) / 4 * ((m_TextureHeader.Height + 3) / 4);
                        uint PitchOrLinearSize = 0;

                        bool isCompressed = false;
                        switch (m_TextureHeader.Format)
                        {
                            case TextureFormat.DXT1:
                            case TextureFormat.CTX1:
                            case TextureFormat.DXN:
                            case TextureFormat.DXT3:
                            case TextureFormat.DXT5:
                            case TextureFormat.DXT3A:
                            case TextureFormat.DXT5A:
                            case TextureFormat.DXT3A_1111:
                                isCompressed = true;
                                break;
                        }

                        //PitchOrLinearSize for compressed vs uncompressed
                        if (isCompressed)
                        {
                            switch (m_TextureHeader.Format)
                            {
                                //blockSize
                                case TextureFormat.DXT1:
                                case TextureFormat.CTX1:
                                case TextureFormat.DXN:
                                    PitchOrLinearSize = (uint)(blockCount * 8);
                                    break;

                                case TextureFormat.DXT3:
                                case TextureFormat.DXT5:
                                case TextureFormat.DXT3A:
                                case TextureFormat.DXT5A:
                                case TextureFormat.DXT3A_1111:
                                    PitchOrLinearSize = (uint)(blockCount * 16);
                                    break;
                            }
                        }
                        else
                        {
                            //most have a pixelwidth of 4, skips a few checks
                            pixelWidth = 4;

                            switch (m_TextureHeader.Format)
                            {
                                case TextureFormat.A32B32G32R32F:
                                    pixelWidth = 16;
                                    break;

                                case TextureFormat.A16B16G16R16:
                                case TextureFormat.A16B16G16R16F:
                                    pixelWidth = 8;
                                    break;

                                case TextureFormat.R8G8B8:
                                case TextureFormat.B8G8R8:
                                    pixelWidth = 3;
                                    break;

                                case TextureFormat.R5G6B5:
                                case TextureFormat.X1R5G5B5:
                                case TextureFormat.A1R5G5B5:
                                case TextureFormat.A4R4G4B4:
                                case TextureFormat.X4R4G4B4:
                                case TextureFormat.V8U8:
                                case TextureFormat.L6V5U5:
                                case TextureFormat.L16:
                                case TextureFormat.R16F:
                                case TextureFormat.D16:
                                case TextureFormat.DF16:
                                case TextureFormat.XENON_HDR_16F:
                                case TextureFormat.XENON_HDR_16:
                                case TextureFormat.XENON_HDR_11:
                                    pixelWidth = 2;
                                    break;

                                case TextureFormat.A8:
                                case TextureFormat.L8:
                                case TextureFormat.A4L4:
                                case TextureFormat.XENON_HDR_8:
                                    pixelWidth = 1;
                                    break;
                            }

                            PitchOrLinearSize = (uint)(m_TextureHeader.Width * pixelWidth);
                        }

                        uint DDS_MAGIC = 0x20534444; // "DDS "

                        uint DDS_HEADER_SIZE = 124;
                        uint DDSCAPS_TEXTURE = 0x1000;

                        DDS_PIXELFORMAT dDS_PIXELFORMAT = DDS.GetPixelFormat(m_TextureHeader.Format);
                        DDS.DDS_HEADER header = new DDS.DDS_HEADER
                        {
                            size = DDS_HEADER_SIZE,
                            flags = m_TextureHeader.Flags, //DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT,
                            height = m_TextureHeader.Height,
                            width = m_TextureHeader.Width,
                            pitchOrLinearSize = PitchOrLinearSize,
                            depth = m_TextureHeader.Depth,
                            mipMapCount = m_TextureHeader.MipMapCount,
                            reserved1 = new uint[11],
                            ddspf = dDS_PIXELFORMAT,
                            caps = DDSCAPS_TEXTURE,
                            caps2 = 0,
                            caps3 = 0,
                            caps4 = 0,
                            reserved2 = 0
                        };

                        using (var output = File.OpenWrite(outputFile))
                        {
                            Util.WriteU32(output, DDS_MAGIC);

                            // Write DDS_HEADER
                            Util.WriteU32(output, header.size);
                            Util.WriteU32(output, header.flags);
                            Util.WriteU32(output, header.height);
                            Util.WriteU32(output, header.width);
                            Util.WriteU32(output, header.pitchOrLinearSize);
                            Util.WriteU32(output, header.depth);
                            Util.WriteU32(output, header.mipMapCount);

                            foreach (uint value in header.reserved1)
                            {
                                Util.WriteU32(output, value);
                            }

                            // Write DDS_PIXELFORMAT
                            Util.WriteU32(output, header.ddspf.size);
                            Util.WriteU32(output, header.ddspf.flags);
                            Util.WriteU32(output, header.ddspf.fourCC);
                            Util.WriteU32(output, header.ddspf.RGBBitCount);
                            Util.WriteU32(output, header.ddspf.RBitMask);
                            Util.WriteU32(output, header.ddspf.GBitMask);
                            Util.WriteU32(output, header.ddspf.BBitMask);
                            Util.WriteU32(output, header.ddspf.ABitMask);

                            // Write remaining header fields
                            Util.WriteU32(output, header.caps);
                            Util.WriteU32(output, header.caps2);
                            Util.WriteU32(output, header.caps3);
                            Util.WriteU32(output, header.caps4);
                            Util.WriteU32(output, header.reserved2);

                            //Write Actuall TextureData
                            output.Write(fileParts[1], 0, fileParts[1].Length);

                            //IDK how to do mipmaps yet
                            output.Close();
                        }
                        /*
                        DDS.DDS_PIXELFORMAT DDSPF = new DDS.DDS_PIXELFORMAT();
                        switch (m_TextureHeader.Format)
                        {
                            case Util.TextureFormat.R8G8B8:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 24;
                                DDSPF.RBitMask = 0x00FF0000;
                                DDSPF.GBitMask = 0x0000FF00;
                                DDSPF.BBitMask = 0x000000FF;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.B8G8R8:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 24;
                                DDSPF.RBitMask = 0x000000FF;
                                DDSPF.GBitMask = 0x0000FF00;
                                DDSPF.BBitMask = 0x00FF0000;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.A8R8G8B8:

                                DDSPF.flags = DDS.DDS_RGBA;
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0x00FF0000;
                                DDSPF.GBitMask = 0x0000FF00;
                                DDSPF.BBitMask = 0x000000FF;
                                DDSPF.ABitMask = 0xFF000000;
                                break;

                            case Util.TextureFormat.X8R8G8B8:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0x00FF0000;
                                DDSPF.GBitMask = 0x0000FF00;
                                DDSPF.BBitMask = 0x000000FF;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.B8G8R8X8:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0x000000FF;
                                DDSPF.GBitMask = 0x0000FF00;
                                DDSPF.BBitMask = 0x00FF0000;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.B8G8R8A8:
                                DDSPF.flags = DDS.DDS_RGBA;
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0x000000FF;
                                DDSPF.GBitMask = 0x0000FF00;
                                DDSPF.BBitMask = 0x00FF0000;
                                DDSPF.ABitMask = 0xFF000000;
                                break;

                            case Util.TextureFormat.A8B8G8R8:
                                DDSPF.flags = DDS.DDS_RGBA;
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFF000000;
                                DDSPF.GBitMask = 0x00FF0000;
                                DDSPF.BBitMask = 0x0000FF00;
                                DDSPF.ABitMask = 0x000000FF;
                                break;

                            case Util.TextureFormat.X8B8G8R8:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFF000000;
                                DDSPF.GBitMask = 0x00FF0000;
                                DDSPF.BBitMask = 0x0000FF00;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            //fall onto next case as are same
                            case Util.TextureFormat.L6V5U5:
                            case Util.TextureFormat.R5G6B5:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0xF800;
                                DDSPF.GBitMask = 0x07E0;
                                DDSPF.BBitMask = 0x001F;
                                DDSPF.ABitMask = 0x0000;
                                break;

                            case Util.TextureFormat.X1R5G5B5:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0x7C00;
                                DDSPF.GBitMask = 0x03E0;
                                DDSPF.BBitMask = 0x001F;
                                DDSPF.ABitMask = 0x0000;
                                break;

                            case Util.TextureFormat.A1R5G5B5:
                                DDSPF.flags = DDS.DDS_RGBA;
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0x7C00;
                                DDSPF.GBitMask = 0x03E0;
                                DDSPF.BBitMask = 0x001F;
                                DDSPF.ABitMask = 0x8000;
                                break;

                            case Util.TextureFormat.A4R4G4B4:
                                DDSPF.flags = DDS.DDS_RGBA;
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0x0F00;
                                DDSPF.GBitMask = 0x00F0;
                                DDSPF.BBitMask = 0x000F;
                                DDSPF.ABitMask = 0xF000;
                                break;

                            case Util.TextureFormat.X4R4G4B4:
                                DDSPF.flags = DDS.DDS_RGB;
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0x0F00;
                                DDSPF.GBitMask = 0x00F0;
                                DDSPF.BBitMask = 0x000F;
                                DDSPF.ABitMask = 0x0000;
                                break;

                            case Util.TextureFormat.A8:
                                DDSPF.flags = DDS.DDS_ALPHA;
                                DDSPF.RGBBitCount = 8;
                                DDSPF.RBitMask = 0x00000000;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x000000FF;
                                break;

                            case Util.TextureFormat.L8:
                                DDSPF.flags = DDS.DDS_LUMINANCE;
                                DDSPF.RGBBitCount = 8;
                                DDSPF.RBitMask = 0x000000FF;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.A8L8:
                                DDSPF.flags = DDS.DDS_LUMINANCEA;
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0x000000FF;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x0000FF00;
                                break;

                            case Util.TextureFormat.A4L4:
                                DDSPF.flags = DDS.DDS_LUMINANCEA;
                                DDSPF.RGBBitCount = 8;
                                DDSPF.RBitMask = 0x0F;
                                DDSPF.GBitMask = 0x00;
                                DDSPF.BBitMask = 0x00;
                                DDSPF.ABitMask = 0xF0;
                                break;

                            case Util.TextureFormat.DXT1:
                                DDSPF.flags = DDS.DDS_FOURCC;
                                DDSPF.fourCC = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("DXT1"), 0); 
                                break;

                            case Util.TextureFormat.DXT3:
                                DDSPF.flags = DDS.DDS_FOURCC;
                                DDSPF.fourCC = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("DXT3"), 0);
                                break;

                            case Util.TextureFormat.DXT5:
                                DDSPF.flags = DDS.DDS_FOURCC;
                                DDSPF.fourCC = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("DXT5"), 0);
                                break;

                            case Util.TextureFormat.V8U8:
                                DDSPF.flags = DDS.DDS_RGB; // DDPF_RGB
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0x00FF00;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x0000FF;
                                break;

                            case Util.TextureFormat.Q8W8V8U8:
                            case Util.TextureFormat.X8L8V8U8:
                                DDSPF.flags = DDS.DDS_RGB; // DDPF_RGB
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFF000000;
                                DDSPF.GBitMask = 0x00FF0000;
                                DDSPF.BBitMask = 0x0000FF00;
                                DDSPF.ABitMask = 0x000000FF;
                                break;

                            case Util.TextureFormat.CxV8U8:
                                DDSPF.flags = DDS.DDS_RGB; // DDPF_RGB
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0xFF00;
                                DDSPF.GBitMask = 0x0000;
                                DDSPF.BBitMask = 0x0000;
                                DDSPF.ABitMask = 0x00FF;
                                break;

                            case Util.TextureFormat.DF16:
                            case Util.TextureFormat.D16:
                            case Util.TextureFormat.R16F:
                            case Util.TextureFormat.L16:
                                DDSPF.flags = 0x20000; // DDPF_LUMINANCE
                                DDSPF.RGBBitCount = 16;
                                DDSPF.RBitMask = 0xFFFF;
                                DDSPF.GBitMask = 0x0000;
                                DDSPF.BBitMask = 0x0000;
                                DDSPF.ABitMask = 0x0000;
                                break;

                            case Util.TextureFormat.G16R16F:
                            case Util.TextureFormat.G16R16:
                                DDSPF.flags = DDS.DDS_RGB; // DDPF_RGB
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFFFF0000;
                                DDSPF.GBitMask = 0x0000FFFF;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.D32:
                            case Util.TextureFormat.R32F:
                                DDSPF.flags = 0x20000; // DDPF_LUMINANCE
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFFFFFFFF;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.D24S8:
                                DDSPF.flags = 0x20000; // DDPF_LUMINANCE
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFFFFFF00;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x000000FF;
                                break;

                            case Util.TextureFormat.D24X8:
                                DDSPF.flags = 0x20000; // DDPF_LUMINANCE
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFFFFFF00;
                                DDSPF.GBitMask = 0x00000000;
                                DDSPF.BBitMask = 0x00000000;
                                DDSPF.ABitMask = 0x00000000;
                                break;

                            case Util.TextureFormat.DF24:
                                DDSPF.flags = 0x20000; // DDPF_LUMINANCE
                                DDSPF.RGBBitCount = 24;
                                DDSPF.RBitMask = 0xFFFFFF;
                                DDSPF.GBitMask = 0x0000;
                                DDSPF.BBitMask = 0x0000;
                                DDSPF.ABitMask = 0x0000;
                                break;

                            case Util.TextureFormat.D24FS8:
                                DDSPF.flags = 0x20000; // DDPF_LUMINANCE
                                DDSPF.RGBBitCount = 32;
                                DDSPF.RBitMask = 0xFFFFFF00;
                                DDSPF.GBitMask = 0x00000000;
                                break;
                            default:
                                Console.WriteLine("Non Implemented TextureFormat");
                                break;

                        }
                        */
                    }
                    //default if unrecognized, folder/type/name/partcount 
                    else
                    {
                        string outputDir = Path.Combine(Path.GetFileNameWithoutExtension(inputfile), Path.Combine(type, firstWord));
                        Directory.CreateDirectory(outputDir);

                        for (int f = 0; f < fileParts.Count; f++)
                        {
                            var part = fileParts[f];

                            string outputFile = Path.Combine(outputDir, f.ToString());
                            using (var output = File.OpenWrite(outputFile))
                            {
                                output.Write(part, 0, part.Length);
                                output.Close();
                            }
                        }
                    }
                }
            }
        }
    }
}
