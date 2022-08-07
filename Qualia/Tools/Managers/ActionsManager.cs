using System;
using System.Collections.Generic;
using System.Linq;

namespace Qualia.Tools
{
    public class ActionsManager
    {
        private readonly List<ApplyAction> _actions = new();

        public ActionsManager()
        {
            //
        }

        public void Add(ApplyAction action)
        {
            _actions.Add(action);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public bool Execute(bool isForRunning)
        {
            bool result = HasActions();

            while (_actions.Any())
            {
                var actions = _actions.ToList();
                Clear();

                foreach (var action in actions)
                {
                    action.Execute(isForRunning);
                }
            }

            return result;
        }

        public bool Cancel()
        {
            return RunCancelActions();
        }

        private bool RunCancelActions()
        {
            bool result = HasActions();

            while (_actions.Any())
            {
                var actions = _actions.ToList();
                Clear();

                foreach (var action in actions)
                {
                    action.Cancel();
                }
            }

            return result;
        }

        public bool HasActions()
        {
            return _actions.Any();
        }

        public bool HasCancelActions()
        {
            return _actions.Any(a => a.CancelAction != null);
        }
    }

    public class ApplyAction
    {
        public Action RunningAction;
        public Action StandingAction;
        public Action CancelAction;

        private static readonly Action _default = delegate { };

        public ApplyAction(Action runningAction = null, Action standingAction = null, Action cancelAction = null)
        {
            RunningAction = runningAction;
            StandingAction = standingAction;
            CancelAction = cancelAction;
        }

        public bool Execute(bool isForRunning)
        {
            if (isForRunning)
            {
                (RunningAction ?? _default)();
            }
            else
            {
                (StandingAction ?? _default)();
            }

            return true;
        }

        public bool Cancel()
        {
            (CancelAction ?? _default)();
            return true;
        }
    }
}
