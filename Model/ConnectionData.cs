using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model
{
	[Table("ConfigTable")]
	public class ConnectionData
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int? Id { get; set; }
		public string JiraAddress { get; set; }
		public string JiraLogin { get; set; }
		public string JiraPassword { get; set; }
		public string JiraProjectName { get; set; }
		public DateTime? JiraDateFrom { get; set; }

		public string TFSAddress { get; set; }
		public string TFSLogin { get; set; }
		public string TFSPassword { get; set; }
		public string TFSProjectName { get; set; }
		public DateTime? TFSDateFrom { get; set; }
	}
}
