using System.Collections.Generic;

namespace JiraTFS
{
	public class History
	{
		public int Id { get; set; }
		public JiraUser Author { get; set; }
		public string Created { get; set; }
		public List<Item> Items { get; set; }

	}
}
