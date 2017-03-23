namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedCondition : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StateChanges", "Condition", c => c.String());
            AddColumn("dbo.StateChanges", "ConditionValue", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StateChanges", "ConditionValue");
            DropColumn("dbo.StateChanges", "Condition");
        }
    }
}
