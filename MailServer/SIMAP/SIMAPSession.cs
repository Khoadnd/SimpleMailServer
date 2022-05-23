using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MailServer.Core;
using Microsoft.VisualBasic.FileIO;

namespace MailServer.SIMAP
{
    public class SIMAPSession
    {
        private readonly Socket _clientSocket;
        private readonly SIMAPServer _simapServer;
        private readonly string _sessionId = "";
        private string _username = "";
        private string _domain = "";
        private string _connectedIp = "";
        private string _seletedMailbox = "";
        private bool _authenticated = false;
        private SIMAP_Messages _messages = null;

        internal SIMAPSession(Socket clientSocket, SIMAPServer server, string sessionId)
        {
            _clientSocket = clientSocket;
            _simapServer = server;
            _sessionId = sessionId;
        }

        public void StartProcessing()
        {
            try
            {
                _connectedIp = GetIpFromEndPoint(RemoteEndPoint.ToString());

                while (true)
                {
                    if (_clientSocket.Available > 0)
                    {
                        var lastCmd = ReadLine();
                        if (SwitchCommand(lastCmd))
                            break;
                    }
                }
            }
            catch (Exception _)
            {
                if (_clientSocket.Connected)
                    SendData("-> Service not available\r\n");
                else
                    Console.WriteLine("Connection is aborted!", _sessionId, _connectedIp, "x");
                Console.WriteLine("Exception caught: " + _.ToString());
            }
            finally
            {
                _simapServer.RemoveSession(_sessionId);

                if (_clientSocket.Connected)
                    _clientSocket.Close();

                Console.WriteLine("Session " + _sessionId + " disconnected!");
            }
        }

        private bool SwitchCommand(string simapCommandtxt)
        {
            // ----Parse command---------------------------------------------------- //
            var cmdParts = simapCommandtxt.TrimStart().Split(new char[] { ' ' });
            if (cmdParts.Length < 2)
                cmdParts = new string[] { "", "" };
            var commandTag = cmdParts[0].Trim().Trim();
            var command = cmdParts[1].ToUpper().Trim();
            var argsText = ServerCore.GetArgsText(simapCommandtxt, cmdParts[0] + " " + cmdParts[1]);
            // --------------------------------------------------------------------- //

            switch (command)
            {
                // Rfc 3501
                // not-authenticated state
                case "AUTHENTICATE":
                    Authenticate(commandTag, argsText);
                    break;

                case "LOGIN":
                    LogIn(commandTag, argsText);
                    break;
                // End not-authenticated state

                // authenticated state
                case "SELECT":
                    Select(commandTag, argsText);
                    break;

                case "CREATE":
                    Create(commandTag, argsText);
                    break;

                case "DELETE":
                    Delete(commandTag, argsText);
                    break;

                case "RENAME":
                    Rename(commandTag, argsText);
                    break;

                case "STATUS":
                    Status(commandTag, argsText);
                    break;

                // Differ from List in RFC 3501
                case "LIST":
                    List(commandTag, argsText);
                    break;
                // end authenticated state

                // selected state

                case "CLOSE":
                    Close(commandTag);
                    break;

                case "FETCH":
                    Fetch(commandTag, argsText, false);
                    break;

                // end selected state
                default:
                    SendData("->command unrecognized\r\n");
                    Console.WriteLine("-> Unrecognized command");
                    break;
            }

            return false;
        }

        private void Authenticate(string cmdTag, string argsText)
        {
            if (_authenticated)
            {
                SendData(cmdTag + " NO AUTH, already logged in\r\n");
                return;
            }

            string userName = "";
            string password = "";

            switch (argsText.ToUpper())
            {
                case "CRAM-MD5":
                    /* Cram-M5
					C: A0001 AUTH CRAM-MD5
					S: + <md5_calculation_hash_in_base64>
					C: base64(decoded:username password_hash)
					S: A0001 OK CRAM authentication successful
					*/

                    string md5Hash = "<" + Guid.NewGuid().ToString().ToLower() + ">";
                    SendData("+ " + Convert.ToBase64String(Encoding.ASCII.GetBytes(md5Hash)) + "\r\n");

                    string reply = ReadLine();
                    reply = Encoding.Default.GetString(Convert.FromBase64String(reply));
                    string[] replyArgs = reply.Split(' ');
                    userName = replyArgs[0];
                    password = replyArgs[1];

                    if (ServerCore.AuthUser(userName, password))
                    {
                        SendData(cmdTag + " OK Authentication successful.\r\n");
                        _authenticated = true;
                        _username = userName;
                    }
                    else
                        SendData(cmdTag + " NO Authentication failed.\r\n");

                    break;

                default:
                    SendData(cmdTag + "NO unsupported.\r\n");
                    break;
            }
        }

