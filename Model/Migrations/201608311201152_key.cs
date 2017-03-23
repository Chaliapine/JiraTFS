namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class key : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Bugs", "JiraId", c => c.String(maxLength: 100));
			CreateIndex("Bugs", "JiraId", true);
        }
        
        public override void Down()
        {
			DropIndex("Bugs", "JiraId");
            AlterColumn("dbo.Bugs", "JiraId", c => c.String());
        }
    }
}
