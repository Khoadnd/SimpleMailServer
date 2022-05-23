using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace MailServer.SSMTP
{
    public class SSMTPServer
    {
        private TcpListener? ssmtpListener = null;
        private Hashtable sessionTable = null;
        private string ipAddress = "All"; // IP address of server
        private int port = 25; // Port of server
        private int maxThread = 20; // Maximum of worker thread
        private bool enable = false; // State of listener
        private int maxMessageSize = 1000000; // Maximun message size
        private int maxRecipients = 100; // Max recipients

        public SSMTPServer()
        {
            InitializeComponent();
        }

        // deprecated
        private void InitializeComponent()
        {
            //domainList = new List<string>();
            //Directory.CreateDirectory("queue");
            //Directory.CreateDirectory("domains");
            //if (!File.Exists("domains.txt"))
            //    File.Create("domains.txt").Close();


            //foreach (var domain in File.ReadAllLines("domains.txt"))
            //{
            //    domainList.Add(domain);
            //    Directory.CreateDirectory("domains/" + domain);

            //    if (!File.Exists("domains/" + domain + "/userdata.txt"))
            //        File.Create("domains/" + domain + "/userdata.txt").Close();


            //    foreach (var userdata in File.ReadAllLines("domains/" + domain + "/userdata.txt"))
            //    {
            //        var username = userdata.Trim().Split(':')[0];
            //        Directory.CreateDirectory("domains/" + domain + "/" + username);
            //        Directory.CreateDirectory("domains/" + domain + "/" + username + "/Inbox");
            //        Directory.CreateDirectory("domains/" + domain + "/" + username + "/Sent");
            //        Directory.CreateDirectory("domains/" + domain + "/" + username + "/Drafts");
            //        Directory.CreateDirectory("domains/" + domain + "/" + username + "/Trash");
            //    }
            //}
        }

        ~SSMTPServer()
        {
            Stop();
        }

        public void Start()
        {
            if (enable) return;
            sessionTable = new Hashtable();

            var startSSMTPServer = new Thread(new ThreadStart(Run));
            startSSMTPServer.Start();
        }

        private void Stop()
        {
            ssmtpListener?.Stop();
        }

        private void Run()
        {
            ssmtpListener = ipAddress.Equals("All")
                             ? new TcpListener(IPAddress.Any, port)
                             : new TcpListener(IPAddress.Parse(ipAddress), port);
            ssmtpListener.Start();

            while (true)
            {
                if (sessionTable.Count <= maxThread)
                {
                    var clientSocket = ssmtpListener.AcceptSocket();
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
            sessionTable.Add(sessionID, session);

            Console.WriteLine("SSMTP Session: " + sessionID + " added " + DateTime.Now);
        }

        internal void RemoveSession(string sessionID)
        {
            lock (sessionTable)
            {
                if (!sessionTable.Contains(sessionID))
                {
                    Console.WriteLine("Session " + sessionID + " doesn't exists.");
                    return;
                }
                sessionTable.Remove(sessionID);
            }
        }

        // deprecated
        //internal bool AuthUser(string userDomain, string password)
        //{
        //    if (!userDomain.Contains('@'))
        //        return false;

        //    var domain = userDomain.Trim().Split('@')[1];
        //    var userName = userDomain.Trim().Split('@')[0];
        //    if (domain.Length == 0)
        //        return false;

        //    if (!domainList.Contains(domain))
        //        return false;

        //    var userdata = File.ReadAllLines("domains/" + domain + "/userdata.txt");
        //    foreach (var user in userdata)
        //    {
        //        var userAndPassword = user.Split(new char[] { ':' });
        //        if (userName.Equals(userAndPassword[0]) && password.Equals(userAndPassword[1])) ;
        //        return true;
        //    }
        //    return false;
        //}

        // deprecated
        //internal bool ValidateUser(string userDomain)
        //{
        //    if (!userDomain.Contains('@'))
        //        return false;

        //    var domain = userDomain.Trim().Split('@')[1];
        //    var username = userDomain.Trim().Split('@')[0];

        //    if (domain.Length == 0)
        //        return false;

        //    if (!domainList.Contains(domain))
        //        return false;

        //    var userdata = File.ReadAllLines("domains/" + domain + "/userdata.txt");
        //    foreach (var user in userdata)
        //        if (username.Equals(user.Split(':')[0]))
        //            return true;

        //    return false;
        //}

        // deprecated
        //public void AddUserToDomain(string username, string password, string domain)
        //{
        //    File.AppendAllText("domains/" + domain + "/userdata.txt", username + ":" + password + Environment.NewLine);
        //    Directory.CreateDirectory("domains/" + domain + "/" + username);
        //    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Inbox");
        //    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Sent");
        //    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Drafts");
        //    Directory.CreateDirectory("domains/" + domain + "/" + username + "/Trash");
        //}

        public int MaxMessageSize
        {
            get { return maxMessageSize; }
        }

        public int MaxRecipients
        {
            get { return maxRecipients; }
        }
    }
}
