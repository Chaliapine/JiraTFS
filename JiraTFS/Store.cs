using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using Model;
using Newtonsoft.Json;

namespace JiraTFS
{
    public class Store : IStore
    {
        private readonly Func<ModelContext> _ctxFactory;
        

        public Store(Func<ModelContext> ctxFactory)
        {
            _ctxFactory = ctxFactory;
            Config = ReadConfig();
        }

        public ConnectionData Config { get; private set; }

        public Session StartSession(SyncDirection direction)
        {
            using (var db = _ctxFactory())
            {
                var session = new Session
                {
                    StartedOn = DateTime.Now,
                    Direction = direction,
                    Result = SessionResult.InProgress
                };
                db.Sessions.Add(session);
                db.SaveChanges();
                return session;
            }
        }

        public void CloseSession(Session sessionP, SessionResult result, Exception exception = null)
        {
            using (var db = _ctxFactory())
            {
	            var session = db.Sessions.Find(sessionP.Id);
	            session.Result = result;
                session.FinishedOn = DateTime.Now;
                if (exception != null)
                    session.Errors = JsonConvert.SerializeObject(exception, Formatting.Indented);
                if (result == SessionResult.Success)
                {
                    var cfg = db.ConfigTable.First();
	                if (session.Direction == SyncDirection.Jira2TFS)
	                {
		                cfg.JiraDateFrom = session.FinishedOn;
		                Config.JiraDateFrom = session.StartedOn;
	                }
	                else
	                {
		                cfg.TFSDateFrom = session.FinishedOn;
		                Config.TFSDateFrom = session.StartedOn;
	                }
                }
                db.SaveChanges();
            }
        }

        public Bug AddBug(int workItemId, string issueKey)
        {
            using (var db = _ctxFactory())
            {
                var bug = new Bug {TFSId = workItemId.ToString(), JiraId = issueKey};
                db.Bugs.Add(bug);
                db.SaveChanges();
                return bug;
            }
        }

        public Bug GetBug(int? workItemId, string issueKey)
        {
            if(workItemId == null && issueKey == null)
                throw new ArgumentNullException();
            using (var db = _ctxFactory())
            {
                if (workItemId.HasValue)
                    return db.Bugs.FirstOrDefault(p => p.TFSId == workItemId.ToString());
                else
                    return db.Bugs.FirstOrDefault(p => p.JiraId == issueKey);
            }
        }

        public bool IsJiraLogInChanges(int historyId)
        {
            using (var db = _ctxFactory())
            {
                return db.SessionChanges.FirstOrDefault(p => p.JiraChangelog == historyId.ToString()) != null;
            }
        }

	    public bool IsJiraLogInChanges(string history)
	    {
			using (var db = _ctxFactory())
			{
				return db.SessionChanges.FirstOrDefault(p => p.JiraChangelog == history) != null;
			}
	    }

        public bool IsTfsRevisionInChanges(int workItemId, int index)
        {
            using (var db = _ctxFactory())
            {
                return db.SessionChanges.FirstOrDefault(change => change.TFSRevision == workItemId + "-" + index) != null;
            }
        }

        public IEnumerable<StateChange> ReadProgram(Tracker tracker, string fromState, string toState)
        {
            using (var db = _ctxFactory())
            {
                return db.StateChanged.Where( elem => elem.TrackerNameString == tracker.ToString() && elem.FromState == fromState && elem.ToState == toState).ToList();
                //return db.StateChangedCommands.Where(command => command.StateChangeId == stateChanged.Id).OrderBy(p => p.Order).ToList();
            }
        }

	    public IEnumerable<StateChangedCommand> ReadCommands(long id)
	    {
		    using (var db = _ctxFactory())
		    {
			    return db.StateChangedCommands.Where(command => command.StateChangeId == id).OrderBy(p => p.Order).ToList();
		    }
	    }

        public void AddSessionChange(SessionChange sessionChange)
        {
            using (var db = _ctxFactory())
            {
	            var bug = sessionChange.Bug;
	            sessionChange.Bug = null;
	            sessionChange.BugID = bug.Id;
	            var session = sessionChange.Session;
	            sessionChange.Session = null;
				sessionChange.SessionId = session.Id;
				db.SessionChanges.Add(sessionChange);
				db.SaveChanges();
            }
        }

        private ConnectionData ReadConfig()
        {
            using (var ctx = _ctxFactory())
            {
                var cfg = ctx.ConfigTable.FirstOrDefault();
                if (cfg == null) throw new ConfigurationErrorsException("Таблица ConfigTable пуста");
                return cfg;
            }
        }
    }
}