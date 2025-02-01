using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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


                for (int i = 0; i < m_MainHeader.m_ResourceNamesCount; i++)
                {
                    uint filetype = (m_LogHeaderEntries[i].m_Bitfields >> 16) & 0xFF;

                    var firstResource = m_LogHeaderEntries[i].m_FirstResource;

                    //splits the name list by its m_Names offset
                    string fullText = m_NamesBuffer.Substring((int)(m_Names[i]));
                    string firstWord = fullText.Split('\0')[0];
                    //prints the section's name
                    Console.WriteLine();
                    Console.WriteLine(firstWord);

                    //prints the sections type
                    string type = Util.GetResourceName((int)filetype);
                    Console.WriteLine($"Detected Type: {type}");


                    var entryCount = ((m_LogHeaderEntries[i].m_Bitfields) & 0xFF);

                    List<byte[]> fileParts = new List<byte[]>();
                    for (int p = 0; p < entryCount; p++)
                    {
                        var currentSection = (m_PhysEntries[firstResource].m_Bitfields >> 0) & 0xFF;
                        var dataSize = m_PhysEntries[firstResource].m_DataByteSize;

                        var offset = m_DefinedTypes[currentSection].m_DataFileOffset + m_PhysEntries[firstResource].m_DataOffset;

                        // Seek to the offset of each file part
                        input.Seek(offset, SeekOrigin.Begin);

                        byte[] bytes = new byte[dataSize];
                        input.Read(bytes, 0, (int)dataSize);

                        //add to list
                        fileParts.Add(bytes); 
                        Console.WriteLine(offset);

                        //inc resource
                        firstResource++;
                    }

                    //folder/type/file.anm2
                    if (filetype == (int)Util.ResourceType.Animation)
                    {
                        //nothing too special for anm2
                        string outputDir = Path.Combine(Path.GetFileNameWithoutExtension(inputfile), type);
                        string outputFile = Path.Combine(outputDir, firstWord + ".anm2");
                        Directory.CreateDirectory(outputDir);

                        for (int f = 0; f < fileParts.Count; f++)
                        {
                            var part = fileParts[f];
                            if(f >= 1)
                            {
                                Console.WriteLine("Don't think that's supposted to happen, MULTIPLE FILES");
                                Debugger.Break();
                                outputFile = Path.Combine(outputDir, f.ToString());
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
                                outputFile = Path.Combine(outputDir, f.ToString());
                            }

                            string fullString = Encoding.UTF8.GetString(part);
                            string[] lines = fullString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                            StringBuilder formated = Util.FormatFX(lines);

                            File.WriteAllText(outputFile, formated.ToString());
                        }
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
