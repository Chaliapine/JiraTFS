using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model
{
    public class Bug
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long? Id { set; get; }

        public string TFSId { set; get; }
		[MaxLength(100)]
        public string JiraId { set; get; }
    }
}