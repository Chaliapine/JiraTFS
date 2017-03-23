namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class notMappedCommand : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.StateChangedCommands", "Command");
        }
        
        public override void Down()
        {
            AddColumn("dbo.StateChangedCommands", "Command", c => c.Int(nullable: false));
        }
    }
}
