using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssmtp
{
    internal class FixedStack
    {
        private byte[] m_StackList = null;
        private byte[] m_Terminator = null;

        public FixedStack(string terminator)
        {
            m_Terminator = Encoding.ASCII.GetBytes(terminator);
            m_StackList = new byte[m_Terminator.Length];

            for (int i = 0; i < m_Terminator.Length; ++i)
                m_StackList[i] = (byte)0;
        }

        public int Push(byte[] bytes, int count)
        {
            if (bytes.Length > m_Terminator.Length)
                throw new Exception("bytes.Length is too big! can not be more than terminator.length!");

            Array.Copy(m_StackList, count, m_StackList, 0, m_StackList.Length - count);
            Array.Copy(bytes, 0, m_StackList, m_StackList.Length - count, count);

            int index = Array.IndexOf(m_StackList, m_Terminator[0]);

            if (index > -1)
            {
                if (index == 0)
                {
                    for (int i = 0; i < m_StackList.Length; ++i)
                        if ((byte)m_StackList[i] != m_Terminator[i])
                            return 1;
                    return 0;
                }

                return 1;
            }
            else
            {
                return m_Terminator.Length;
            }
        }

        public bool ContainsTerminator()
        {
            for (int i = 0; i < m_StackList.Length; ++i)
                if ((byte)m_StackList[i] != m_Terminator[i])
                    return false;
            return true;
        }
    }
}
