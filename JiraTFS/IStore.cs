using System;
using System.Collections;
using System.Collections.Generic;
using Model;

namespace JiraTFS
{
    public interface IStore
    {
        ConnectionData Config { get; }
        Session StartSession(SyncDirection direction);
        void CloseSession(Session session, SessionResult result, Exception exception = null);
        Bug AddBug(int workItemId, string issueKey);
        Bug GetBug(int? workItemId = null, string issueKey = null);
        void AddSessionChange(SessionChange sessionChange);

        bool IsJiraLogInChanges(int historyId);
	    bool IsJiraLogInChanges(string history);

        bool IsTfsRevisionInChanges(int workItemId, int index);

		IEnumerable<StateChange> ReadProgram(Tracker tracker, string fromState, string toState);
		IEnumerable<StateChangedCommand> ReadCommands(long id);
    }
}