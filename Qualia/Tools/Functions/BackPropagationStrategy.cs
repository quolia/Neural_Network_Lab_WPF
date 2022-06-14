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
            : base(nameof(EveryRound))
        {
            PrepareForRun = prepareForRun;
            PrepareForRound = prepareForRound;
            PrepareForLoop = prepareForLoop;
            OnAfterLoopFinished = onAfterLoopFinished;
            OnError = onError;
            IsBackPropagationNeeded = isBackPropagationNeeded;
        }

        public static void Stub(NetworkDataModel network)
        {

        }


        unsafe sealed public class EveryRound
        {
            public static readonly BackPropagationStrategy Instance = new(&PrepareForRun,
                                                                          &PrepareForRound,
                                                                          &PrepareForLoop,
                                                                          &OnAfterLoopFinished,
                                                                          &OnError,
                                                                          &IsBackPropagationNeeded);



            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void PrepareForRun(NetworkDataModel network)
            {
                
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void PrepareForRound(NetworkDataModel network)
            {

            }

            public static void PrepareForLoop(NetworkDataModel model)
            {
                
            }

            public static void OnAfterLoopFinished(NetworkDataModel networkModel)
            {
                
            }

            public static void OnError(NetworkDataModel networkModel, bool v)
            {
                
            }

            public static bool IsBackPropagationNeeded(NetworkDataModel networkModel)
            {
                return true;
            }
        }

        unsafe sealed public class IfErrorInLoop
        {
            public static readonly BackPropagationStrategy Instance = new(&PrepareForRun,
                                                                          &PrepareForRound,
                                                                          &PrepareForLoop,
                                                                          &OnAfterLoopFinished,
                                                                          &OnError,
                                                                          &IsBackPropagationNeeded);



            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void PrepareForRun(NetworkDataModel network)
            {
                Instance._isBackPropagationNeeded = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void PrepareForRound(NetworkDataModel network)
            {

            }

            public static void PrepareForLoop(NetworkDataModel model)
            {
                Instance._isError = false;
            }

            public static void OnAfterLoopFinished(NetworkDataModel networkModel)
            {
                Instance._isBackPropagationNeeded = Instance._isError;
            }

            public static void OnError(NetworkDataModel networkModel, bool v)
            {
                Instance._isError = true;
            }

            public static bool IsBackPropagationNeeded(NetworkDataModel networkModel)
            {
                return Instance._isBackPropagationNeeded;
            }
        }
    }
}

