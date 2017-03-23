namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class renamingFail : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.StateChangeds", newName: "StateChanges");
            RenameColumn(table: "dbo.StateChangedCommands", name: "StateChangedId", newName: "StateChangeId");
            RenameIndex(table: "dbo.StateChangedCommands", name: "IX_StateChangedId", newName: "IX_StateChangeId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.StateChangedCommands", name: "IX_StateChangeId", newName: "IX_StateChangedId");
            RenameColumn(table: "dbo.StateChangedCommands", name: "StateChangeId", newName: "StateChangedId");
            RenameTable(name: "dbo.StateChanges", newName: "StateChangeds");
        }
    }
}
