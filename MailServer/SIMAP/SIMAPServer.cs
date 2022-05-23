using System.Collections;
using System.Net;
using System.Net.Sockets;
using MailServer.Core;

namespace MailServer.SIMAP
{
  public class SIMAPServer
  {
    private TcpListener? simapListener = null;
    private Hashtable sessionTable = null;
    private string ipAddress = "All";
    private int port = 143;
    private int maxThread = 20;
    private bool enable = false;

    public SIMAPServer()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {

    }

    public void Start()
    {
      if (enable) return;
      sessionTable = new Hashtable();

      Thread startServer = new Thread(new ThreadStart(Run));
      startServer.Start();
    }

    public void Stop()
    {
      simapListener?.Stop();
    }

    private void Run()
    {
      simapListener = ipAddress.Equals("All")
          ? new TcpListener(IPAddress.Any, port)
          : new TcpListener(IPAddress.Parse(ipAddress), port);
      simapListener.Start();

      while (true)
      {
        if (sessionTable.Count <= maxThread)
        {
          var clientSocket = simapListener.AcceptSocket();
          var sessionID = clientSocket.GetHashCode().ToString();

          var session = new SIMAPSession(clientSocket, this, sessionID);

          var clientThread = new Thread(new ThreadStart(session.StartProcessing));
          AddSession(sessionID, session);
          clientThread.Start();
        }
        else
          Thread.Sleep(100);
      }
    }

    private void AddSession(string sessionID, SIMAPSession session)
    {
      sessionTable.Add(sessionID, session);
      Console.WriteLine("SIMAP Session: " + sessionID + " added " + DateTime.Now);
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

    internal SIMAP_Messages OnGetMessagesInfo(SIMAPSession session, string mailbox)
    {
      SIMAP_Messages messages = new SIMAP_Messages(mailbox);

      ServerCore.GetMessagesInfo(session, messages);

      return messages;
    }

    internal void OnDeleteMessage(SIMAPSession session, SIMAP_Message message)
    {
      ServerCore.DeleteMessage(session, message);
    }
  }
}
