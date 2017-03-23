using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TfsAttachment = Microsoft.TeamFoundation.WorkItemTracking.Client.Attachment;

namespace JiraTFS
{
	public static class Extensions
	{
		public static List<string> GetHistory(this WorkItem workItem)
		{
			var retList = new List<string>();
			if (workItem == null) throw new NullReferenceException();
			foreach (Revision revision in workItem.Revisions)
			{
				if (!string.IsNullOrWhiteSpace(revision.Fields["History"].Value.ToString()))
					retList.Add(revision.Fields["History"].Value.ToString());
			}
			return retList;
		}

		public static bool EqualToComment(this string first, string second)
		{
			var s1 = first.RemoveSpecialChars();
			var s2 = second.RemoveSpecialChars();
			return s1 == s2 || s2.EndsWith(s1);
		}

		public static bool KeyGreaterThan(this string first, string second)
		{
			var firstKeyNum = int.Parse(first.Split('-')[1]);
			var secondKeyNum = int.Parse(second.Split('-')[1]);
			return firstKeyNum > secondKeyNum;
		}
		public static string RemoveSpecialChars(this string str)
		{
			var charsToRemove = new[] { "\r", "\t", "\n", " ","\"" };
			foreach (var ch in charsToRemove)
			{
				str = str.Replace(ch, string.Empty);
			}
			return str;
		}

		public static string RemakeBadTags(this string str)
		{
			return str.Replace("<br/>", "<br>").Replace("\"", "&quot;");
		}
		public static Revision GetLastRevision(this WorkItem workItem)
		{
			return workItem.Revisions.Cast<Revision>().LastOrDefault();
		}

		public static void ClearFolder(this DirectoryInfo directory)
		{
			foreach (var file in directory.GetFiles()) file.Delete();
			foreach (var subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
		}
	}
}
