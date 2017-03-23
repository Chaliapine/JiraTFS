using System.Configuration;
using System.Data.Entity;
using System.Threading;
using Autofac;
using Model;
using Configuration = Model.Migrations.Configuration;

namespace JiraTFS
{
    internal class Program
    {

        private static IContainer InitContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ModelContext>().AsSelf().InstancePerDependency().ExternallyOwned();
            builder.RegisterType<Store>().As<IStore>().SingleInstance();
            builder.RegisterType<Jira>().AsSelf().SingleInstance();
            builder.RegisterType<Tfs>().AsSelf().SingleInstance();
            builder.RegisterType<JiraTfs>().AsSelf().SingleInstance();
            return builder.Build();
        }

        private static void Main(string[] args)
        {
			//tfs.ExportTaskToJson(32297);
	        //tfs.TranslateItemToJira(tfs.GetWorkItemByIssueKey("easbug-879"), jira);
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ModelContext, Configuration>()); 
            var container = InitContainer();
	        var globalConfig = ConfigurationManager.GetSection("globalConfig") as GlobalConfigurationSection;
            var jiratfs = container.Resolve<JiraTfs>();
                while (true)
                {
                    jiratfs.Integrate();
                    Thread.Sleep(globalConfig.IntegrationDelay);
                }
            /*
            using (var db = new ModelContext())
            {
	           
				var jira = new Jira(db);
				var tfs = new Tfs(db);
	            //var content =@"{""update"" : { ""customfield_11000"":[{""set"" : ""test changin summary""}]}}";
					//@"{""fields"":{""summary"":""test changing summary""}}";
				//jira.UpdateIssue("EASBUG-925", content);
	            //jira.TranslateIssueToTfs(jira.GetIssue("easbug-922"), tfs);
				//db.ConfigTable.FirstOrDefault().JiraDateFrom = DateTime.Now.AddHours(-1);
				//db.ConfigTable.FirstOrDefault().TFSDateFrom = DateTime.Now.AddHours(-1);
				//db.SaveChanges();
				var jiratfs = new JiraTfs(db, jira, tfs);
				//tfs.UpdateItemTo(jira.GetIssue("easbug-879"));
				//jira.UpdateIssueBy(tfs.GetWorkItem(46653));
				while (true)
				{
					jiratfs.Integrate();
					Thread.Sleep(30000);
				}
            }
             */
        }
    }
}