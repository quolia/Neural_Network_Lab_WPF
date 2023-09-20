using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Qualia.Tools.Managers;

public class ActionManager
{
    public delegate void ApplyActionDelegate(ApplyAction action);
    public static readonly ActionManager Instance = new();

    private readonly List<ApplyAction> _actions = new();
    private IEnumerable<ApplyAction> _applyActions => _actions.Where(a => a.Apply != null);
    private IEnumerable<ApplyAction> _instantActions => _actions.Where(a => a.ApplyInstant != null);
    private IEnumerable<ApplyAction> _cancelActions => _actions.Where(a => a.Cancel != null).Reverse();
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
        if (action == null || IsLocked)
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
        var result = HasApplyActions();

        while (true)
        {
            var actions = _applyActions.ToList();
            if (!actions.Any())
            {
                break;
            }

            foreach (var action in actions)
            {
                if (!_applyActions.ToList().Contains(action))
                {
                    throw new InvalidOperationException();
                }

                action.Execute(isForRunning);
                Remove(action);
            }

            actions = _applyActions.ToList();
            if (actions.Any())
            {
                // RemoveNetwork can add instant actions, which can add apply-actions.
            }
        }

        return result;
    }

    public bool ExecuteInstant(bool isRunning)
    {
        var result = HasInstantActions();

        while (true)
        {
            var actions = _instantActions.ToList();
            if (!actions.Any())
            {
                break;
            }

            foreach (var action in actions)
            {
                if (!_instantActions.ToList().Contains(action))
                {
                    throw new InvalidOperationException();
                }

                action.ExecuteInstant(isRunning);
                action.ApplyInstant = null;
            }

            actions = _instantActions.ToList();
            if (actions.Any())
            {
                throw new InvalidOperationException();
            }
        }

        return result;
    }

    public bool ExecuteCancel(bool isRunning)
    {
        var result = HasCancelActions();

        while (true)
        {
            var actions = _cancelActions.ToList();
            if (!actions.Any())
            {
                break;
            }

            List<ApplyAction> prevCancelActions = new();

            foreach (var action in actions)
            {
                if (!_cancelActions.ToList().Contains(action))
                {
                    throw new InvalidOperationException();
                }

                if (!prevCancelActions.Any(x => x.Sender == action.Sender && action.Sender is TextBox))
                {
                    action.ExecuteCancel(isRunning);
                }

                Remove(action);
                prevCancelActions.Add(action);
            }

            actions = _cancelActions.ToList();
            if (actions.Any())
            {
                throw new InvalidOperationException(); // this could be if cancel action creates another cancel action
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
    public Notification.ParameterChanged Param;
    public Action<bool> Apply;
    public Action<bool> ApplyInstant;
    public Action<bool> Cancel;

    public ApplyAction(object sender, Notification.ParameterChanged param = Notification.ParameterChanged.Unknown)
    {
        Sender = sender;
        Param = param;
    }

    public bool IsActive => Apply != null || ApplyInstant != null || Cancel != null;

    public bool Execute(bool isRunning)
    {
        Apply?.Invoke(isRunning);
        return true;
    }

    public bool ExecuteInstant(bool isRunning)
    {
        ApplyInstant?.Invoke(isRunning);
        return true;
    }

    public bool ExecuteCancel(bool isRunning)
    {
        Cancel?.Invoke(isRunning);
        return true;
    }
}