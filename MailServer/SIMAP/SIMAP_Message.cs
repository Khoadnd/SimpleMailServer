using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.SIMAP
{
    public class SIMAP_Message
    {
        private SIMAP_Messages messages = null;
        private string messageID = "";
        private int UID = 1;
        private SIMAP_MessageFlags flags;
        private DateTime date;

        internal SIMAP_Message(SIMAP_Messages messages, string messageID, int UID, SIMAP_MessageFlags flags,
            DateTime date)
        {
            this.messages = messages;
            this.messageID = messageID;
            this.UID = UID;
            this.flags = flags;
            this.date = date;
        }

        public string FlagsToString()
        {
            var ret = "";
            if (((int)SIMAP_MessageFlags.Answered & (int)this.flags) != 0)
                ret += " \\ANSWERED";

            if (((int)SIMAP_MessageFlags.Flagged & (int)this.flags) != 0)
                ret += " \\FLAGGED";

            if (((int)SIMAP_MessageFlags.Deleted & (int)this.flags) != 0)
                ret += " \\DELETED";

            if (((int)SIMAP_MessageFlags.Seen & (int)this.flags) != 0)
                ret += " \\SEEN";

            if (((int)SIMAP_MessageFlags.Draft & (int)this.flags) != 0)
                ret += " \\DRAFT";


            return ret.Trim();
        }

        internal void SetFlags(SIMAP_MessageFlags flags)
        {
            this.flags = flags;
        }

        public int MessageNo
        {
            get
            {
                if (messages != null)
                {
                    return messages.IndexOf(this);
                }
                else
                {
                    return -1;
                }
            }
        }

        public string MessageID
        {
            get { return messageID; }

            set { messageID = value; }
        }

        public int MessageUID
        {
            get { return UID; }
        }

        public SIMAP_MessageFlags Flags
        {
            get { return flags; }
        }

        public DateTime Date
        {
            get { return date; }
        }
    }
}
