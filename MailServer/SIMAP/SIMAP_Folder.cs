using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.SIMAP
{
    public class SIMAP_Folder
    {
        public string Folder { get; } = "";
        public SIMAP_Folder(string folder)
        {
            this.Folder = folder;
        }

    }
}
