namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StateChanging : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StateChangeds",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TrackerName = c.String(),
                        FromState = c.String(maxLength: 500),
                        ToState = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.StateChangedCommands",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        ChangedStateId = c.Long(),
                        CommandString = c.String(maxLength: 500),
                        Command = c.Int(nullable: false),
                        FieldName = c.String(maxLength: 500),
                        ChangeTo = c.String(maxLength: 500),
                        Order = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.StateChangeds", t => t.ChangedStateId)
                .Index(t => t.ChangedStateId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StateChangedCommands", "ChangedStateId", "dbo.StateChangeds");
            DropIndex("dbo.StateChangedCommands", new[] { "ChangedStateId" });
            DropTable("dbo.StateChangedCommands");
            DropTable("dbo.StateChangeds");
        }
    }
}
