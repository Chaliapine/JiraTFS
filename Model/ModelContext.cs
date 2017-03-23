using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Model
{
    public class ModelContext : DbContext
    {
        public ModelContext() : base("DbConnection")
        {
        }

        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionChange> SessionChanges { get; set; }
        public DbSet<Bug> Bugs { get; set; }
		public DbSet<ConnectionData> ConfigTable { get; set; }
		public DbSet<StateChange> StateChanged { get; set; }
		public DbSet<StateChangedCommand> StateChangedCommands { get; set; }
    }
}