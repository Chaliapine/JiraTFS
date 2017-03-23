using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client.Catalog.Objects;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Model;
using Newtonsoft.Json;
using NLog;
using TfsAttachment = Microsoft.TeamFoundation.WorkItemTracking.Client.Attachment;

namespace JiraTFS
{
	internal class Tfs
	{
	    private readonly IStore _store;
	    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public Project TeamProject { get; private set; }
		public TfsTeamProjectCollection Tpc { get; private set; }
		public WorkItemStore WorkItemStore { get; private set; }

		public NetworkCredential NetCred { get; private set; }
		


        private readonly GlobalConfigurationSection globalConfig;
		private HtmlConverter converter = new HtmlConverter();


		public Tfs(IStore store)
		{
		    _store = store;


			var collectionUri = new Uri(_store.Config.TFSAddress);
			NetCred = new NetworkCredential(_store.Config.TFSLogin, _store.Config.TFSPassword, "Axapta");
			Tpc = new TfsTeamProjectCollection(collectionUri, NetCred);
			Tpc.EnsureAuthenticated();
			WorkItemStore = Tpc.GetService<WorkItemStore>();
			//WorkItemStore = new WorkItemStore(Tpc, WorkItemStoreFlags.BypassRules);
			
			TeamProject = WorkItemStore.Projects[_store.Config.TFSProjectName];

			globalConfig = ConfigurationManager.GetSection("globalConfig") as GlobalConfigurationSection;

		
		}

		public WorkItem GetWorkItemByIssueKey(string issueKey)
		{
			var keyQuery = string.Format("and [ExternalAccountId] = '{0}'", issueKey);
			WorkItemCollection workItemCollection = WorkItemStore.Query(BugsQuery() + keyQuery);
			if (workItemCollection.Count == 0) return null;
			return workItemCollection[0];
		}

		public void AttachFileTo(string fileUrl, WorkItem wi)
		{
			wi.Attachments.Add(new TfsAttachment(fileUrl));
            Logger.WriteInfo(p => p.JMessage(Site.Tfs, Operation.Create, Artifact.Attachment, fileUrl, wi.Id.ToString()));
		}

		public string BugsQuery()
		{
			return "SELECT * FROM WorkItems WHERE [System.TeamProject] = 'EASOPS'" +
			       "AND [Work Item Type] = 'Bug'" +
			       "AND [ExternalAccountType] = 'bugs.caits.ru'";
		}

		public void SyncFields(WorkItem workItem, Jira jira)
		{
			var issue = new
			{
				update = new
				{
					customfield_11000 = new[] {new {set = workItem.Fields["State"].Value.ToString()}},
					customfield_11001 = new[] {new {set = workItem.Fields["Iteration path"].Value.ToString()}},
					customfield_11002 = new[] {new {set = workItem.Fields["Id"].Value.ToString()}},
					customfield_11003 = new[] {new {set = workItem.Fields["Assigned To"].Value.ToString()}}
				}
			};
			jira.UpdateIssue(workItem.Fields["ExternalAccountId"].Value.ToString(), JsonConvert.SerializeObject(issue));
		}

		public void TranslateItemToJira(WorkItem workItem, Jira jira, Session session)
		{
			var key = workItem.Fields["ExternalAccountId"].Value.ToString();
			var issue = jira.GetIssue(key);
			var correspondings = jira.GetCorrespondings(issue);
			var revisions =
				workItem.Revisions.Cast<Revision>()
					.Where(elem => !_store.IsTfsRevisionInChanges(elem.WorkItem.Id, elem.Index)).OrderBy(elem => elem.Index);
			foreach (Revision revision in revisions)
			{
				try
				{
					TranslateRevisionToJira(revision, jira, session);
				}
				catch (Exception ex)
				{
					_store.AddSessionChange(new SessionChange
					{
						Bug = correspondings,
						Session = session,
						Date = DateTime.Now,
						//JiraChangelog = jira.GetLastChangelogId(key),
						Operation = string.Format(ex.ToString()),
						TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
						SyncState = SyncState.Fail
					});
				}
			}
			
            Logger.WriteInfo(p => p.JMessage(Site.Jira, Operation.Sended, Artifact.WorkItem, workItem.Fields["ExternalAccountId"].Value.ToString(), partnerId: workItem.Id.ToString()));
		}

