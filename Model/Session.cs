using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model
{
    public class Session
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { set; get; }

        public DateTime StartedOn { set; get; }
        public DateTime? FinishedOn { set; get; }
        public SyncDirection Direction { set; get; }
        public List<SessionChange> Changes { set; get; }
        public string Errors { set; get; }

        public SessionResult Result { get; set; }
    }
}