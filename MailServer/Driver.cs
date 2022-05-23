using MailServer.Core;

namespace MailServer
{
    public class Driver
    {
        public static void Main(string[] args)
        {
            ServerCore.InitializeComponent();

            // initialize server
            SSMTP.SSMTPServer ssmtpServer = new SSMTP.SSMTPServer();
            ssmtpServer.Start();

            SIMAP.SIMAPServer simapServer = new SIMAP.SIMAPServer();
            simapServer.Start();

            // initialize mail delivery system
            MsgDelivery.CreateQueueWatcher();
        }
    }
}
