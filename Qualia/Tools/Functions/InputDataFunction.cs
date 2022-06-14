
namespace Qualia.Tools
{
    unsafe public class InputDataFunction : BaseFunction<InputDataFunction>
    {
        public delegate*<double?, double> Do;

        public InputDataFunction(delegate*<double?, double> doFunc)
            : base(nameof(FlatRandom))
        {
            Do = doFunc;
        }

        unsafe sealed public class Constant
        {
            public static readonly InputDataFunction Instance = new(&InitializeFunction.Centered.Do);
        }

        unsafe sealed public class FlatRandom
        {
            public static readonly InputDataFunction Instance = new(&InitializeFunction.FlatRandom.Do);
        }

        unsafe sealed public class Gaussian
        {
            public static readonly InputDataFunction Instance = new(&InitializeFunction.Gaussian.Do);
        }

        unsafe sealed public class GaussianInverted
        {
            public static readonly InputDataFunction Instance = new(&InitializeFunction.GaussianInverted.Do);
        }

        unsafe sealed public class GaussianInverted2
        {
            public static readonly InputDataFunction Instance = new(&InitializeFunction.GaussianInverted2.Do);
        }
    }
}
