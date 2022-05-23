using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.SIMAP
{
    public enum SIMAP_MessageFlags
    {
        Seen = 2,
        Answered = 4,
        Flagged = 8,
        Deleted = 16,
        Draft = 32,
        Recent = 64,
    };
}
