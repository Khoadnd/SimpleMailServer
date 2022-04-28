using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ssmtp
{
    public class SSMTP_Session
    {
        private Socket m_ClientSocket = null; // ref to Client socket
        private SSMTP_Server m_SSMTP_Server = null; // ref to SSMTP server
        private string m_SessionID = "";
        private string m_Username = "";
        private string m_ConnectedIP = "";
        private string m_Reverse_path = "";
        private Hashtable m_Forward_path = null;
        private bool m_Authenticated = false;
        private MemoryStream m_MsgStream = null;

        internal SSMTP_Session(Socket clientSocket, SSMTP_Server server, string sesisonID)
        {
            m_ClientSocket = clientSocket;
            m_SSMTP_Server = server;
            m_SessionID = sesisonID;
            m_Forward_path = new Hashtable();
        }

        public void StartProcessing()
        {
            try
            {
                m_ConnectedIP = GetIPFromEndPoint(RemoteEndpoint.ToString());

                string lastCmd = "";
                while (true)
                {
                    if (m_ClientSocket.Available > 0)
                    {
                        lastCmd = ReadLine();
                        if (SwitchCommand(lastCmd))
                            break;
                    }
                }
            }
            catch (Exception x)
            {
                if (m_ClientSocket.Connected)
                    SendData("Service not available ^^\r\n");
                else
                    Console.WriteLine("Connection is aborted!", m_SessionID, m_ConnectedIP, "x");
            }
            finally
            {
                m_SSMTP_Server.RemoveSession(m_SessionID);

                if (m_ClientSocket.Connected)
                    m_ClientSocket.Close();
            }
        }

        private bool SwitchCommand(string SSMTP_Commandtxt)
        {
            // ----Parse command---------------------------------------------------- //
            string[] cmdParts = SSMTP_Commandtxt.TrimStart().Split(new char[] { ' ' });
            string SSMTP_command = cmdParts[0].ToUpper().Trim();
            string argsText = GetArgsText(SSMTP_Commandtxt, SSMTP_command);
            // --------------------------------------------------------------------- //

            switch (SSMTP_command)
            {
                case "HELO":
                    HELO(argsText);
                    break;

                case "EHLO":
                    EHLO(argsText);
                    break;

                case "AUTH":
                    AUTH(argsText);
                    break;

                //case "MAIL":
                //    MAIL(argsText);
                //    break;

                //case "RCPT":
                //    RCPT(argsText);
                //    break;

                //case "DATA":
                //    DATA(argsText);
                //    break;

                //case "BDAT":
                //    BDAT(argsText);
                //    break;

                //case "RSET":
                //    RSET(argsText);
                //    break;

                //case "VRFY":
                //    VRFY();
                //    break;

                //case "EXPN":
                //    EXPN();
                //    break;

                //case "HELP":
                //    HELP();
                //    break;

                //case "NOOP":
                //    NOOP();
                //    break;

                case "QUIT":
                    QUIT(argsText);
                    return true;

                default:
                    SendData("command unrecognized\r\n");
                    break;
            }

            return false;
        }

        private void HELO(string argsText)
        {
            SendData("250 " + Dns.GetHostName() + " Hello [" + m_ConnectedIP + "]\r\n");
        }

        private void EHLO(string argsText)
        {
            string reply = "" +
                "250-" + Dns.GetHostName() + " Hello [" + m_ConnectedIP + "]\r\n" +
                "250-PIPELINING\r\n" +
                "250-SIZE " + m_SSMTP_Server.MaxMessageSize + "\r\n" +
                "250-8BITMIME\r\n" +
                "250-BINARYMIME\r\n" +
                "250-CHUNKING\r\n" +
                "250-AUTH LOGIN CRAM-MD5\r\n" + //CRAM-MD5 DIGEST-MD5
                "250 Ok\r\n";

            SendData(reply);
        }

        private void QUIT(string argsText)
        {
            if (argsText.Length > 0)
            {
                SendData("500 Syntax error. Syntax:{DATA}\r\n");
                return;
            }

            SendData("221 Service closing transmission channel\r\n");
        }

        private void AUTH(string argsText)
        {

        }


        public string GetArgsText(string input, string cmdTxtToRemove)
        {
            string buff = input.Trim();
            if (buff.Length >= cmdTxtToRemove.Length)
            {
                buff = buff.Substring(cmdTxtToRemove.Length);
            }
            buff = buff.Trim();

            return buff;
        }

        public EndPoint RemoteEndpoint
        {
            get { return m_ClientSocket.RemoteEndPoint; }
        }

        internal string GetIPFromEndPoint(string x)
        {
            return x.Remove(x.IndexOf(":"));
        }

        // Read data from socket
        private string ReadLine()
        {
            string line = ReadLine(m_ClientSocket, 500);

            Console.WriteLine(line + "<CRLF>", m_SessionID, m_ConnectedIP, "C");

            return line;
        }

        private string ReadLine(Socket socket, int maxLen)
        {
            ArrayList lineBuf = new ArrayList();
            byte prevByte = 0;

            while (true)
            {
                if (socket.Available > 0)
                {
                    byte[] currByte = new byte[1];
                    int countReceived = socket.Receive(currByte, 1, SocketFlags.None);
                    if (countReceived == 1)
                    {
                        lineBuf.Add(currByte[0]);

                        if (prevByte == (byte)'\r' && currByte[0] == '\n')
                        {
                            byte[] ret = new byte[lineBuf.Count - 2]; // remove <CRLF>
                            lineBuf.CopyTo(0, ret, 0, lineBuf.Count - 2);

                            return Encoding.Default.GetString(ret).Trim();
                        }

                        prevByte = currByte[0];

                        if (lineBuf.Count > maxLen)
                        {
                            throw new Exception("Maximum line length exceeded");
                        }
                    }
                }
            }
        }


        // Send data to socket
        private void SendData(string data)
        {
            byte[] byte_data = System.Text.Encoding.ASCII.GetBytes(data.ToCharArray());

            int nCount = m_ClientSocket.Send(byte_data, byte_data.Length, 0);
            if (nCount != byte_data.Length)
                throw new Exception("SendData not send enough data as requested!");

            Console.WriteLine(data, m_SessionID, m_ConnectedIP, "Send");
        }
    }
}
