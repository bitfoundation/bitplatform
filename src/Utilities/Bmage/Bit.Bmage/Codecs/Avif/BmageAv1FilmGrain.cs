namespace Bit.Bmage;

/// <summary>Film grain parameters (spec 5.9.30).</summary>
internal sealed class BmageAv1FilmGrain
{
    public bool ApplyGrain;
    public int GrainSeed;
    public int NumYPoints;
    public readonly int[] PointYValue = new int[16];
    public readonly int[] PointYScaling = new int[16];
    public bool ChromaScalingFromLuma;
    public int NumCbPoints;
    public readonly int[] PointCbValue = new int[16];
    public readonly int[] PointCbScaling = new int[16];
    public int NumCrPoints;
    public readonly int[] PointCrValue = new int[16];
    public readonly int[] PointCrScaling = new int[16];
    public int GrainScalingMinus8;
    public int ArCoeffLag;
    public readonly int[] ArCoeffsYPlus128 = new int[24];
    public readonly int[] ArCoeffsCbPlus128 = new int[25];
    public readonly int[] ArCoeffsCrPlus128 = new int[25];
    public int ArCoeffShiftMinus6;
    public int GrainScaleShift;
    public int CbMult, CbLumaMult, CbOffset;
    public int CrMult, CrLumaMult, CrOffset;
    public bool OverlapFlag;
    public bool ClipToRestrictedRange;
}
