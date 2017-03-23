namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SessionRefactored : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Sessions", "Result", c => c.Int(nullable: false));
            AlterColumn("dbo.Sessions", "StartedOn", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Sessions", "Direction", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Sessions", "Direction", c => c.Int());
            AlterColumn("dbo.Sessions", "StartedOn", c => c.DateTime());
            DropColumn("dbo.Sessions", "Result");
        }
    }
}
