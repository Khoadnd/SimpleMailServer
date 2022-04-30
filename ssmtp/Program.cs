using ssmtp;

namespace Program
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // initialize server
            SSMTP_Server server = new SSMTP_Server();
            server.Start();

            // initialize mail delivery system
            Core.MsgDelivery.CreateQueueWatcher();
        }
    }
}