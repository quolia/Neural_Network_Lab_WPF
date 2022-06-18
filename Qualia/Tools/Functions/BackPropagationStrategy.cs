using Qualia.Model;
using System;
using System.Runtime.CompilerServices;
using static Qualia.Tools.BackPropagationStrategy;

namespace Qualia.Tools
{
    unsafe public class BackPropagationStrategy : BaseFunction<BackPropagationStrategy>
    {
        //public override string DefaultFunction => nameof(Always);


        public readonly delegate*<NetworkDataModel, void> PrepareForRun;
        public readonly delegate*<NetworkDataModel, void> PrepareForRound;
        public readonly delegate*<NetworkDataModel, void> PrepareForLoop;
        public readonly delegate*<NetworkDataModel, void> OnAfterLoopFinished;
        public readonly delegate*<NetworkDataModel, bool, void> OnError;
        public readonly delegate*<NetworkDataModel, bool> IsBackPropagationNeeded;

        protected bool _isError;
        protected bool _isBackPropagationNeeded;

        public BackPropagationStrategy(delegate*<NetworkDataModel, void> prepareForRun,
                                       delegate*<NetworkDataModel, void> prepareForRound,
                                       delegate*<NetworkDataModel, void> prepareForLoop,
                                       delegate*<NetworkDataModel, void> onAfterLoopFinished,
                                       delegate*<NetworkDataModel, bool, void> onError,
                                       delegate*<NetworkDataModel, bool> isBackPropagationNeeded)
            : base(nameof(Always))
        {
            PrepareForRun = prepareForRun;
            PrepareForRound = prepareForRound;
            PrepareForLoop = prepareForLoop;
            OnAfterLoopFinished = onAfterLoopFinished;
            OnError = onError;
            IsBackPropagationNeeded = isBackPropagationNeeded;
        }

        public static void Stub1(NetworkDataModel network) {}
        public static void Stub2(NetworkDataModel network, bool isError) {}

        unsafe sealed public class Always
        {
            public static readonly string Description = "BP executes every round.";

            public static readonly BackPropagationStrategy Instance = new(&Stub1,
                                                                          &Stub1,
                                                                          &Stub1,
                                                                          &Stub1,
                                                                          &Stub2,
                                                                          &IsBackPropagationNeeded);

            public static bool IsBackPropagationNeeded(NetworkDataModel networkModel)
            {
                return true;
            }
        }

        unsafe sealed public class Never
        {
            public static readonly string Description = "BP never executes.";

            public static readonly BackPropagationStrategy Instance = new(&Stub1,
                                                                          &Stub1,
                                                                          &Stub1,
                                                                          &Stub1,
                                                                          &Stub2,
                                                                          &IsBackPropagationNeeded);

            public static bool IsBackPropagationNeeded(NetworkDataModel networkModel)
            {
                return false;
            }
        }

        unsafe sealed public class InErrorRound
        {
            public static readonly string Description = "BP executes only in error round.";

            public static readonly BackPropagationStrategy Instance = new(&Stub1,
                                                                          &PrepareForRound,
                                                                          &Stub1,
                                                                          &Stub1,
                                                                          &OnError,
                                                                          &IsBackPropagationNeeded);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void PrepareForRound(NetworkDataModel network)
            {
                Instance._isBackPropagationNeeded = false;
            }

            public static void OnError(NetworkDataModel networkModel, bool isError)
            {
                Instance._isBackPropagationNeeded = isError;
            }

            public static bool IsBackPropagationNeeded(NetworkDataModel networkModel)
            {
                return Instance._isBackPropagationNeeded;
            }
        }

        unsafe sealed public class InCorrectRound
        {
            public static readonly string Description = "BP executes only on correct round.";

            public static readonly BackPropagationStrategy Instance = new(&Stub1,
                                                                          &PrepareForRound,
                                                                          &Stub1,
                                                                          &Stub1,
                                                                          &OnError,
                                                                          &IsBackPropagationNeeded);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void PrepareForRound(NetworkDataModel network)
            {
                Instance._isBackPropagationNeeded = false;
            }

            public static void OnError(NetworkDataModel networkModel, bool isError)
            {
                Instance._isBackPropagationNeeded = !isError;
            }

            public static bool IsBackPropagationNeeded(NetworkDataModel networkModel)
            {
                return Instance._isBackPropagationNeeded;
            }
        }

        unsafe sealed public class UntilNoErrorInLoop
        {
            public static readonly string Description = "BP executes in every round until all rounds in a loop are correct. BP does not execute until the next error round.";

            public static readonly BackPropagationStrategy Instance = new(&PrepareForRun,
                                                                          &Stub1,
                                                                          &PrepareForLoop,
                                                                          &OnAfterLoopFinished,
                                                                          &OnError,
                                                                          &IsBackPropagationNeeded);

            public static void PrepareForRun(NetworkDataModel model)
            {
                Instance._isBackPropagationNeeded = true;
                Instance._isError = false;
            }

            public static void PrepareForLoop(NetworkDataModel model)
            {
                Instance._isError = false;
            }

            public static void OnError(NetworkDataModel networkModel, bool isError)
            {
                Instance._isError = Instance._isError || isError;
            }

            public static bool IsBackPropagationNeeded(NetworkDataModel network)
            {
                return Instance._isBackPropagationNeeded;
            }

            public static void OnAfterLoopFinished(NetworkDataModel network)
            {
                Instance._isBackPropagationNeeded = Instance._isError;
            }
        }
    }
}

