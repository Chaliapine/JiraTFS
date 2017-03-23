using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model
{
    public class SessionChange
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string TFSRevision { get; set; }
        public string JiraChangelog { get; set; }
        public DateTime? Date { get; set; }
        public long? SessionId { get; set; }
        public Session Session { get; set; }
        public long? BugID { set; get; }
        public Bug Bug { set; get; }
		public string Operation { get; set; }
        public string Errors { get; set; }
        public SyncState? SyncState { get; set; }
    }
}