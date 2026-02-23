namespace Records.Core.Infrastructure.Sql.QueryCriteria;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyCriteria<T>(
        this IQueryable<T> query,
        IQueryCriteria<T> criteria
    )
        where T : class
    {
        return QueryCriteriaEvaluator.GetQuery(query, criteria);
    }
}