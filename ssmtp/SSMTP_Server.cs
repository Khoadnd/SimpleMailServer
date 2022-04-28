using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace ssmtp
{
    public class SSMTP_Server : IDisposable
    {
        private TcpListener? SSmtp_Listener = null;
        private Hashtable m_SessionTable = null;
        private string m_IPAddress = "All"; // IP address of server
        private int m_Port = 25; // Port of server
        private int m_MaxThread = 20; // Maximum of worker thread
        private bool m_enable = false; // State of listener
        private int m_MaxMessageSize = 1000000; // Maximun message size
        private int m_MaxRecipients = 100; // Max recipients

        public SSMTP_Server()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {

        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            if (!m_enable)
            {
                m_SessionTable = new Hashtable();

                Thread startSSMTPServer = new Thread(new ThreadStart(Run));
                startSSMTPServer.Start();
            }
        }

        private void Stop()
        {
            if (SSmtp_Listener != null)
                SSmtp_Listener.Stop();
        }

        private void Run()
        {
            if (m_IPAddress.Equals("All"))
                SSmtp_Listener = new TcpListener(IPAddress.Any, m_Port);
            else
                SSmtp_Listener = new TcpListener(IPAddress.Parse(m_IPAddress), m_Port);
            SSmtp_Listener.Start();

            while(true)
            {
                if (m_SessionTable.Count <= m_MaxThread)
                {
                    Socket clientSocket = SSmtp_Listener.AcceptSocket();
                    string sessionID = clientSocket.GetHashCode().ToString();

                    SSMTP_Session session = new SSMTP_Session(clientSocket, this, sessionID);

                    Thread clientThread = new Thread(new ThreadStart(session.StartProcessing));
                    AddSession(sessionID, session);
                    clientThread.Start();
                }
                else
                    Thread.Sleep(100);
            }
        }

        internal void AddSession(string sessionID, SSMTP_Session session)
        {
            m_SessionTable.Add(sessionID, session);

            Console.WriteLine("Session: " + sessionID + " added " + DateTime.Now);
        }

        internal void RemoveSession(string sessionID)
        {
            lock(m_SessionTable)
            {
                if (!m_SessionTable.Contains(sessionID))
                {
                    Console.WriteLine("Session " + sessionID + " doesn't exists.");
                    return;
                }
                m_SessionTable.Remove(sessionID);
            }
        }

        public int MaxMessageSize
        {
            get { return m_MaxMessageSize; }
        }
    }
}
