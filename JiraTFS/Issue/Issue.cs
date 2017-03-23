using System;

namespace JiraTFS
{
	public class Issue : IssueRef
	{
		public Issue()
		{
			Fields = new IssueFields();
		}

		public string Expand { get; set; }
		public string Self { get; set; }

		public IssueFields Fields { get; set; }
		public IssueRenderedFields RenderedFields { get; set; }
		public Changelog Changelog { get; set; }


		public DateTime LastUpdate
		{
			get { return Convert.ToDateTime(Fields.Updated); }
			set { Fields.Updated = value.ToString(); }
		}

		public string State
		{
			get { return Fields.Status.Name; }
			set { Fields.Status.Name = value; }
		}
	}
}
