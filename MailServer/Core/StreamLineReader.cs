using System.Collections;

namespace MailServer.Core
{
    public class StreamLineReader
    {
        private Stream m_streamSource = null;

        public StreamLineReader(Stream streamSource)
        {
            m_streamSource = streamSource;
        }

        public byte[] ReadLine()
        {
            ArrayList lineBuf = new ArrayList();
            byte prevByte = 0;
            int currByteInt = m_streamSource.ReadByte();
            while (currByteInt > -1)
            {
                lineBuf.Add((byte)currByteInt);
                if (prevByte == (byte)'\r' && (byte)currByteInt == (byte)'\n')
                {
                    var ret = new byte[lineBuf.Count - 2];
                    lineBuf.CopyTo(0, ret, 0, lineBuf.Count - 2);

                    return ret;
                }

                prevByte = (byte)currByteInt;
                currByteInt = m_streamSource.ReadByte();
            }

            if (lineBuf.Count > 0)
            {
                var ret = new byte[lineBuf.Count];
                lineBuf.CopyTo(0, ret, 0, lineBuf.Count);
                return ret;
            }
            return null;
        }
    }
}
