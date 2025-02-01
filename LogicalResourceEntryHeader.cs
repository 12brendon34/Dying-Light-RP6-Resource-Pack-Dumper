using System.IO;
using System.Text;

namespace DumpRP6
{
    internal class LogicalResourceEntryHeader
    {
        public uint m_Bitfields;
        public uint m_FirstNameIndex;
        public uint m_FirstResource;
        public void Deserialize(Stream input)
        {
            m_Bitfields = Util.ReadValueU32(input);
            m_FirstNameIndex = Util.ReadValueU32(input);
            m_FirstResource = Util.ReadValueU32(input);
        }
    }
}
