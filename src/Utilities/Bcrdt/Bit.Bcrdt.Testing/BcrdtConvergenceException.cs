namespace Bit.Bcrdt;

/// <summary>Raised when replicas that should have converged did not.</summary>
public sealed class BcrdtConvergenceException(string message) : Exception(message);
