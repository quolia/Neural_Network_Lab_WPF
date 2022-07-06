using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Qualia.Tools
{
    public class TaskSolutions
    {
        public IList<Solution> Solutions => _solutions;

        private readonly List<Solution> _solutions = new();
        private readonly Stopwatch _solutionsTimer = new();
        private readonly Type _taskFunctionType;

        public TaskSolutions(Type taskFunctionType)
        {
            _taskFunctionType = taskFunctionType;
        }

        public void Add(string functionName)
        {
            var methodInfo = _taskFunctionType.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            _solutions.Add(new Solution(methodInfo));
        }

        public void Clear()
        {
            _solutions.Clear();
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

        internal SolutionsData GetSolutionsData(IList<Solution> solutions)
        {
            return new SolutionsData(solutions);
        }
    }

    public class Solution
    {
        public readonly MethodInfo Function;
        public string Name => Function.Name;

        public int TargetOutput { get; private set; }
        public long ExecutionMicroseconds { get; private set; }

        public long MismatchCount => _mismatchCounter.Values.Sum();

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
            var key = Tuple.Create(targetOutput, commonTargetOutput);
            if (!_mismatchCounter.ContainsKey(key))
            {
                _mismatchCounter.Add(key, 0);
            }
            ++_mismatchCounter[key];
        }
    }

    public class SolutionData
    {
        public string Name => _solution.Name;

        private readonly Solution _solution;

        public SolutionData(Solution solution)
        {
            _solution = solution;
        }
    }

    public class SolutionsData
    {
        private List<SolutionData> _solutionsData = new();

        public SolutionsData(IList<Solution> solutions)
        {
            _solutionsData.Clear();

            foreach (var solution in solutions)
            {
                _solutionsData.Add(new SolutionData(solution));
            }
        }
    }
}
