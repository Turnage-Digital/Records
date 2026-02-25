using Microsoft.EntityFrameworkCore;

namespace Records.Core.Infrastructure.Sql.QueryCriteria;

public static class QueryCriteriaEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, IQueryCriteria<T> specification)
        where T : class
    {
        var query = inputQuery;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification.OrderBy is not null)
        {
            query = specification.OrderBy(query);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = specification.OrderByDescending(query);
        }

        if (specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        return query;
    }
}