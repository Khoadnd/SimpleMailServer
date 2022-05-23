
namespace MailServer.Core
{
    public static partial class ServerCore
    {
        private static Dictionary<string, List<string>> domainAndUserList;
        //public static List<string> domainList = null;

        public static void InitializeComponent()
        {
            domainAndUserList = new Dictionary<string, List<string>>();

            Directory.CreateDirectory("queue");
            Directory.CreateDirectory("domains");
            if (!File.Exists("Domains.txt"))
                File.Create("domains.txt");

            foreach (var domain in File.ReadAllLines("domains.txt"))
            {
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

                    domainAndUserList.AddOrUpdate(domain, userdata);
                }
            }
        }

        private static void AddOrUpdate(this Dictionary<string, List<string>>? dic, string key, string entry)
        {
            if (!dic.ContainsKey(key))
                dic.Add(key, new List<string>());

            dic[key].Add(entry);
        }

        public static bool AuthUser(string userDomain, string pass)
        {
            if (!userDomain.Contains('@'))
                return false;

            var domain = userDomain.Trim().Split('@')[1];
            var username = userDomain.Trim().Split('@')[0];

            return domain.Length != 0 &&
                   domainAndUserList.ContainsKey(domain) &&
                   domainAndUserList[domain].Contains(username + ":" + pass);
        }

        public static bool ValidateUser(string userDomain)
        {
            if (!userDomain.Contains('@'))
                return false;

            var domain = userDomain.Trim().Split('@')[1];
            var username = userDomain.Trim().Split('@')[0];

            return domain.Length != 0 &&
                   domainAndUserList.ContainsKey(domain) &&
                   domainAndUserList[domain].Any(x => x.Split(':')[0] == username);
        }

        public static void AddDomain(string domain)
        {
            if (domainAndUserList.ContainsKey(domain))
            {
                Console.WriteLine("Warning: " + domain + " already exists!");
                throw new Exception("domain already exists!");
            }
            else
            {
                File.AppendAllText("domains.txt", domain + Environment.NewLine);
                Directory.CreateDirectory("domains/" + domain);
                domainAndUserList.Add(domain, new List<string>());
            }
        }

        public static void AddUserToDomain(string username, string password, string domain)
        {
            if (!domainAndUserList.ContainsKey(domain))
            {
                Console.WriteLine("Warning: " + domain + " not found!");
                throw new Exception("domain not found!");
            }

            if (domainAndUserList[domain].Any(x => x.Split(':')[0] == username))
            {
                Console.WriteLine("Warning: " + username + " already exists!");
                throw new Exception("username already exists");
            }
            File.AppendAllText("domains/" + domain + "/userdata.txt", username + ":" + password + Environment.NewLine);
            Directory.CreateDirectory("domains/" + domain + "/" + username);
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Inbox");
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Sent");
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Drafts");
            Directory.CreateDirectory("domains/" + domain + "/" + username + "/Trash");
        }
    }
}