        private void LogIn(string cmdTag, string argsText)
        {
            if (_authenticated)
            {
                SendData(cmdTag + "NO LOGIN already logged in\r\n");
                return;
            }

            var args = ParseParams(argsText);

            if (args.Length != 2)
            {
                SendData(cmdTag + " BAD invalid args.\r\n");
                return;
            }

            var userName = args[0];
            var password = args[1];

            if (ServerCore.AuthUser(userName, password))
            {
                SendData(cmdTag + " OK LOGIN completed.\r\n");
                _username = userName;
                _authenticated = true;
            }
            else
                SendData(cmdTag + " NO LOGIN failed.\r\n");
        }

        private void Select(string cmdTag, string argsText)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            string[] args = ParseParams(argsText);
            if (args.Length != 1)
            {
                SendData(cmdTag + " BAD SELECT invalid args.\r\n");
                return;
            }

            SIMAP_Messages messages = _simapServer.OnGetMessagesInfo(this, args[0]);

            _messages = messages;
            _seletedMailbox = args[0];

            if (_messages.Error == null)
            {

                SendData("* FLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)\r\n");
                SendData("* " + messages.Count + " EXISTS\r\n");
                SendData("* " + messages.RecentCount + " RECENT\r\n");
                SendData("* OK [UNSEEN " + messages.FirstUnseen + "] Message " + messages.FirstUnseen +
                         " is first unseen\r\n");
                SendData("* OK [UIDVALIDITY " + messages.MailboxUID + "] UIDs valid\r\n");
                SendData("* OK [UIDNEXT " + messages.UID_Next + "] Predicted next UID\r\n");
                SendData(
                    "* OK [PERMANENTFLAGS (\\Answered \\Flagged \\Deleted \\Seen \\Draft)] Available permanent flags\r\n");
                SendData(cmdTag + " OK [" + (messages.ReadOnly ? "READ-ONLY" : "READ-WRITE") +
                         "] SELECT Completed\r\n");
            }
            else
                SendData(cmdTag + " BAD SELECT " + _messages.Error + "\r\n");
        }

        private void Create(string cmdTag, string argsText)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            var args = ParseParams(argsText);
            if (args.Length != 1)
            {
                SendData(cmdTag + " BAD CREATE invalid args.\r\n");
                return;
            }

