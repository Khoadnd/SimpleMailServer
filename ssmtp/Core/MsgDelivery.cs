using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core
{
    public static class MsgDelivery
    {
        public static void CreateQueueWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Filter = "*.*";
            watcher.Path = "queue/";
            watcher.EnableRaisingEvents = true;
            watcher.Created += new FileSystemEventHandler(watcher_FileCreated);

        }

        private static void watcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            // deliver mail stuff
            var exp = @"(?<param>TO)[\s]{0,}:\s{0,}(?<value>[\w\@\.\-\*\+\=\#\/\s]*)";
            var r = new Regex(exp, RegexOptions.IgnoreCase);
            Match tmp = Match.Empty;

            foreach (var line in File.ReadLines(e.FullPath))
                if ((tmp = r.Match(line)).Success)
                    break;

            var forwardPath = tmp.Result("${value}").Trim().Split(' ');
            foreach (var path in forwardPath)
            {
                var username = path.Trim().Split('@')[0];
                var domain = path.Trim().Split('@')[1];
                File.Copy(e.FullPath, "domains/" + domain + "/" + username + "/Inbox/" + e.Name, true);
                Console.WriteLine("Delivered mail to " + path);
            }
            //File.Delete(e.FullPath);
        }
    }
}
