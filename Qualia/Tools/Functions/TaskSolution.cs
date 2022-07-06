using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Qualia.Tools
{
    public class TaskSolutions
    {
        private readonly List<Solution> _solutions = new();
        private readonly Stopwatch _solutionsTimer = new();
        private readonly Type _taskFunctionType;

        public TaskSolutions(Type taskFunctionType)
        {
            _taskFunctionType = taskFunctionType;
        }

        public void Add(string functionName)
        {
            _solutions.Add(new Solution(_taskFunctionType.GetMethod(functionName)));
        }

        public int GetTargetOutput(object[] solutionParams)
        {
            for (int i = 0; i < _solutions.Count; ++i)
            {
                var solution = _solutions[i];

                _solutionsTimer.Restart();
                var yes = (bool)solution.Function.Invoke(null, solutionParams);
                _solutionsTimer.Stop();

                solution.AddResult(yes ? 1 : 0, _solutionsTimer.Elapsed);
            }

            return GetFinalTargetOutput();
        }

        private int GetFinalTargetOutput()
        {
            var targetOutputs = _solutions.Select(s => s.TargetOutput).ToList();

            HashSet<int> output = new(targetOutputs);

            if (targetOutputs.All(t => t == targetOutputs[0])) // All the same.
            {
                return targetOutputs[0];
            }

            int maxCount = 0;
            int commonTargetOutput = -1;

            for (int i = 0; i < _solutions.Count; ++i)
            {
                var solution = _solutions[i];
                var targetOutput = solution.TargetOutput;

                var count = targetOutputs.Count(t => t == targetOutput);
                if (count >  maxCount)
                {
                    maxCount = count;
                    commonTargetOutput = targetOutput;
                }
            }

            for (int i = 0; i < _solutions.Count; ++i)
            {
                var solution = _solutions[i];
                var targetOutput = solution.TargetOutput;

                if (targetOutput != commonTargetOutput)
                {
                    solution.AddMismatch(targetOutput, commonTargetOutput);
                }
            }

            return commonTargetOutput;
        }

        public class Solution
        {
            public readonly MethodInfo Function;
            public string Name => Function.Name;

            public int TargetOutput { get; private set; }
            public long ExecutionMicroseconds { get; private set; }

            private readonly Dictionary<Tuple<int, int>, int> _mismatchCounter = new(); // targetOutput, commonTargetOutput, count.

            public Solution(MethodInfo method)
            {
                Function = method;
            }

            public void AddResult(int targetOutput, TimeSpan executionTime)
            {
                TargetOutput = targetOutput;
                ExecutionMicroseconds = executionTime.TotalMicroseconds();
            }

            internal void AddMismatch(int targetOutput, int commonTargetOutput)
            {
                ++_mismatchCounter[Tuple.Create(targetOutput, commonTargetOutput)];
            }
        }
    }

    public class SolutionsData
    {

    }
}
