using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailServer.SIMAP
{
	public class SIMAP_Folders
	{
		private ArrayList mailboxes = null;
		private string refName = "";
		private string mailbox = "";

		public SIMAP_Folders(string referenceName, string folder)
		{
			mailboxes = new ArrayList();
			refName = referenceName;
			mailbox = folder;
		}

		public void Add(string folder)
		{
			if (refName.Length > 0)
				if (!folder.ToLower().StartsWith(refName.ToLower()))
					return;

			if (mailbox.StartsWith("*") && mailbox.EndsWith("*"))
			{
				if (folder.ToLower().IndexOf(mailbox.Replace("*", "").ToLower()) > -1)
					mailboxes.Add(new SIMAP_Folder(folder));
				
				return;
			}

			if (mailbox.IndexOf("*") == -1 && mailbox.IndexOf("%") == -1 && mailbox.ToLower() != folder.ToLower())
				return;
			


			mailboxes.Add(new SIMAP_Folder(folder));
		}

		public SIMAP_Folder[] Folders
		{
			get
			{
				SIMAP_Folder[] retVal = new SIMAP_Folder[mailboxes.Count];
				mailboxes.CopyTo(retVal);
				return retVal;
			}
		}
	}
}
