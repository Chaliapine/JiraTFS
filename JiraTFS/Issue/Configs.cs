using System;
using System.Configuration;

namespace JiraTFS
{
	public class JiraConfigSection : ConfigurationSection
	{
		[ConfigurationProperty("url")]
		public string Url
		{
			get
			{
				if (this["url"] == null)
					throw new NullReferenceException("jira | url is missing at config file");
				return (string)this["url"];
			}
		}

		[ConfigurationProperty("apiPath")]
		public string ApiPath
		{

			get { return (string)this["apiPath"] ?? "rest/api/latest/"; }
		}

		[ConfigurationProperty("login")]
		public string Login
		{
			get
			{
				if (this["login"] == null)
					throw new NullReferenceException("jira | login is missing at config file");
				return (string)this["login"];
			}
		}

		[ConfigurationProperty("password")]
		public string Password
		{
			get
			{
				if (this["password"] == null)
					throw new NullReferenceException("jira | password is missing at config file");
				return (string)this["password"];
			}
		}

		[ConfigurationProperty("projectName")]
		public string ProjectName
		{
			get
			{
				if (this["projectName"] == null)
					throw new NullReferenceException("jira | projectName is missing at config file");
				return (string)this["projectName"];
			}
		}
	}
	
	public class TfsConfigSection : ConfigurationSection
	{
		[ConfigurationProperty("defaultIterationPath")]
		public string DefaultIterationPath
		{
			get
			{
				if (this["defaultIterationPath"] == null)
					throw new NullReferenceException("tfs | defaultIterationPath is missing at config file");
				return (string)this["defaultIterationPath"];
			}
		}
		[ConfigurationProperty("url")]
		public string Url
		{
			get
			{
				if (this["url"] == null)
					throw new NullReferenceException("tfs | Url is missing at config file");
				return (string)this["url"];
			}
		}

		[ConfigurationProperty("apiPath")]
		public string ApiPath
		{

			get { return (string)this["apiPath"] ?? "_api/"; }
		}

		[ConfigurationProperty("login")]
		public string Login
		{
			get
			{
				if (this["login"] == null)
					throw new NullReferenceException("tfs | login is missing at config file");
				return (string)this["login"];
			}
		}
		[ConfigurationProperty("password")]
		public string Password
		{
			get
			{
				if (this["password"] == null)
					throw new NullReferenceException("tfs | password is missing at config file");
				return (string)this["password"];
			}
		}

		[ConfigurationProperty("projectName")]
		public string ProjectName
		{
			get
			{
				if (this["projectName"] == null)
					throw new NullReferenceException("tfs | projectName is missing at config file");
				return (string)this["projectName"];
			}
		}
		[ConfigurationProperty("workItemsQuery")]
		public string WorkItemsQuery
		{
			get
			{
				if (this["workItemsQuery"] == null)
					throw new NullReferenceException("tfs | workItemsQuery is missing at config file");
				return (string)this["workItemsQuery"];
			}
		}
	}
	public class GlobalConfigurationSection : ConfigurationSection
	{
		[ConfigurationProperty("attachmentsFolder")]
		public string AttachmentsFolder
		{
			get
			{
				if (this["attachmentsFolder"] == null)
					throw new NullReferenceException("Missing attachments folder at config file");
				return (string)this["attachmentsFolder"];
			}
		}
		[ConfigurationProperty("integrationDelay")]
		public int IntegrationDelay
		{
			get
			{
				return (int?)this["integrationDelay"] ?? 10000;
			}
		}
		[ConfigurationProperty("trackListPath")]
		public string TrackListPath
		{
			get
			{
				if (this["trackListPath"] == null)
					throw new NullReferenceException("trackListPath not found at global config");
				return (string)this["trackListPath"];
			}
		}
		[ConfigurationProperty("startKey")]
		public string StartKey
		{
			get
			{
				if (this["startKey"] == null)
					throw new NullReferenceException("startKey not found at global config");
				return (string)this["startKey"];
			}
		}
	}
}
