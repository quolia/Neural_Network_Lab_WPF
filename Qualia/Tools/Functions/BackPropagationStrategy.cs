using Qualia.Model;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class BackPropagationStrategy : BaseFunction<BackPropagationStrategy>
    {
        public delegate*<NetworkDataModel, void> PrepareForRun;
        public delegate*<NetworkDataModel, void> PrepareForRound;
        public delegate*<NetworkDataModel, void> PrepareForLoop;
        public delegate*<NetworkDataModel, void> OnAfterLoopFinished;
        public delegate*<NetworkDataModel, bool, void> OnError;
        public delegate*<NetworkDataModel, bool> IsBackPropagationNeeded;

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

