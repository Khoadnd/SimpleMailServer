using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.IO;
using System.Reflection.PortableExecutable;

namespace SSMTP
{
    public class SSMTPServer
    {
        private TcpListener? SSmtp_Listener = null;
        private Hashtable m_SessionTable = null;
        private string m_IPAddress = "All"; // IP address of server
        private int m_Port = 25; // Port of server
        private int m_MaxThread = 20; // Maximum of worker thread
        private bool m_enable = false; // State of listener
        private int m_MaxMessageSize = 1000000; // Maximun message size
        private int m_MaxRecipients = 100; // Max recipients
        private List<string> m_domainList = null;

        public SSMTPServer()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            m_domainList = new List<string>();
            Directory.CreateDirectory("queue");
            Directory.CreateDirectory("domains");
            if (!File.Exists("domains.txt"))
                File.Create("domains.txt").Close();
            

            foreach (var domain in File.ReadAllLines("domains.txt"))
            {
                m_domainList.Add(domain);
                Directory.CreateDirectory("domains/" + domain);

                if (!File.Exists("domains/" + domain + "/userdata.txt"))
                    File.Create("domains/" + domain + "/userdata.txt").Close();


                foreach (var userdata in File.ReadAllLines("domains/" + domain + "/userdata.txt"))
                {
                    var username = userdata.Trim().Split(':')[0];
                    Directory.CreateDirectory("domains/" + domain + "/" + username);
                    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Inbox");
                    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Sent");
                    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Drafts");
                    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Trash");
                }
            }
        }

        ~SSMTPServer()
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

            while (true)
            {
                if (m_SessionTable.Count <= m_MaxThread)
                {
                    var clientSocket = SSmtp_Listener.AcceptSocket();
                    var sessionID = clientSocket.GetHashCode().ToString();

                    var session = new SSMTPSession(clientSocket, this, sessionID);

                    var clientThread = new Thread(new ThreadStart(session.StartProcessing));
                    AddSession(sessionID, session);
                    clientThread.Start();
                }
                else
                    Thread.Sleep(100);
            }
        }

        private void AddSession(string sessionID, SSMTPSession session)
        {
            m_SessionTable.Add(sessionID, session);

            Console.WriteLine("Session: " + sessionID + " added " + DateTime.Now);
        }

        internal void RemoveSession(string sessionID)
        {
            lock (m_SessionTable)
            {
                if (!m_SessionTable.Contains(sessionID))
                {
                    Console.WriteLine("Session " + sessionID + " doesn't exists.");
                    return;
                }
                m_SessionTable.Remove(sessionID);
            }
        }

        internal bool AuthUser(string userDomain, string password)
        {
            if (!userDomain.Contains('@'))
                return false;

            var domain = userDomain.Trim().Split('@')[1];
            var userName = userDomain.Trim().Split('@')[0];
            if (domain.Length == 0)
                return false;

            if (!m_domainList.Contains(domain))
                return false;

            var userdata = File.ReadAllLines("domains/" + domain + "/userdata.txt");
            foreach (var user in userdata)
            {
                var userAndPassword = user.Split(new char[] { ':' });
                if (userName.Equals(userAndPassword[0]) && password.Equals(userAndPassword[1])) ;
                return true;
            }
            return false;
        }

        internal bool ValidateUser(string userDomain)
        {
            if (!userDomain.Contains('@'))
                return false;

            var domain = userDomain.Trim().Split('@')[1];
            var username = userDomain.Trim().Split('@')[0];

            if (domain.Length == 0)
                return false;

            if (!m_domainList.Contains(domain))
                return false;

            var userdata = File.ReadAllLines("domains/" + domain + "/userdata.txt");
            foreach (var user in userdata)
                if (username.Equals(user.Split(':')[0]))
                    return true;

            return false;
        }

        public void AddDomain(string domain)
        {
            File.AppendAllText("domains.txt", domain + Environment.NewLine);
            Directory.CreateDirectory("domains/" + domain);
            m_domainList.Add(domain);
        }

        public void AddUserToDomain(string username, string password, string domain)
        {
            File.AppendAllText("domains/" + domain + "/userdata.txt", username + ":" + password + Environment.NewLine);
            Directory.CreateDirectory("domains/" + domain + "/" + username);
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Inbox");
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Sent");
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Drafts");
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Trash");
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
