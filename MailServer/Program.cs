
namespace Program
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // initialize server
            SSMTP.SSMTPServer server = new SSMTP.SSMTPServer();
            server.Start();

            // initialize mail delivery system
            Core.MsgDelivery.CreateQueueWatcher();
        }
    }
}