		public void TranslateRevisionToJira(Revision revision, Jira jira, Session session)
		{
			//bug разобраться с удалением вложения на сторонеtfs
			var key = revision.Fields["ExternalAccountId"].Value.ToString();
			var issue = jira.GetIssue(key);
			var currentComments = jira.GetComments(issue);
			var correspondings = jira.GetCorrespondings(issue);

			var previousRevision = GetRevisionPreviousTo(revision);
			IEnumerable<TfsAttachment> attachsToDelete = Enumerable.Empty<TfsAttachment>();
			IEnumerable<TfsAttachment> attachsToAdd;

			if (previousRevision != null)
			{
				attachsToDelete =
					previousRevision.Attachments
						.Cast<TfsAttachment>()
						.Where(elem => !revision.Attachments.Contains(elem));

				attachsToAdd = revision.Attachments
					.Cast<TfsAttachment>()
					.Where(elem => !previousRevision.Attachments.Contains(elem));
			}
			else
			{
				attachsToAdd = revision.Attachments.Cast<TfsAttachment>();
			}

			foreach (var attachment in attachsToDelete)
			{
				var jiraAttachment = issue.Fields.Attachment.FirstOrDefault(elem => elem.Filename == attachment.Name);
				if (jiraAttachment != null)
				{
					jira.DeleteAttachment(key, jiraAttachment);
					_store.AddSessionChange(new SessionChange
					{
						Bug = correspondings,
						Session = session,
						Date = DateTime.Now,
						JiraChangelog = jira.GetLastChangelogId(key),
						Operation = string.Format("Удалено вложение {0} элемента {1}", attachment.Name, key),
						TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
						SyncState = SyncState.Success
					});
				}
			}
			var currentAttachments = issue.Fields.Attachment;
			foreach (var attachment in attachsToAdd)
			{

				using (var client = new WebClient())
				{
					client.Credentials = NetCred;
					if (!Directory.Exists(globalConfig.AttachmentsFolder)) Directory.CreateDirectory(globalConfig.AttachmentsFolder);
					client.DownloadFile(attachment.Uri, globalConfig.AttachmentsFolder + attachment.Name);
				}
				var attachmentToSend = new Attachment
				{
					Content = attachment.Uri.AbsoluteUri,
					Filename = attachment.Name
				};

				if (currentAttachments.All(elem => elem.Filename != attachmentToSend.Filename))
				{
					jira.CreateAttachment(key, attachmentToSend);

					_store.AddSessionChange(new SessionChange
					{
						Bug = correspondings,
						Session = session,
						Date = DateTime.Now,
						JiraChangelog = jira.GetLastChangelogId(key),
						Operation = string.Format("Добавлено вложение {0} для элемента {1}", attachment.Name, key),
						TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
						SyncState = SyncState.Success
					});

					Logger.WriteInfo(
						p =>
							p.JMessage(Site.Jira, Operation.Create, Artifact.Attachment, attachmentToSend.Filename, key,
								revision.WorkItem.Id.ToString()));
				}
			}
			SyncFields(revision.WorkItem, jira);
			var text = revision.Fields["History"].Value.ToString();
			if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("Jira") &&
			    !currentComments.Any(comment => comment.renderedBody.RemakeBadTags().EqualToComment(text)))
			{
				var newComment = new Comment
				{
					Author = new JiraUser {Name = revision.Fields["Authorized As"].Value.ToString()},
					body = converter.Html2Wiki(revision.Fields["History"].Value.ToString()),
					Updated = revision.Fields["Changed Date"].Value.ToString()
				};

				text = string.Format("TFS | [{3}] {0} пишет:{1}{2}", newComment.Author.Name, Environment.NewLine,
					newComment.body,
					newComment.UpdatedTime);

				jira.SendComment(key, text);

				_store.AddSessionChange(new SessionChange
				{
					Bug = correspondings,
					Session = session,
					Date = DateTime.Now,
					JiraChangelog = text,
					Operation = string.Format("Добавлен комменатрий для элемента {0}", key),
					TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
					SyncState = SyncState.Success
				});
				Logger.WriteInfo(
					p =>
						p.JMessage(Site.Jira, Operation.Sended, Artifact.Comment, text, key,
							revision.WorkItem.Id.ToString()));
			}

			var toState = revision.Fields["State"].Value.ToString();
			
			string fromState = null;
			if (previousRevision != null)
				fromState = previousRevision.Fields["State"].Value.ToString();
			var stateChanges = _store.ReadProgram(Tracker.TFS, fromState, toState);
			IEnumerable<StateChangedCommand> stateChangedCommands = Enumerable.Empty<StateChangedCommand>();
			if (stateChanges.Count() == 1)
				stateChangedCommands = _store.ReadCommands(stateChanges.FirstOrDefault().Id);

