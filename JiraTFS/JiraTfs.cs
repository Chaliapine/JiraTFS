using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Model;
using NLog;

namespace JiraTFS
{
	internal class JiraTfs
	{
		private readonly IStore _store;
		private readonly Jira _jira;
		private readonly Tfs _tfs;



		private readonly TimeSpan _timeDifferenceBtwJira;
		private readonly TimeSpan _timeDifferenceBtfTfs;

		private Dictionary<string, string> fromToStates;



		private GlobalConfigurationSection global;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public JiraTfs(IStore store, Jira jira, Tfs tfs)
		{
			_store = store;
			_jira = jira;
			_tfs = tfs;

			_timeDifferenceBtwJira = jira.GetServerTime() - DateTime.Now;
			_timeDifferenceBtfTfs = tfs.GetServerTime() - DateTime.Now;

			global = ConfigurationManager.GetSection("globalConfig") as GlobalConfigurationSection;

		}

		public void Integrate()
		{
			try
			{
				InternalIntegrate();
			}

			catch (Exception ex)
			{
				Logger.Error(ex, "Ошибка во время синхронизации: {0}", ex);
			}
		}

		public void InternalIntegrate()
		{
			 Logger.Info("Начало синхронизации из Jira в  TFS");
			var lastSyncTime = _store.Config.JiraDateFrom.Value.AddMinutes(-1);
			var jiraSession = _store.StartSession(SyncDirection.Jira2TFS);
			try
			{
				var updatedIssues = _jira
					.GetUpdatedBtw(lastSyncTime + _timeDifferenceBtwJira, jiraSession.StartedOn)
					.Where(elem => elem.Key.KeyGreaterThan(global.StartKey));
				Logger.Info("С {0} по {1} обновлено {2} запросов", lastSyncTime, jiraSession.StartedOn,
					updatedIssues.Count());
				foreach (var updatedIssue in updatedIssues)
				{
					try
					{
						_jira.TranslateIssueToTfs(updatedIssue, _tfs, jiraSession);
					}
					catch (Exception issueException)
					{
						Logger.Error(issueException, "Ошибка синхронизации элемента - " + updatedIssue.Key);
					}
					
				}
				_store.CloseSession(jiraSession, SessionResult.Success);
				Logger.Info("Окончание синхронизации из Jira в  TFS");
			}
			catch (Exception ex)
			{
				_store.CloseSession(jiraSession, SessionResult.Failure, ex);
				Logger.Error(ex, "Ошибка синхронизации из Jira в  TFS");
			}


			lastSyncTime = _store.Config.TFSDateFrom.Value.AddMinutes(-1);
			var tfsSession = _store.StartSession(SyncDirection.TFS2Jira);
			Logger.Info("Начало синхронизации из TFS в Jira");
			try
			{
				var updatedWorkItems = _tfs
					.GetUpdatedBtw(lastSyncTime + _timeDifferenceBtfTfs, tfsSession.StartedOn + _timeDifferenceBtfTfs)
					.Where(elem => elem.Fields["ExternalAccountId"].Value.ToString().KeyGreaterThan(global.StartKey));
				Logger.Info("С {0} по {1} обновлено {2} запросов", lastSyncTime, tfsSession.StartedOn,
					updatedWorkItems.Count());
				foreach (var updatedWorkItem in updatedWorkItems)
				{
					try
					{
						_tfs.TranslateItemToJira(updatedWorkItem, _jira, tfsSession);
					}
					catch (Exception workitemException)
					{
						Logger.Error(workitemException, "Ошибка синхронизации элемента - " + updatedWorkItem.Id );
					}
				}
				_store.CloseSession(tfsSession, SessionResult.Success);
				Logger.Info("Окончание синхронизации из TFS в Jira");
			}
			catch (Exception ex)
			{
				_store.CloseSession(tfsSession, SessionResult.Failure, ex);
				Logger.Error(ex, "Ошибка синхронизации из TFS в Jira");
			}

		}
	}
}
