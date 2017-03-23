using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public class StateChangedCommand
	{
		public long Id { get; set; }

		public long? StateChangeId { get; set; }
		public StateChange StateChange { get; set; }
		[MaxLength(500), Column("Command")]
		public string CommandString
		{
			get { return Command.ToString(); }
			set { Command = value.ParseEnum<Command>(); }
		}
		[NotMapped]
		public Command Command { get; set; }
		[MaxLength(500)]
		public string FieldName { get; set; }
		[MaxLength(500)]
		public string ChangeTo { get; set; }
		public int Order { get; set; }
	}
}
