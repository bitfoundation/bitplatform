namespace Bit.Bmage;

/// <summary>The colour description of an image: an 'nclx' colr property, or the AV1 sequence header's.</summary>
internal readonly record struct BmageAvifNclx(int ColorPrimaries, int TransferCharacteristics, int MatrixCoefficients, bool FullRange);
