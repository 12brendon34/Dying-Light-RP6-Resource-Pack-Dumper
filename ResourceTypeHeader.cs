using System.IO;
using System.Text;

namespace DumpRP6
{
    internal class ResourceTypeHeader
    {
        public uint m_Bitfields;
        public uint m_DataFileOffset;
        public uint m_DataByteSize;
        public uint m_CompressedByteSize;
        public uint m_ResourceCount;

        public void Deserialize(Stream input)
        {
            m_Bitfields = Util.ReadValueU32(input);
            m_DataFileOffset = Util.ReadValueU32(input);
            m_DataByteSize = Util.ReadValueU32(input);
            m_CompressedByteSize = Util.ReadValueU32(input);
            m_ResourceCount = Util.ReadValueU32(input);
        }
    }
}
