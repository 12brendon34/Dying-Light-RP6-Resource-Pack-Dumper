using System.IO;
using System.Text;

namespace DumpRP6
{
    internal class MainHeader
    {
        public bool Endianness; //true for little endian

        public string MagicID;
        public uint m_Version;
        public uint m_Flags;
        public uint m_PhysResCount;
        public uint m_PhysResTypeCount;
        public uint m_ResourceNamesCount;
        public uint m_ResourceNamesBlockSize;
        public uint m_LogResCount;
        public uint m_SectorAlignment;
        public void Deserialize(Stream input)
        {
            MagicID = Util.ReadString(input, Encoding.ASCII, 4);
            m_Version = Util.ReadValueU32(input);

            m_Flags = Util.ReadValueU32(input);
            m_PhysResCount = Util.ReadValueU32(input);
            m_PhysResTypeCount = Util.ReadValueU32(input);
            m_ResourceNamesCount = Util.ReadValueU32(input);
            m_ResourceNamesBlockSize = Util.ReadValueU32(input);
            m_LogResCount = Util.ReadValueU32(input);
            m_SectorAlignment = Util.ReadValueU32(input);

            Endianness = MagicID[3] == 'L';
        }
    }
}
