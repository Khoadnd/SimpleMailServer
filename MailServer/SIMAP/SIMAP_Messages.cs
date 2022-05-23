using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.SIMAP
{
    public class SIMAP_Messages
    {
        private SortedList messages = null;
        public string Error { get; set; } = null;
        public string Mailbox { get; } = "";
        public int MailboxUID { get; set; } = 420690;
        public bool ReadOnly { get; set; } = false;

        public SIMAP_Messages(string folder)
        {
            this.Mailbox = folder;
            messages = new SortedList();
        }

        public void AddMessage(string messageID, int UID, SIMAP_MessageFlags flags, DateTime date)
        {
            messages.Add(UID, new SIMAP_Message(this, messageID, UID, flags, date));
        }

        public int IndexOf(SIMAP_Message message)
        {
            return messages.IndexOfValue(message) + 1;
        }

        public int IndexFromUID(int uid)
        {
            var ret = 0;
            foreach (SIMAP_Message msg in messages.GetValueList())
            {
                ret++;
                if (msg.MessageUID == uid)
                    return ret;
            }

            return 1;
        }

        public SIMAP_Message[] GetDeleteMessages()
        {
            var retVal = new ArrayList();
            foreach (SIMAP_Message msg in this.messages.GetValueList())
            {
                if (((int)SIMAP_MessageFlags.Deleted & (int)msg.Flags) != 0)
                {
                    retVal.Add(msg);
                }
            }

            var messages = new SIMAP_Message[retVal.Count];
            retVal.CopyTo(messages);

            return messages;
        }

        public SIMAP_Message? this[int msgNo] => (SIMAP_Message)messages.GetByIndex(msgNo)!;

        public int FirstUnseen
        {
            get
            {
                for (var i = 0; i < messages.Count; i++)
                {
                    var msg = (SIMAP_Message)messages.GetByIndex(i)!;
                    if (((int)SIMAP_MessageFlags.Recent & (int)msg.Flags) != 0)
                        return i + 1;
                }

                return 0;
            }
        }

        public int UnSeenCount
        {
            get
            {
                return messages.GetValueList().Cast<SIMAP_Message>().Count(msg => ((int)SIMAP_MessageFlags.Seen & (int)msg.Flags) == 0);
            }
        }

        public int RecentCount
        {
            get
            {
                return messages.GetValueList().Cast<SIMAP_Message>().Count(msg => ((int)SIMAP_MessageFlags.Recent & (int)msg.Flags) != 0);
            }
        }

        public int DeleteCount
        {
            get
            {
                return messages.GetValueList().Cast<SIMAP_Message>().Count(msg => ((int)SIMAP_MessageFlags.Deleted & (int)msg.Flags) != 0);
            }
        }

        public int Count => messages.Count;

        public int UID_Next
        {
            get
            {
                if (messages.Count > 0)
                    return (((SIMAP_Message)messages.GetByIndex(messages.Count - 1)!)!).MessageUID + 1;


                return 1;

            }
        }

    }
}