			foreach (var stateChangedCommand in stateChangedCommands)
			{
				switch (stateChangedCommand.Command)
				{
//todo: если же статус в жире не нужно менять
					case Command.ChangeState:
						jira.ChangeIssueStateBy(issue, stateChangedCommand.ChangeTo);
						_store.AddSessionChange(new SessionChange
						{
							Bug = correspondings,
							Session = session,
							Date = DateTime.Now,
							JiraChangelog = jira.GetLastChangelogId(issue.Key),
							Operation =
								string.Format("Измение статуса элемента {1} на {0}", stateChangedCommand.ChangeTo, key),
							TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
							SyncState = SyncState.Success
						});
						break;
					case Command.AssignToAuthor:
						jira.AssignIssueToAuthor(issue);
						_store.AddSessionChange(new SessionChange
						{
							Bug = correspondings,
							Session = session,
							Date = DateTime.Now,
							JiraChangelog = jira.GetLastChangelogId(issue.Key),
							Operation =
								string.Format("Изменение исполнителя на автора у {0}", key),
							TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
							SyncState = SyncState.Success
						});
						break;
					case Command.SendFieldAsComment:
						if (!string.IsNullOrWhiteSpace(revision.Fields[stateChangedCommand.FieldName].Value.ToString()))
						{
							var field = revision.Fields[stateChangedCommand.FieldName];
							jira.SendComment(key, string.Format("TFS | {0}:\n{1}", field.Name, field.Value));
							_store.AddSessionChange(new SessionChange
							{
								Bug = correspondings,
								Session = session,
								Date = DateTime.Now,
								JiraChangelog = jira.GetLastChangelogId(issue.Key),
								Operation =
									string.Format("Перенос поля {0} ({1})в качестве комментария ", field.Name, converter.Html2Wiki(field.Value.ToString())),
								TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
								SyncState = SyncState.Success
							});
						}
						break;
				}
			}
			if (!_store.IsTfsRevisionInChanges(revision.WorkItem.Id, revision.Index))
			{
				_store.AddSessionChange(new SessionChange
				{
					Bug = correspondings,
					Session = session,
					Date = DateTime.Now,
					JiraChangelog = jira.GetLastChangelogId(issue.Key),
					Operation ="Отсутствуют значимые изменения",
					TFSRevision = revision.WorkItem.Id + "-" + revision.Index,
					SyncState = SyncState.Success
				});
			}

			var attachmentsFolder = new DirectoryInfo(globalConfig.AttachmentsFolder);
			attachmentsFolder.ClearFolder();
		}

		private List<WorkItem> GetWorkItems()
		{
			
			var workItemCollection = WorkItemStore.Query(BugsQuery());
            Logger.WriteInfo(p => p.JMessage(Site.Tfs, Operation.Reading, Artifact.WorkItem, BugsQuery()));
			return workItemCollection.Cast<WorkItem>().ToList();

		}

		public List<WorkItem> GetUpdatedFrom(DateTime time)
		{
			var wiql = BugsQuery() +
			           "and [Changed Date] >= '" + time.Date + "'";
			var query = new Query(WorkItemStore, wiql, null, false);
			var workItemCollection =
				query.RunQuery().Cast<WorkItem>().Where(elem => Convert.ToDateTime(elem.Fields["Changed Date"].Value) > time);
			return workItemCollection.ToList();
		}

		public List<WorkItem> GetUpdatedBtw(DateTime fromTime, DateTime toTime)
		{
			var wiql = BugsQuery() +
					   "and [Changed Date] >= '" + fromTime.ToString("yyyy-MM-dd") + "'";// +
					   //"and [Changed Date] <= '" + toTime.ToString("yyyy-MM-dd") + "'";

			var query = new Query(WorkItemStore, wiql, null, false);
			var workItemCollection =
				query.RunQuery()
					.Cast<WorkItem>()
					.Where(
						elem =>
							Convert.ToDateTime(elem.Fields["Changed Date"].Value) >= fromTime &&
							Convert.ToDateTime(elem.Fields["Changed Date"].Value) <= toTime &&
                            _store.GetBug(workItemId: elem.Id) != null);
			return workItemCollection.ToList();
		}

		public DateTime GetServerTime()
		{
			return WorkItemStore.TimeZone.ToLocalTime(DateTime.UtcNow);
		}

		public Revision GetRevisionPreviousTo(Revision revision)
		{
			return revision.Index == 0 ? null : revision.WorkItem.Revisions[revision.Index - 1];
		}

		public bool SomeWorkIsDone(WorkItem workItem)
		{
			foreach (RelatedLink link in workItem.Links)
			{
				var oppositeItem = WorkItemStore.GetWorkItem(link.RelatedWorkItemId);
				if (link.LinkTypeEnd.Name == "Child" && oppositeItem.Type.Name == "Task" && oppositeItem.State == "Завершён")
				{
					return true;
				}
			}
			return false;
		}

	}
}
