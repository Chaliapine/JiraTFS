using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JiraTFS
{
	public class IssueFields
	{
		public IssueFields()
		{
			Status = new SimpleField();
			Labels = new List<String>();
			Comment = new CommentsContainer();
			Attachment = new List<Attachment>();
		}

		public String Summary { get; set; }
		public String Updated { get; set; }
		public String Description { get; set; }
		[JsonProperty("CustomField_10400")]
		public String Assembly { get; set; }
		[JsonProperty("CustomField_10401")]
		public SimpleField Release { get; set; }
		public SimpleField Status { get; set; }
		public SimpleField Priority { get; set; }
		public JiraUser Reporter { get; set; }
		public JiraUser Assignee { get; set; }
		public JiraUser Creator { get; set; }
		public List<String> Labels { get; set; }
		public CommentsContainer Comment { get; set; }
		public List<Attachment> Attachment { get; set; }
	}
}
