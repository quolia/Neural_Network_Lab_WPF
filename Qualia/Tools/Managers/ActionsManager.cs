using System;
using System.Collections.Generic;
using System.Linq;

namespace Qualia.Tools
{
    public class ActionsManager
    {
        public delegate void ApplyActionDelegate(Notification.ParameterChanged sender, ApplyAction action);

        public static readonly ActionsManager Instance = new();

        private readonly List<ApplyAction> _actions = new();

        private IEnumerable<ApplyAction> _applyActions => _actions.Where(a => a.RunningAction != null || a.StandingAction != null);
        private IEnumerable<ApplyAction> _cancelActions => _actions.Where(a => a.CancelAction != null).Reverse();
        private IEnumerable<ApplyAction> _instantActions => _actions.Where(a => a.InstantAction != null);

        protected ActionsManager()
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
            bool result = HasApplyActions();

            while (_actions.Any())
            {
                var actions = _applyActions.ToList();
                foreach (var action in actions)
                {
                    action.Execute(isForRunning);
                    _actions.Remove(action);
                }
            }

            return result;
        }

        public bool ExecuteInstant()
        {
            bool result = HasInstantActions();

            while (true)
            {
                var actions = _instantActions.ToList();
                if (!actions.Any())
                {
                    break;
                }

                foreach (var action in actions)
                {
                    action.ExecuteInstant();
                }
            }

            return result;
        }

        public bool Cancel()
        {
            bool result = HasCancelActions();

            while (true)
            {
                var actions = _cancelActions.ToList();
                if (!actions.Any())
                {
                    break;
                }

                foreach (var action in actions)
                {
                    action.Cancel();
                    _actions.Remove(action);
                }
            }

            return result;
        }

        public bool HasApplyActions() => _applyActions.Any();
        public bool HasCancelActions() => _cancelActions.Any();
        public bool HasInstantActions() => _instantActions.Any();
    }

    public class ApplyAction
    {
        public Action RunningAction;
        public Action StandingAction;
        public Action CancelAction;
        public Action InstantAction;

        public ApplyAction(Action runningAction = null, Action standingAction = null, Action cancelAction = null)
        {
            RunningAction = runningAction;
            StandingAction = standingAction;
            CancelAction = cancelAction;
        }

        public bool Execute(bool isRunning)
        {
            if (isRunning)
            {
                RunningAction?.Invoke();
            }
            else
            {
                StandingAction?.Invoke();
            }

            return true;
        }

        public bool ExecuteInstant()
        {
            InstantAction?.Invoke();
            InstantAction = null;
            return true;
        }

        public bool Cancel()
        {
            CancelAction?.Invoke();
            return true;
        }
    }
}
