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
            if (m_enable) return;
            m_SessionTable = new Hashtable();

            var startSSMTPServer = new Thread(new ThreadStart(Run));
            startSSMTPServer.Start();
        }

        private void Stop()
        {
            SSmtp_Listener?.Stop();
        }

        private void Run()
        {
            SSmtp_Listener = m_IPAddress.Equals("All")
                             ? new TcpListener(IPAddress.Any, m_Port)
                             : new TcpListener(IPAddress.Parse(m_IPAddress), m_Port);
            SSmtp_Listener.Start();

            while(true)
            {
                if (m_SessionTable.Count <= m_MaxThread)
                {
                    var clientSocket = SSmtp_Listener.AcceptSocket();
                    var sessionID = clientSocket.GetHashCode().ToString();

                    var session = new SsmtpSession(clientSocket, this, sessionID);

                    var clientThread = new Thread(new ThreadStart(session.StartProcessing));
                    AddSession(sessionID, session);
                    clientThread.Start();
                }
                else
                    Thread.Sleep(100);
            }
        }

        private void AddSession(string sessionID, SsmtpSession session)
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

        internal bool AuthUser(string userName, string password)
        {
            if (!File.Exists("userdata.txt"))
                File.Create("userdata.txt");
            var userdata = File.ReadAllLines("userdata.txt");
            foreach (var user in userdata)
            {
                var userAndPassword = user.Split(new char[] { ':' });
                if (userName.Equals(userAndPassword[0]) && password.Equals(userAndPassword[1]))
                    return true;
            }
            // to do
            return false;
        }

        public int MaxMessageSize
        {
            get { return m_MaxMessageSize; }
        }

        public int MaxRecipients
        {
            get { return m_MaxRecipients; }
        }
    }
}
