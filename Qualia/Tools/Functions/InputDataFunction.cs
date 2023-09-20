
namespace Qualia.Tools.Functions;

public unsafe class DistributionFunction : BaseFunction<DistributionFunction>
{
    public readonly delegate*<double, double> Do;

    public DistributionFunction(delegate*<double, double> doFunc)
        : base(defaultFunction: nameof(FlatRandom))
    {
        Do = doFunc;
    }

    public sealed unsafe class Constant
    {
        public static readonly string Description = InitializeFunction.Constant.Description;

        public static readonly DistributionFunction Instance = new(&InitializeFunction.Constant.Do);
    }

    public sealed unsafe class FlatRandom
    {
        public static readonly string Description = InitializeFunction.FlatRandom.Description;

        public static readonly DistributionFunction Instance = new(&InitializeFunction.FlatRandom.Do);
    }

    public sealed unsafe class GaussNormal
    {
        public static readonly string Description = InitializeFunction.GaussNormal.Description;

        public static readonly DistributionFunction Instance = new(&InitializeFunction.GaussNormal.Do);
    }

    public sealed unsafe class GaussNormalInverted
    {
        public static readonly string Description = InitializeFunction.GaussNormalInverted.Description;

        public static readonly DistributionFunction Instance = new(&InitializeFunction.GaussNormalInverted.Do);
    }
}