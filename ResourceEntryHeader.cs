using System;
using System.IO;
using System.Text;

namespace DumpRP6
{
    internal class ResourceEntryHeader
    {
        public uint m_Bitfields;
        public uint m_DataOffset;
        public uint m_DataByteSize;
        public void Deserialize(Stream input)
        {
            m_Bitfields = Util.ReadValueU32(input);
            m_DataOffset = Util.ReadValueU32(input);
            m_DataByteSize = Util.ReadValueU32(input);

            /* discarded bc It's never used?
            struct {
                short m_CompressedByteSize;
                short m_ReferencedResource;
            } ResourceEntryHeader;
             */
            _ = Util.ReadValueU32(input);
        }
    }
}
