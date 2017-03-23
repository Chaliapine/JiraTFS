using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public class StateChange
	{
		[Key]
		public long Id { get; set; }
		[Column("TrackerName")]
		public string TrackerNameString {
			get { return TrackerName.ToString(); }
			set { TrackerName = value.ParseEnum<Tracker>(); }
		}
		[NotMapped]
		public Tracker TrackerName { get; set; }
		[MaxLength(500)]
		public string FromState { get; set; }
		[MaxLength(500)]
		public string ToState { get; set; }
		[Column("Condition")]
		public string ConditionString
		{
			get { return Condition.ToString(); }
			set { Condition = value.ParseEnum<Condition>(); }
		}
		[NotMapped]
		public Condition Condition { get; set; }
		public bool ConditionValue { get; set; }

	}
}
