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
        private readonly Stopwatch _solutionTimer = new();
        private readonly Stopwatch _commonTimer = new();
        private readonly Type _taskFunctionType;

        public TaskSolutions(Type taskFunctionType)
        {
            _taskFunctionType = taskFunctionType;
        }

        public void Add(string functionName)
        {
            var methodInfo = _taskFunctionType.GetMethod(functionName, BindingFlags.Public
                                                                       | BindingFlags.NonPublic
                                                                       | BindingFlags.Static);

            _solutions.Add(new Solution(methodInfo));
        }

        public void Clear()
        {
            _solutions.Clear();
        }

        public int GetResult(object[] solutionParams)
        {
            _commonTimer.Restart();

            int rating = 0;

            var sorted = _solutions.Where(s => !s.IsExcluded)
                                   .OrderBy(s => s.AverageTime)
                                   .ThenBy(s => s.LastTime)
                                   .ThenBy(s => s.MinTime)
                                   .ToList();

            for (int i = 0; i < sorted.Count; ++i)
            {
                var solution = sorted[i];

                _solutionTimer.Restart();
                int result = (int)solution.Function.Invoke(null, solutionParams);
                _solutionTimer.Stop();

                solution.AddResult(result, _solutionTimer.Elapsed);
                solution.Rating += rating;
                ++rating;
            }

            _commonTimer.Stop();

            return sorted.Count > 1 ? GetCommonResult() : sorted[0].Result;
        }

        private int GetCommonResult()
        {
            var targetOutputs = _solutions.Select(s => s.Result).ToList();

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
                var targetOutput = solution.Result;

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
                var targetOutput = solution.Result;

                if (targetOutput != commonTargetOutput)
                {
                    solution.AddError(targetOutput, commonTargetOutput);
                }
            }

            return commonTargetOutput;
        }

        public SolutionsData GetSolutionsData(IList<Solution> solutions)
        {
            return new SolutionsData(solutions);
        }
    }

    public class Solution
    {
        public readonly MethodInfo Function;
        public string Name => Function.Name;

        public int Result { get; private set; }
        public double ExecutionMicroseconds { get; private set; }

        public long ErrorsCount => _errorsCount;

        public double MinTime { get; internal set; }
        public double LastTime => ExecutionMicroseconds;
        public double AverageTime { get; internal set; }

        public long Rating;

        public bool IsExcluded;

        private long _resultsCount;

        private int _errorsCount;

        public Solution(MethodInfo method)
        {
            Function = method;
        }

        public void AddResult(int result, TimeSpan executionTime)
        {
            Result = result;
            ExecutionMicroseconds = executionTime.TotalNanoseconds() / (double)1000;

            if (_resultsCount == 0)
            {
                MinTime = AverageTime = ExecutionMicroseconds;
            }
            else
            {
                MinTime = MathX.Min(MinTime, ExecutionMicroseconds);
                AverageTime = ((AverageTime * _resultsCount) + ExecutionMicroseconds) / (_resultsCount + 1);
            }

            ++_resultsCount;
        }

        public void AddError(int targetOutput, int commonTargetOutput)
        {
            ++_errorsCount;
        }

        public void Reset()
        {
            _resultsCount = 0;
            AverageTime = 0;
            Rating = 0;
        }
    }

    public class SolutionData
    {
        public string Name { get; private set; }
        public string MinTime { get; private set; }
        public string LastTime { get; private set; }
        public string AverageTime { get; private set; }
        public long ErrorsCount { get; private set; }
        public long Rating { get; private set; }
        public bool IsExcluded { get; private set; }

        public SolutionData(Solution solution)
        {
            Name = solution.Name;
            MinTime = Converter.DoubleToText(solution.MinTime, solution.MinTime < 10 ? "F3" : "F0");
            LastTime = Converter.DoubleToText(solution.LastTime, solution.LastTime < 10 ? "F3" : "F0");
            AverageTime = Converter.DoubleToText(solution.AverageTime, solution.AverageTime < 10 ? "F3" : "F0");
            ErrorsCount = solution.ErrorsCount;
            Rating = solution.Rating;
            IsExcluded = solution.IsExcluded;
        }
    }

    public class SolutionsData
    {
        public IList<SolutionData> Solutions => _solutionsData;

        private List<SolutionData> _solutionsData = new();

        public SolutionsData(IList<Solution> solutions)
        {
            _solutionsData.Clear();

            var minRating = solutions.Count > 0 ? solutions.Min(s => s.Rating) - 1 : 0;

            var sorted = solutions.OrderBy(s => s.Rating).ToList();

            foreach (var solution in solutions)
            {
                if (solution.IsExcluded)
                {
                    continue;
                }

                solution.Rating -= minRating;
                _solutionsData.Add(new SolutionData(solution));
                solution.Reset(); //?
            }

            if (sorted.Count > 1)
            {
                sorted[sorted.Count - 1].IsExcluded = true;
            }
        }
    }
}
