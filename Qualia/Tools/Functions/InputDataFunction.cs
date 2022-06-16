
namespace Qualia.Tools
{
    unsafe public class InputDataFunction : BaseFunction<InputDataFunction>
    {
        public readonly delegate*<double?, double> Do;

        public InputDataFunction(delegate*<double?, double> doFunc)
            : base(defaultValue: nameof(FlatRandom))
        {
            Do = doFunc;
        }

        unsafe sealed public class Constant
        {
            public static readonly string Description = InitializeFunction.Constant.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.Constant.Do);
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly string Description = InitializeFunction.FlatRandom.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.FlatRandom.Do);
        }

        unsafe sealed public class GaussNormal
        {
            public static readonly string Description = InitializeFunction.GaussNormal.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.GaussNormal.Do);
        }

        unsafe sealed public class GaussNormalInverted
        {
            public static readonly string Description = InitializeFunction.GaussNormalInverted.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.GaussNormalInverted.Do);
        }
    }
}
