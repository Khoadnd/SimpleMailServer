using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailServer.SIMAP;

namespace MailServer.Core
{
    public partial class ServerCore
    {
        public static void GetMessagesInfo(SIMAPSession ses, SIMAP_Messages e)
        {
            var userName = ses.UserName;

            var userNameParsed = userName.Split('@');
            var domain = userNameParsed[1];
            userName = userNameParsed[0];

            var folderPath = "domains/" + domain + "/" + userName + "/" + e.Mailbox;

            if (!Directory.Exists(folderPath))
                if (e.Mailbox.ToLower().Trim() == "inbox")
                    Directory.CreateDirectory(folderPath);
                else
                    e.Error = "Folder '" + e.Mailbox + "' doesn't exist";

            var files = Directory.GetFiles(folderPath);

            foreach (var file in files)
            {
                var fileParts = Path.GetFileNameWithoutExtension(file).Split('_');
                var receiveDate = new DateTime(long.Parse(fileParts[1]));
                var uid = Convert.ToInt32(fileParts[2], 16);
                SIMAP_MessageFlags flags = (SIMAP_MessageFlags)Enum.Parse(typeof(SIMAP_MessageFlags), fileParts[3]);
                e.AddMessage(Path.GetFullPath(file), uid, flags, receiveDate);
            }
        }

        public static string CreateMailBox(SIMAPSession ses, string mailBox)
        {
            var userName = ses.UserName;

            var userNameParsed = userName.Split('@');
            var domain = userNameParsed[1];
            userName = userNameParsed[0];

            var folderPath = "domains/" + domain + "/" + userName + "/";

            if (Directory.Exists(folderPath + mailBox))
                return "Folder already exists";

            Directory.CreateDirectory(folderPath + mailBox);
            return null;
        }

        public static string DeleteMailBox(SIMAPSession ses, string mailBox)
        {
            var userName = ses.UserName;

            var userNameParsed = userName.Split('@');
            var domain = userNameParsed[1];
            userName = userNameParsed[0];

            var folderPath = "domains/" + domain + "/" + userName + "/";

            if (!Directory.Exists(folderPath + mailBox))
                return "Folder doesn't exists";

            Directory.Delete(folderPath + mailBox);
            return null;
        }

        public static string RenameMailBox(SIMAPSession ses, string mailBox, string newMailBox)
        {
            var userName = ses.UserName;

            var userNameParsed = userName.Split('@');
            var domain = userNameParsed[1];
            userName = userNameParsed[0];

            var folderPath = "domains/" + domain + "/" + userName + "/";

            if (!Directory.Exists(folderPath + mailBox))
                return "Folder doesn't exists";

            Directory.Move(folderPath + mailBox, folderPath + newMailBox);
            return null;
        }

        public static IEnumerable<string> GetMailBoxes(SIMAPSession ses)
        {
            var userName = ses.UserName;

            var userNameParsed = userName.Split('@');
            var domain = userNameParsed[1];
            userName = userNameParsed[0];

            var folderPath = "domains/" + domain + "/" + userName + "/";
            var ret = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);

            for (var i = 0; i < ret.Length; i++)
                ret[i] = ret[i].Replace(folderPath, "");
            
            return ret;
        }

        public static void DeleteMessage(SIMAPSession ses, SIMAP_Message message)
        {
            File.Delete(message.MessageID);
        }
    }
}
