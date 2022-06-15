
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

        unsafe sealed public class Gaussian
        {
            public static readonly string Description = InitializeFunction.Gaussian.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.Gaussian.Do);
        }

        unsafe sealed public class GaussianInverted
        {
            public static readonly string Description = InitializeFunction.GaussianInverted.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.GaussianInverted.Do);
        }

        unsafe sealed public class GaussianInverted2
        {
            public static readonly string Description = InitializeFunction.GaussianInverted2.Description;

            public static readonly InputDataFunction Instance = new(&InitializeFunction.GaussianInverted2.Do);
        }
    }
}
