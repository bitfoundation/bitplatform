using System.Linq.Expressions;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>The predicates index selection could not serve, plus whether it also answered the ordering.</summary>
internal sealed record BlazeDbIndexSelection(IReadOnlyList<LambdaExpression> Residual, bool OrderingSatisfied);
