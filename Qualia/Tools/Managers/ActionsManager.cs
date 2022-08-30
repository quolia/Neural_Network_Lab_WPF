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

        public bool IsLocked = false;

        protected ActionsManager()
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

                //actions = _applyActions;
                //if (actions.Any())
                //{
                //    throw new InvalidOperationException();
                //}
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

        public bool HasActions() => _actions.Any();
        public bool HasApplyActions() => _applyActions.Any();
        public bool HasCancelActions() => _cancelActions.Any();
        public bool HasInstantActions() => _instantActions.Any();

        public void Lock()
        {
            IsLocked = true;
        }

        public void Unlock()
        {
            IsLocked = false;
        }
    }

    public class ApplyAction
    {
        public Action RunningAction;
        public Action StandingAction;
        public Action CancelAction;
        public Action InstantAction;

        public ApplyAction NextAction;

        public ApplyAction(Action runningAction = null, Action standingAction = null, Action instantAction = null, Action cancelAction = null)
        {
            RunningAction = runningAction;
            StandingAction = standingAction;
            InstantAction = instantAction;
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
            return true;
        }

        public bool Cancel()
        {
            CancelAction?.Invoke();
            return true;
        }
    }
}
