using ssmtp;

namespace Program
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            SSMTP_Server server = new SSMTP_Server();
            server.Start();
        }
    }
}