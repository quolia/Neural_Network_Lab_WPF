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
            foreach (var solution in _solutions.OrderBy(s => s.AverageTime))
            {
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
                    solution.AddError(targetOutput, commonTargetOutput);
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
        public double ExecutionMicroseconds { get; private set; }

        public long ErrorsCount => _errorsCount;

        public double MinTime { get; internal set; }
        public double MaxTime { get; internal set; }
        public double AverageTime { get; internal set; }

        private long _resultsCount;

        private int _errorsCount;

        public Solution(MethodInfo method)
        {
            Function = method;
        }

        public void AddResult(int targetOutput, TimeSpan executionTime)
        {
            TargetOutput = targetOutput;
            ExecutionMicroseconds = executionTime.TotalNanoseconds() / (double)1000;

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

        internal void AddError(int targetOutput, int commonTargetOutput)
        {
            ++_errorsCount;
        }
    }

    public class SolutionData
    {
        public string Name => _solution.Name;

        public double MinTime => _solution.MinTime;
        public double MaxTime => _solution.MaxTime;
        public double AverageTime => _solution.AverageTime;
        public long ErrorsCount => _solution.ErrorsCount;

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
