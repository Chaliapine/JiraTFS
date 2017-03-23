namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Bugs",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TFSId = c.String(),
                        JiraId = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SessionChanges",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TFSRevision = c.String(),
                        JiraChangelog = c.String(),
                        Date = c.DateTime(),
                        SessionId = c.Long(),
                        BugID = c.Long(),
                        Errors = c.String(),
                        SyncState = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Bugs", t => t.BugID)
                .ForeignKey("dbo.Sessions", t => t.SessionId)
                .Index(t => t.SessionId)
                .Index(t => t.BugID);
            
            CreateTable(
                "dbo.Sessions",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        StartedOn = c.DateTime(),
                        FinishedOn = c.DateTime(),
                        Direction = c.Int(),
                        Errors = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ConfigTable",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        JiraAddress = c.String(),
                        JiraLogin = c.String(),
                        JiraPassword = c.String(),
                        JiraProjectName = c.String(),
                        JiraDateFrom = c.DateTime(),
                        TFSAddress = c.String(),
                        TFSLogin = c.String(),
                        TFSPassword = c.String(),
                        TFSProjectName = c.String(),
                        TFSDateFrom = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SessionChanges", "SessionId", "dbo.Sessions");
            DropForeignKey("dbo.SessionChanges", "BugID", "dbo.Bugs");
            DropIndex("dbo.SessionChanges", new[] { "BugID" });
            DropIndex("dbo.SessionChanges", new[] { "SessionId" });
            DropTable("dbo.ConfigTable");
            DropTable("dbo.Sessions");
            DropTable("dbo.SessionChanges");
            DropTable("dbo.Bugs");
        }
    }
}
