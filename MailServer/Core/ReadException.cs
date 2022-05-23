namespace MailServer.Core
{
    public enum ReadReplyCode
    {
        Ok = 0,
        TimeOut = 1,
        LengthExceeded = 2,
        UnknownError = 3,
    }
    public class ReadException : Exception
    {
        private ReadException m_ReadReplyCode;

        public ReadException(ReadException code, string message) : base(message)
        {
            m_ReadReplyCode = code;
        }

        // get error
        public ReadException ReadReplyCode
        {
            get { return m_ReadReplyCode; }
        }
    }
}
