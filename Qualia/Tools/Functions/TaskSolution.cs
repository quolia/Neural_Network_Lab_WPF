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
                var yes = (int)solution.Function.Invoke(null, solutionParams) > 0;
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

        public long MinTime { get; internal set; }
        public long MaxTime { get; internal set; }
        public long AverageTime { get; internal set; }

        private long _resultsCount;

        private readonly Dictionary<Tuple<int, int>, int> _mismatchCounter = new(); // targetOutput, commonTargetOutput, count.

        public Solution(MethodInfo method)
        {
            Function = method;
        }

        public void AddResult(int targetOutput, TimeSpan executionTime)
        {
            TargetOutput = targetOutput;
            ExecutionMicroseconds = executionTime.TotalMicroseconds();

            if (_resultsCount == 0)
            {
                MinTime = MaxTime = AverageTime = ExecutionMicroseconds;
            }
            else
            {
                MinTime = MathX.Min(MinTime, ExecutionMicroseconds);
                MaxTime = MathX.Max(MaxTime, ExecutionMicroseconds);

                AverageTime = ((AverageTime * _resultsCount) + ExecutionMicroseconds) / (_resultsCount + 1);
            }

            ++_resultsCount;
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

        public long MinTime => _solution.MinTime;
        public long MaxTime => _solution.MaxTime;
        public long AverageTime => _solution.AverageTime;
        public long MismatchCount => _solution.MismatchCount;

        private readonly Solution _solution;

        public SolutionData(Solution solution)
        {
            _solution = solution;
        }
    }

    public class SolutionsData
    {
        public IList<SolutionData> Solutions => _solutionsData;

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
