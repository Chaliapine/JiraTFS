namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ColumnRenaming : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.StateChangedCommands", name: "CommandString", newName: "Command");
        }
        
        public override void Down()
        {
            RenameColumn(table: "dbo.StateChangedCommands", name: "Command", newName: "CommandString");
        }
    }
}
