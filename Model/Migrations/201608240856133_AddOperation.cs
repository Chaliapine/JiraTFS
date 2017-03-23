namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOperation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SessionChanges", "Operation", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SessionChanges", "Operation");
        }
    }
}