            var error = ServerCore.CreateMailBox(this, args[0]);
            if (error == null)
                SendData(cmdTag + " OK CREATE complete.\r\n");
            else
                SendData(cmdTag + " NO " + error + "\r\n");
        }

        private void Delete(string cmdTag, string argsText)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            var args = ParseParams(argsText);
            if (args.Length != 1)
            {
                SendData(cmdTag + " BAD DELETE invalid args.\r\n");
                return;
            }

            var error = ServerCore.DeleteMailBox(this, args[0]);
            if (error == null)
                SendData(cmdTag + " OK DELETE complete.\r\n");
            else
                SendData(cmdTag + " NO " + error + "\r\n");
        }

        private void Rename(string cmdTag, string argsText)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            var args = ParseParams(argsText);
            if (args.Length != 2)
            {
                SendData(cmdTag + " BAD RENAME invalid args.\r\n");
                return;
            }

            var error = ServerCore.RenameMailBox(this, args[0], args[1]);
            if (error == null)
                SendData(cmdTag + " OK RENAME complete.\r\n");
            else
                SendData(cmdTag + " NO " + error + "\r\n");
        }

        private void Status(string cmdTag, string argsText)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            var args = ParseParams(argsText);
            if (args.Length != 2)
            {
                SendData(cmdTag + " BAD STATUS invalid arguments\r\n");
                return;
            }

            var mailbox = args[0];
            var wantedItems = args[1].ToUpper();

            // See wanted items are valid.
            if (wantedItems.Replace("MESSAGES", "").Replace("RECENT", "").Replace("UIDNEXT", "").Replace("UIDVALIDITY", "").Replace("UNSEEN", "").Trim().Length > 0)
            {
                SendData(cmdTag + " BAD STATUS invalid arguments\r\n");
                return;
            }

            SIMAP_Messages messages = _simapServer.OnGetMessagesInfo(this, mailbox);
            if (messages.Error == null)
            {
                string itemsReply = "";
                if (wantedItems.IndexOf("MESSAGES") > -1)
                    itemsReply += " MESSAGES " + messages.Count;

                if (wantedItems.IndexOf("RECENT") > -1)
                    itemsReply += " RECENT " + messages.RecentCount;

                if (wantedItems.IndexOf("UNSEEN") > -1)
                    itemsReply += " UNSEEN " + messages.UnSeenCount;

                if (wantedItems.IndexOf("UIDVALIDITY") > -1)
                    itemsReply += " UIDVALIDITY " + messages.MailboxUID;

                if (wantedItems.IndexOf("UIDNEXT") > -1)
                    itemsReply += " UIDNEXT " + messages.UID_Next;

                itemsReply = itemsReply.Trim();

                SendData("* STATUS " + messages.Mailbox + " (" + itemsReply + ")\r\n");
                SendData(cmdTag + " OK STATUS completed\r\n");
            }
            else
                SendData(cmdTag + " NO " + messages.Error + "\r\n");
        }

        private void List(string cmdTag, string argsText)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            var args = ParseParams(argsText);
            if (args.Length != 0)
            {
                SendData(cmdTag + " BAD LIST invalid args.\r\n");
                return;
            }

            var folders = ServerCore.GetMailBoxes(this);

            var data = folders.Aggregate((s, r) => r += ' ' + s).Trim();

            SendData(data + "\r\n");
            SendData(cmdTag + " OK LIST completed.\r\n");

            //var refName = args[0];
            //var mailbox = args[1];

            //if (mailbox.Length == 0)
            //    SendData("* LIST (\\Noselect) \"/\" \"\"\r\n");
            //else
            //{
            //    SIMAP_Folders mailboxes = 
            //}
        }

        private void Close(string cmdTag)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            if (_seletedMailbox.Length == 0)
            {
                SendData(cmdTag + " NO Select mailbox first !\r\n");
                return;
            }

            if (!_messages.ReadOnly)
            {
                var messages = _messages.GetDeleteMessages();
                foreach (var msg in messages)
                    _simapServer.OnDeleteMessage(this, msg);
            }

            _seletedMailbox = "";
            _messages = null;

            SendData(cmdTag + " OK CLOSE completed\r\n");
        }

        private void Fetch(string cmdTag, string argsText, bool uidFetch)
        {
            if (!_authenticated)
            {
                SendData(cmdTag + " NO you must authenticate/login first.\r\n");
                return;
            }

            if (_seletedMailbox.Length == 0)
            {
                SendData(cmdTag + " NO Select mailbox first !\r\n");
                return;
            }

            var args = ParseParams(argsText);
            if (args.Length != 2)
            {
                SendData(cmdTag + " BAD Invalid arguments\r\n");
                return;
            }

            // If contains '*' , replace it with total messages number
            var msgSet = args[0].Trim().Replace("*", _messages.Count.ToString()).Split(':');
            var nStartMsg = Convert.ToInt32(msgSet[0]);
            var nEndMsg = Convert.ToInt32(msgSet[0]);

            if (msgSet.Length == 2)
                nEndMsg = Convert.ToInt32(msgSet[1]);

            if (uidFetch)
            {
                nStartMsg = _messages.IndexFromUID(nStartMsg);
                // If end message *, we don't need change it, it ok already
                if (args[0].IndexOf("*") == -1)
                    nEndMsg = _messages.IndexFromUID(nEndMsg);
            }


            if (nEndMsg > _messages.Count)
                nEndMsg = _messages.Count;

            // TODO: HERE <-
        }

        private string[] ParseParams(string argsText)
        {
            ArrayList p = new ArrayList();

            try
            {
                while (argsText.Length > 0)
                {
                    // params is between ""
                    if (argsText.StartsWith("\""))
                    {
                        p.Add(argsText.Substring(1, argsText.IndexOf("\"", 1) - 1));
                        // remove parsed
                        argsText = argsText.Substring(argsText.IndexOf("\"", 1) + 1).Trim();
                    }
                    else
                    {
                        // params is between ()
                        if (argsText.StartsWith("("))
                        {
                            p.Add(argsText.Substring(1, argsText.IndexOf("(", 1) - 1));
                            // remove parsed
                            argsText = argsText.Substring(argsText.IndexOf("(", 1) + 1).Trim();
                        }
                        else
                        {
                            // doc toi khi " ", co the con params o dang sau
                            if (argsText.IndexOf(" ") > -1)
                            {
                                p.Add(argsText.Substring(0, argsText.IndexOf(" ")));
                                // remove parsed 
                                argsText = argsText.Substring(argsText.IndexOf(" ") + 1).Trim();
                            }
                            else
                            {
                                // last
                                p.Add(argsText);
                                argsText = "";
                            }
                        }
                    }
                }
            }
            catch (Exception _)
            {
            }

            var ret = new string[p.Count];
            p.CopyTo(ret);

            return ret;
        }

        private string ReadLine()
        {
            var line = ServerCore.ReadLine(_clientSocket, 500);
            Console.WriteLine(_sessionId + ": " + line + "<CRLF>");

            return line;
        }

        private void SendData(string data)
        {
            var byteData = System.Text.Encoding.ASCII.GetBytes(data.ToCharArray());

            var nCount = _clientSocket.Send(byteData, byteData.Length, 0);
            if (nCount != byteData.Length)
                throw new Exception("SendData not send enough data as requested!");

            Console.WriteLine("-> OK");
        }

        private string GetIpFromEndPoint(string x)
        {
            return x.Remove(x.IndexOf(":"));
        }

        private EndPoint RemoteEndPoint
        {
            get { return _clientSocket.RemoteEndPoint; }
        }

        public string UserName
        {
            get { return _username; }
        }
    }
}
