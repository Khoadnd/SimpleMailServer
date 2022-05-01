using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class ServerCore
    {
        public static MemoryStream DoPeriodHandling(Stream stream, bool addRemove)
        {
            return DoPeriodHandling(stream, addRemove, true);
        }

        public static MemoryStream DoPeriodHandling(Stream stream, bool addRemove, bool setStreamPosTo0)
        {
            MemoryStream replyData = new MemoryStream();

            var crlf = new byte[] { (byte)'\r', (byte)'\n' };
            if (setStreamPosTo0)
                stream.Position = 0;

            StreamLineReader r = new StreamLineReader(stream);
            byte[] line = r.ReadLine();

            while (line != null)
            {
                if (line.Length > 0)
                {
                    if (line[0] == (byte)'.')
                    {
                        if (addRemove)
                        {
                            replyData.WriteByte((byte)'.');
                            replyData.Write(line, 0, line.Length);
                        }
                        else
                            replyData.Write(line, 1, line.Length - 1);
                    }
                    else
                    {
                        replyData.Write(line, 0, line.Length);
                    }
                }
                replyData.Write(crlf, 0, crlf.Length);
                line = r.ReadLine();
            }
            replyData.Position = 0;
            return replyData;
        }

        public static ReadReplyCode ReadData(Socket socket, out MemoryStream replyData, byte[] addData, int maxLength,
            string terminator, string removeFromEnd)
        {
            ReadReplyCode replyCode = ReadReplyCode.Ok;
            replyData = null;

            try
            {
                replyData = new MemoryStream();
                // write header
                replyData.Write(addData, 0, addData.Length);
                FixedStack stack = new FixedStack(terminator);
                var nextReadWritelen = 1;

                //var lastDataTime = DateTime.Now.Ticks;
                while (nextReadWritelen > 0)
                {
                    if (socket.Available >= nextReadWritelen)
                    {
                        var b = new byte[nextReadWritelen];
                        var countReceived = socket.Receive(b);

                        if (replyCode != ReadReplyCode.LengthExceeded)
                            replyData.Write(b, 0, countReceived);

                        nextReadWritelen = stack.Push(b, countReceived);

                        if (replyCode != ReadReplyCode.LengthExceeded && replyData.Length > maxLength)
                            replyCode = ReadReplyCode.LengthExceeded;

                        //lastDataTime = DateTime.Now.Ticks;
                    }
                    else
                    {
                        // timeout stuff if you want to implement ^^
                    }
                }
                if (replyCode == ReadReplyCode.Ok && removeFromEnd.Length > 0)
                    replyData.SetLength(replyData.Length - removeFromEnd.Length);
            }
            catch
            {
                replyCode = ReadReplyCode.UnknownError;
            }
            return replyCode;
        }
    }
}
