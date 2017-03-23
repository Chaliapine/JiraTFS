using System;

namespace JiraTFS
{
	public class IssueRef
	{
		public string Id { get; set; }
		public string Key { get; set; }

		internal string JiraIdentifier
		{
			get { return String.IsNullOrWhiteSpace(Id) ? Key : Id; }
		}
	}
}
