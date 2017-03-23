using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Model;
using Newtonsoft.Json;
using NLog;
using RestSharp;
using RestSharp.Extensions.MonoHttp;
using TfsAttachment = Microsoft.TeamFoundation.WorkItemTracking.Client.Attachment;

namespace JiraTFS
{
	class Jira
	{
	    private readonly IStore _store;
	    //private JiraConfigSection config;
		private GlobalConfigurationSection global;
		
		
		private string password;
		private string username;
		private string baseApiUrl;
		private string projectName;

		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Dictionary<string, string> statesToNewState; 

		
		public Jira(IStore store)
		{
		    _store = store;
			
			global = ConfigurationManager.GetSection("globalConfig") as GlobalConfigurationSection;
			if (global == null) throw new NullReferenceException("Global config section not found");
			
			baseApiUrl = _store.Config.JiraAddress;
			username = _store.Config.JiraLogin;
			password = _store.Config.JiraPassword;
			projectName = _store.Config.JiraProjectName;

		

			logger.Info("Менеджер jira создан");
		}


		/// <summary>
		/// Создает запрос к трекеру
		/// </summary>
		/// <param name="method"> метод запроса </param>
		/// <param name="path"> путь </param>
		public RestRequest CreateRequest(Method method, string path)
		{
			var request = new RestRequest { Method = method, Resource = path, RequestFormat = DataFormat.Json };
			request.AddHeader("Authorization",
				"Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", username, password))));
            logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Create, Artifact.Query, string.Format("{0} {1}", method, path)));
			return request;
		}

		/// <summary>
		/// Выполняет запрос и возвращает ответ на запрос
		/// </summary>
		/// <returns> Ответ на запрос</returns>
		public IRestResponse ExecuteRequest(RestRequest request)
		{
			var response = new RestClient(baseApiUrl).Execute(request);
            logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Success, Artifact.Query, string.Format("{0} - {1}", response.StatusCode, response.StatusDescription)));
			return response;
		}
		public Changelog GetChangelog(int id)
		{
			var path = string.Format("issue/easbug-{0}?expand=changelog", id);
			var request = CreateRequest(Method.GET, path);
			Issue response = JsonConvert.DeserializeObject<Issue>(ExecuteRequest(request).Content);
			return response.Changelog;
		}

		private string GetSimpleJqlQuery(string projectCode)
		{
			return "project='" + projectCode + "'" +
			       " AND issuetype='bug report'"; //+
			//"&status!=6" +                    //Решено
			//"&status!=10303";				 //Approved
		}

		public Issue GetIssue(int id)
		{
			var path = string.Format("issue/{0}?expand=changelog", id);
			var request = CreateRequest(Method.GET, path);
			return JsonConvert.DeserializeObject<Issue>(ExecuteRequest(request).Content);
		}

		public Issue GetIssue(string key)
		{
			try
			{
				var path = string.Format("issue/{0}?expand=changelog,renderedFields", key);
				var request = CreateRequest(Method.GET, path);
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.OK) throw new HttpListenerException((int) response.StatusCode);
				return JsonConvert.DeserializeObject<Issue>(response.Content);
			}
			catch (Exception ex)
			{
				//logger.Error(ex, "Get issue {0} not found or access denied, error message {1}", key, ex.Message);
				logger.WriteError(p => p.JMessage(Site.Jira, Operation.Readed, Artifact.Query, key).Exception(ex));
				throw ex;
			}
		}
		public List<Issue> GetIssues(string expand = "")
		{
			try
			{
				var queryCount = 50;
				var resultCount = 0;
				var issues = new List<Issue>();
				if (!string.IsNullOrEmpty(expand))
				{
					expand = "&expand=" + expand;
				}
				while (true)
				{

					var path = string.Format("search?jql={0}&startAt={1}&maxResults={2}" + expand,
						GetSimpleJqlQuery(projectName),
						resultCount,
						queryCount);

					var request = CreateRequest(Method.GET, path);

					var response = ExecuteRequest(request);
					var data = JsonConvert.DeserializeObject<IssuesContainer>(response.Content);
					var tempIssues = data.Issues ?? Enumerable.Empty<Issue>();
					issues.AddRange(tempIssues);
					resultCount += tempIssues.Count();

					if (resultCount >= data.Total) break;
				}
				return issues.ToList();
			}
			catch (Exception ex)
			{
                logger.WriteError(p => p.JMessage(Site.Jira, Operation.Reading, Artifact.Query, null).Exception(ex));
				return Enumerable.Empty<Issue>().ToList();
			}
		}

		public List<Changelog> GetChangelogs(List<Issue> issues)
		{
			return issues.Select(issue => issue.Changelog).ToList();
		}

		/// <summary>
		/// Загружает все вложения запроса key в attachmentsFolderPath
		/// </summary>
		/// <param name="key"> Ключ запроса </param>
		public List<Attachment> LoadAttachments(string key)
		{
			var issue = GetIssue(key);
			using (var webClient = new WebClient())
			{
				foreach (var attachment in issue.Fields.Attachment)
				{
					var temp = attachment.Content.Split('/');
					var newAttach = string.Join("/", temp.Take(temp.Count() - 1));
					var credetentials = string.Format("?&os_username={0}&os_password={1}&",username, password);
					var url = newAttach + '/' + credetentials + HttpUtility.UrlEncode(attachment.Filename);
					webClient.DownloadFile(url, global.AttachmentsFolder + attachment.Filename);
				}
			}
            logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Readed, Artifact.Attachment, "ALL Attachments", key));
			return issue.Fields.Attachment;
		}

		public void LoadAttachment(Issue issue, string fileName)
		{
			var attachment = issue.Fields.Attachment.FirstOrDefault(elem => elem.Filename == fileName);
			var temp = attachment.Content.Split('/');
			var newAttach = string.Join("/", temp.Take(temp.Count() - 1));
			var credetentials = string.Format("?&os_username={0}&os_password={1}&", username, password);
			var url = newAttach + '/' + credetentials + HttpUtility.UrlEncode(attachment.Filename);
			using (var webClient = new WebClient())
			{
				if (!Directory.Exists(global.AttachmentsFolder)) Directory.CreateDirectory(global.AttachmentsFolder);
				webClient.DownloadFile(url, global.AttachmentsFolder + fileName);
			}
            logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Readed, Artifact.Attachment, fileName, issue.Id));
		}

		private void InnerTranslateChangelogToTfs(Issue issue, Tfs tfs, Session session, History history)
		{
			var workItem = tfs.GetWorkItemByIssueKey(issue.Key);
			var correspondings = _store.GetBug(workItemId: workItem.Id);
			if (!workItem.IsOpen) workItem.Open();
			foreach (var item in history.Items)
			{
				switch (item.Field)
				{
					case "status":
						var fromStatus = item.FromString;
						var toStatus = item.ToString;
						var stateChanges = _store.ReadProgram(Tracker.Jira, fromStatus, toStatus);
						IEnumerable<StateChangedCommand> stateChangedCommands = Enumerable.Empty<StateChangedCommand>();
						if (stateChanges.Count() == 1)
							stateChangedCommands = _store.ReadCommands(stateChanges.FirstOrDefault().Id);
						else
						{
							foreach (var stateChange in stateChanges)
							{
								if (stateChange.Condition == Condition.WorkIsDone && stateChange.ConditionValue == tfs.SomeWorkIsDone(workItem))
								{
									stateChangedCommands = _store.ReadCommands(stateChange.Id);
									break;
								}
							}
						}

						foreach (var stateChangedCommand in stateChangedCommands)
						{
							switch (stateChangedCommand.Command)
							{
								case Command.ChangeState:
									workItem.Fields[stateChangedCommand.FieldName].Value =
										stateChangedCommand.ChangeTo;
									if (workItem.IsDirty)
									{
										logger.Info(
											"Изменение статуса элемента {0} на {1}. Изменился статус запроса {2} с {3} на {4}. Команда {5}",
											workItem.Id, stateChangedCommand.ChangeTo, issue.Key, fromStatus,
											toStatus,
											JsonConvert.SerializeObject(stateChangedCommand));
										logger.WriteInfo(
											p =>
												p.JMessage(Site.Jira, Operation.Changed, Artifact.WorkItem,
													workItem.Id.ToString(),
													workItem.Id.ToString(), issue.Key));
										workItem.Save(SaveFlags.MergeAll);
										_store.AddSessionChange(new SessionChange
										{
											Bug = correspondings,
											Session = session,
											Date = DateTime.Now,
											JiraChangelog = history.Id.ToString(),
											Operation =
												string.Format("Измение статуса на {0}",
													stateChangedCommand.ChangeTo),
											TFSRevision = workItem.Id + "-" + workItem.GetLastRevision().Index,
											SyncState = SyncState.Success
										});
									}
									break;

							}
						}

						break;
					case "Attachment":
						if (item.FromString == null)
						{
							var attachmentName = item.ToString;
							if (issue.Fields.Attachment.Any(elem => elem.Filename == attachmentName) &&
								workItem.Attachments.Cast<TfsAttachment>().All(elem => elem.Name != attachmentName))
							{
								LoadAttachment(issue, attachmentName);
								tfs.AttachFileTo(global.AttachmentsFolder + attachmentName, workItem);
							}
						}
						else
						{
							var attachmentToDelete =
								workItem.Attachments.Cast<TfsAttachment>()
									.FirstOrDefault(elem => elem.Name == item.FromString);
							if (attachmentToDelete != null)
							{
								workItem.Attachments.Remove(attachmentToDelete);
							}
						}
						break;
				}

				if (workItem.IsDirty)
				{
					workItem.Save(SaveFlags.MergeAll);
					var attachmentsFolder = new DirectoryInfo(global.AttachmentsFolder);
					attachmentsFolder.ClearFolder();
					_store.AddSessionChange(new SessionChange
					{
						Bug = correspondings,
						Session = session,
						Date = DateTime.Now,
						JiraChangelog = history.Id.ToString(),
						Operation = "Добавлены вложения",
						TFSRevision = workItem.Id + "-" + workItem.GetLastRevision().Index,
						SyncState = SyncState.Success
					});
				}

			}
		}
	    public void TraslateChangelogToTfs(Issue issue, Tfs tfs, Session session)
	    {
			var workItem = tfs.GetWorkItemByIssueKey(issue.Key);
			var correspondings = _store.GetBug(workItemId: workItem.Id);
	        foreach (var history in issue.Changelog.Histories)
	        {
				if (_store.IsJiraLogInChanges(history.Id)) continue;
		        try
		        {
			        InnerTranslateChangelogToTfs(issue, tfs, session, history);
		        }
		        catch (Exception ex)
		        {
					_store.AddSessionChange(new SessionChange
					{
						Bug = correspondings,
						Session = session,
						Date = DateTime.Now,
						JiraChangelog = history.Id.ToString(),
						Operation = ex.Message,
						SyncState = SyncState.Fail
					});
		        }
	        }
	    }

	    public void TranslateIssueToTfs(Issue issue, Tfs tfs, Session session)
		{
			issue = GetIssue(issue.Key);
			WorkItem workItem = tfs.GetWorkItemByIssueKey(issue.Key);
			Bug correspondings = GetCorrespondings(issue);

			if (workItem == null)
			{
				workItem = CreateWorkItem(tfs, issue);
				//Если появились при создании
				var attachments = LoadAttachments(issue.Key);
				foreach (var attachment in attachments)
				{
					var path = global.AttachmentsFolder + attachment.Filename;
						tfs.AttachFileTo(path, workItem);
				}


				AddChangeAtCurrentSession(workItem, issue, session, string.Format("Создан элемент {0}. Количество вложений {1}", workItem.Id, workItem.AttachedFileCount));
			}

			if (!workItem.IsOpen) workItem.Open();
			//SyncFields(workItem, issue);

			TraslateChangelogToTfs(issue, tfs, session);

			if (workItem.IsDirty)
			{
				workItem.Save(SaveFlags.MergeAll);
				AddChangeAtCurrentSession(workItem, issue, session, string.Format("Перемещены поля из Jira в TFS ({0} - {1})", issue.Key, workItem.Id));
			}
			
			
			
			//todo: Подписывать, что комментарий изменен
			var issueComments = GetComments(issue).Where(comment => !comment.body.StartsWith("TFS"));
			var tfsComments = workItem.GetHistory();
			foreach (var issueComment in issueComments)
			{
				if (!_store.IsJiraLogInChanges(issueComment.body) &&
					!tfsComments.Any(comment => issueComment.renderedBody.RemakeBadTags().EqualToComment(comment)))
				{
					issueComment.renderedBody = string.Format("Jira |[{3}] {0} пишет:{1}{2}", issueComment.Author.DisplayName,
						Environment.NewLine,
						issueComment.renderedBody, issueComment.UpdatedTime);

					workItem.Fields["History"].Value = issueComment.renderedBody;
				}
				if (workItem.IsDirty)
				{
					workItem.Save(SaveFlags.MergeAll);
					_store.AddSessionChange(
						new SessionChange
						{
							Bug = correspondings,
							Session = session,
							Date = DateTime.Now,
							Operation = string.Format("Добавлен комментарий  элементу {0}", workItem.Id),
							JiraChangelog = issueComment.body,
							TFSRevision = workItem.Id + "-" + workItem.GetLastRevision().Index,
							SyncState = SyncState.Success
						});
				}
			}
			tfs.SyncFields(workItem, this);
            logger.WriteInfo(p => p.JMessage(Site.Tfs, Operation.Sended, Artifact.WorkItem, issue.Key, issue.Key, workItem.Id.ToString()));
		}

		public WorkItem CreateWorkItem(Tfs tfs, Issue issue)
		{
			var tfsConfig = ConfigurationManager.GetSection("tfs") as TfsConfigSection;
			var workItem = new WorkItem(tfs.TeamProject.WorkItemTypes["Bug"]);
			SyncFields(workItem, issue);
			workItem.IterationPath = tfsConfig.DefaultIterationPath;
			workItem.Fields["Repro steps"].Value = "См. Исходное замечание";

			workItem.Save(SaveFlags.MergeAll);
			tfs.SyncFields(workItem, this);
		    var correspondings =_store.AddBug(workItem.Id, issue.Key);

            logger.WriteInfo(p => p.JMessage(Site.Tfs, Operation.Create, Artifact.WorkItem, workItem.Id.ToString(), workItem.Id.ToString(), issue.Key));
			return workItem;
		}

		public void SyncFields(WorkItem workItem, Issue issue)
		{
			workItem.Fields["ExternalAccountType"].Value = "bugs.caits.ru";
			workItem.Fields["ExternalAccountId"].Value = issue.Key;
			var severity = (issue.Fields.Priority.Id == "1" ? 0 : Int32.Parse(issue.Fields.Priority.Id) - 2);
			workItem.Fields["Severity"].Value = workItem.Fields["Severity"].AllowedValues[severity];
			workItem.Fields["Found In"].Value = issue.Fields.Release.Name;// + '.' + issue.Fields.Assembly;
			workItem.Title = issue.Key + ": " + issue.Fields.Summary;
			workItem.Fields["Исходное замечание"].Value = issue.RenderedFields.Description.RemakeBadTags();
			workItem.Fields["ExternalAccountState"].Value = issue.Fields.Status.Name;
		}

		
	    private void AddChangeAtCurrentSession(WorkItem workItem, Issue issue, Session session, string operation)
	    {
	        var correspondings = _store.GetBug(workItemId: workItem.Id);

	        var changelog = GetLastChangelogId(issue.Key);

	        _store.AddSessionChange(
	            new SessionChange
	            {
	                Bug = correspondings,
	                Session = session,
	                Date = DateTime.Now,
	                Operation = operation,
					JiraChangelog = changelog,
	                TFSRevision = workItem.Id + "-" + workItem.GetLastRevision().Index,
	                SyncState = SyncState.Success
	            });

	    }

	    /// <summary>
		/// Возвращает все комментарии запроса
		/// </summary>
		/// <param name="issueNum"></param>
		/// <returns></returns>
		public List<Comment> GetComments(Issue issue)
	    {
		    var request = CreateRequest(Method.GET, string.Format("issue/{0}/comment?expand=renderedBody", issue.Key));
		    var response = ExecuteRequest(request);
		    var commentsContainer = JsonConvert.DeserializeObject<CommentsContainer>(response.Content);
			return commentsContainer.Comments ?? Enumerable.Empty<Comment>().ToList();
		}

		public void SendComment(string key, string comment)
		{
			try
			{
				var path = String.Format("issue/{0}/comment", key);
				var request = CreateRequest(Method.POST, path);
				request.AddBody(new Comment { body = comment });
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.Created) throw new HttpListenerException((int)response.StatusCode);
				logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Create, Artifact.Comment, comment, key, status: string.Format("{0} {1}", response.StatusCode, response.StatusDescription)));
			}
			catch (Exception ex)
			{
                logger.WriteError(p => p.JMessage(Site.Jira, Operation.Create, Artifact.Comment, comment, key).Exception(ex));
			}
		}

		public string GetLastChangelogId(string key)
		{
			var issue = GetIssue(key);
			if (issue.Changelog.Histories.Count == 0) return null;
			return issue.Changelog.Histories.LastOrDefault().Id.ToString();
		}

		public void CreateAttachment(string key, Attachment attachment)
		{
			try
			{
				var path = String.Format("issue/{0}/attachments", key);
				var request = CreateRequest(Method.POST, path);
				request.AddHeader("X-Atlassian-Token", "nocheck");
				request.AddHeader("ContentType", "multipart/form-data");
				request.AddFile("file", global.AttachmentsFolder + attachment.Filename);
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.Created) throw new HttpListenerException((int) response.StatusCode);
                logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Create, Artifact.Attachment, attachment.Filename, key, status: string.Format("{0} {1}", response.StatusCode, response.StatusDescription)));
			}
			catch (Exception ex)
			{
                logger.WriteError(p => p.JMessage(Site.Jira, Operation.Create, Artifact.Attachment, attachment.Filename, key).Exception(ex));
			}
		}
		public List<Issue> GetUpdatedFrom(DateTime time)
		{
			var path = string.Format("search?jql={0} and updated>'{1}'", HttpUtility.UrlEncode(GetSimpleJqlQuery(projectName)), time.ToString("yyyy-MM-dd HH:mm"));
			var request = CreateRequest(Method.GET, path);
			var response = ExecuteRequest(request);
			var issues = JsonConvert.DeserializeObject<IssuesContainer>(response.Content);
			//Делаем проверку, потому секунды не учитываются
			return issues.Issues.Where(elem => elem.LastUpdate > time).ToList();
		}

		public List<Issue> GetUpdatedBtw(DateTime fromTime, DateTime toTime)
		{
			var path = string.Format("search?jql={0} and updated>='{1}'",
				HttpUtility.UrlEncode(GetSimpleJqlQuery(projectName)),
				fromTime.ToString("yyyy-MM-dd HH:mm"));
			var request = CreateRequest(Method.GET, path);
			var response = ExecuteRequest(request);
			if (response.StatusCode != HttpStatusCode.OK) throw new HttpListenerException((int)response.StatusCode);
			var issues = JsonConvert.DeserializeObject<IssuesContainer>(response.Content);
			logger.WriteInfo(
				p =>
					p.JMessage(Site.Jira, Operation.Readed, Artifact.Query, null,
						status: string.Format("{0} - {1}", response.StatusCode, response.StatusDescription)));
			
			//Делаем проверку, потому секунды не учитываются
			return issues.Issues.Where(elem => elem.LastUpdate >= fromTime && elem.LastUpdate <= toTime).ToList();
		}
		public DateTime GetServerTime()
		{
			var path = "serverInfo";
			var request = CreateRequest(Method.GET, path);
			var response = ExecuteRequest(request);
			var si = JsonConvert.DeserializeObject<ServerInfo>(response.Content);
			return Convert.ToDateTime(si.ServerTime);
		}
		public Bug GetCorrespondings(Issue issue)
		{
		    return issue == null ? null : _store.GetBug(issueKey: issue.Key);
		}

	    public void ChangeIssueStateBy(Issue issue, string transitionId)
		{
			try
			{
				var path = string.Format("issue/{0}/transitions", issue.Key);
				var request = CreateRequest(Method.POST, path);
				request.AddHeader("ContentType", "Application/json");
				var update = new Dictionary<string, object>();
				update.Add("transition", new {id = transitionId});
				request.AddBody(update);
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.NoContent) throw new HttpListenerException((int) response.StatusCode);
				logger.WriteInfo(
					p =>
						p.JMessage(Site.Jira, Operation.Changed, Artifact.Issue, "status to " + transitionId, issue.Key,
							status: string.Format("{0} {1}", response.StatusCode, response.StatusDescription)));
			}
			catch (Exception ex)
			{
				logger.WriteError(
					p =>
						p.JMessage(Site.Jira, Operation.Changed, Artifact.Issue, "status to " + transitionId, issue.Key,
							status: ex.Message).Exception(ex));
			}
		}

		public void DeleteAttachment(string key, Attachment attachment)
		{
			try
			{
				var path = String.Format("attachment/{0}", attachment.Id);
				var request = CreateRequest(Method.DELETE, path);
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.NoContent) throw new HttpListenerException((int)response.StatusCode);
                logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Removed, Artifact.Attachment, attachment.Filename, key, status: string.Format("{0} {1}", response.StatusCode, response.StatusDescription)));
                
			}
			catch (Exception ex)
			{
                logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Removed, Artifact.Attachment, attachment.Filename, key).Exception(ex));
			}
		}

		public void UpdateIssue(string key, string content)
		{
			try
			{
				//RestRequest request = new RestRequest("issue/{key}", Method.PUT);
				var request = CreateRequest(Method.PUT, string.Format("issue/{0}?expand=renderedFields", key));
				request.RequestFormat = DataFormat.Json;
				request.AddParameter("application/json", content, ParameterType.RequestBody);
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.NoContent) throw new HttpListenerException((int) response.StatusCode);
				logger.WriteInfo(
					p =>
						p.JMessage(Site.Jira, Operation.Changed, Artifact.Issue, content, key,
							status: string.Format("{0} {1}", response.StatusCode, response.StatusDescription)));
			}
			catch (Exception ex)
			{
				logger.WriteError(
					p =>
						p.JMessage(Site.Jira, Operation.Changed, Artifact.Issue, content, key,
							status: ex.Message).Exception(ex));
			}
		}

		public void AssignIssueToAuthor(Issue issue)
		{
			try
			{
				string authorName = issue.Fields.Creator.Name;
				var request = CreateRequest(Method.PUT, string.Format("issue/{0}/assignee", issue.Key));
				request.RequestFormat = DataFormat.Json;
				request.AddParameter("application/json", "{ \"name\" : \""+authorName+"\"}", ParameterType.RequestBody);
				var response = ExecuteRequest(request);
				if (response.StatusCode != HttpStatusCode.NoContent) throw new HttpListenerException((int)response.StatusCode);
				logger.WriteInfo(
					p =>
						p.JMessage(Site.Jira, Operation.Changed, Artifact.Issue, authorName, issue.Key,
							status: string.Format("{0} {1}", response.StatusCode, response.StatusDescription)));
			}
			catch (Exception ex)
			{
				logger.WriteError(
					p =>
						p.JMessage(Site.Jira, Operation.Changed, Artifact.Issue, issue.Fields.Creator.Name, issue.Key,
							status: ex.Message).Exception(ex));
			}
		}
	}
}
