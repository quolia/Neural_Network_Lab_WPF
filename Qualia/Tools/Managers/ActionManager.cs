using System;
using System.Collections.Generic;
using System.Linq;

namespace Qualia.Tools
{
    public class ActionManager
    {
        public delegate void ApplyActionDelegate(Notification.ParameterChanged sender, ApplyAction action);
        public static readonly ActionManager Instance = new();

        private readonly List<ApplyAction> _actions = new();
        private IEnumerable<ApplyAction> _applyActions => _actions.Where(a => a.RunningAction != null || a.StandingAction != null);
        private IEnumerable<ApplyAction> _cancelActions => _actions.Where(a => a.CancelAction != null).Reverse();
        private IEnumerable<ApplyAction> _instantActions => _actions.Where(a => a.InstantAction != null);
        private int _lockCount;
        public bool IsLocked => _lockCount != 0;
        
        public object Invalidator;

        public bool IsValid => Invalidator == null;

        protected ActionManager()
        {
            //
        }

        public void Add(ApplyAction action)
        {
            if (IsLocked)
            {
                return;
            }

            _actions.Add(action);
        }

        public void AddMany(List<ApplyAction> actions)
        {
            if (IsLocked)
            {
                return;
            }

            _actions.AddRange(actions);
        }

        public void Remove(ApplyAction action)
        {
            _actions.Remove(action);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public bool Execute(bool isForRunning)
        {
            bool result = HasApplyActions();

            while (true)
            {
                var actions = _applyActions.ToList();
                if (!actions.Any())
                {
                    break;
                }

                foreach (var action in actions)
                {
                    action.Execute(isForRunning);
                    Remove(action);
                }

                actions = _applyActions.ToList();
                if (actions.Any())
                { 
                    //throw new InvalidOperationException();
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
                    action.InstantAction = null;
                }

                actions = _instantActions.ToList();
                if (actions.Any())
                {
                    throw new InvalidOperationException();
                }
            }

            return result;
        }

        public bool ExecuteCancel()
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
                    Remove(action);
                }

                actions = _cancelActions.ToList();
                if (actions.Any())
                {
                    throw new InvalidOperationException();
                }
            }

            return result;
        }

        public bool HasActions() => HasApplyActions() || HasInstantActions() || HasCancelActions();
        public bool HasApplyActions() => _applyActions.Any();
        public bool HasCancelActions() => _cancelActions.Any();
        public bool HasInstantActions() => _instantActions.Any();

        public void Lock()
        {
            ++_lockCount;
        }

        public void Unlock()
        {
            --_lockCount;
        }
    }

    public class ApplyAction
    {
        public object Sender;
        public Action RunningAction;
        public Action StandingAction;
        public Action CancelAction;
        public Action InstantAction;

        public ApplyAction(object sender)
        {
            Sender = sender;
        }

        public bool IsActive => RunningAction != null || StandingAction != null || CancelAction != null || InstantAction != null;

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
            return true;
        }

        public bool Cancel()
        {
            CancelAction?.Invoke();
            return true;
        }
    }
}
