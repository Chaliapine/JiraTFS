namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fieldRenaming : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.StateChanges", newName: "StateChangeds");
            RenameColumn(table: "dbo.StateChangedCommands", name: "ChangedStateId", newName: "StateChangedId");
            RenameIndex(table: "dbo.StateChangedCommands", name: "IX_ChangedStateId", newName: "IX_StateChangedId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.StateChangedCommands", name: "IX_StateChangedId", newName: "IX_ChangedStateId");
            RenameColumn(table: "dbo.StateChangedCommands", name: "StateChangedId", newName: "ChangedStateId");
            RenameTable(name: "dbo.StateChangeds", newName: "StateChanges");
        }
    }
}
