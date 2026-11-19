using System.Linq.Expressions;

namespace Bit.BlazeDb;

/// <summary>A plan, plus the LINQ the plan could not absorb.</summary>
internal sealed record BlazeDbTranslatedQuery(BlazeDbQueryPlan Plan, Expression? Remainder);
