namespace Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removingAttrForOperation : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.SessionChanges", "Operation", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.SessionChanges", "Operation", c => c.String(maxLength: 500));
        }
    }
}
