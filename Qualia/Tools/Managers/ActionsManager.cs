using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            foreach (var action in _actions)
            {
                action.Execute(isForRunning);
            }

            bool result = HasActions();
            _actions.Clear();

            return result;
        }

        public bool HasActions()
        {
            return _actions.Any();
        }
    }

    public class  ApplyAction
    {
        private Action _runningAction;
        private Action _standingAction;

        public ApplyAction(Action runningAction, Action standingAction)
        {
            _runningAction = runningAction;
            _standingAction = standingAction;
        }

        public bool Execute(bool isForRunning)
        {
            if (isForRunning && _runningAction != null)
            {
                _runningAction();
            }

            if (!isForRunning && _standingAction != null)
            {
                _standingAction();
            }

            return true;
        }
    }
}
