namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class classRenaming : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.StateChangeds", newName: "StateChanges");
        }
        
        public override void Down()
        {
            RenameTable(name: "dbo.StateChanges", newName: "StateChangeds");
        }
    }
}
