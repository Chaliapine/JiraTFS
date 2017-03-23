using System.Collections.Generic;

namespace JiraTFS
{
	class IssuesContainer
	{
		public int MaxResults { get; set; }
		public int Total { get; set; }
		public int StartAt { get; set; }
		public List<Issue> Issues { get; set; }
	}
}
