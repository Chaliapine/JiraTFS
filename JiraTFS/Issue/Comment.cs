using System;
using Newtonsoft.Json;

namespace JiraTFS
{
	public class Comment
	{
		public string Id { get; set; }
		public string body { get; set; }
		public string renderedBody { get; set; }
		public JiraUser Author { get; set; }
		public string Updated { get; set; }
		public DateTime UpdatedTime
		{
			get { return Convert.ToDateTime(Updated); }
			set { Updated = value.ToString(); }

		}
	}
}
