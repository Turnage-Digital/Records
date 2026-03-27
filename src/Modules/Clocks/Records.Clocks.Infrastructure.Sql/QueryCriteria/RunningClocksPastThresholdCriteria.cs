using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Infrastructure.Sql.QueryCriteria;

namespace Records.Clocks.Infrastructure.Sql.QueryCriteria;

public sealed class RunningClocksPastThresholdCriteria : QueryCriteria<ClockDb>
{
    public RunningClocksPastThresholdCriteria(DateTime asOfUtc)
    {
        AddCriteria(c =>
            c.State != (int)ClockState.Breached &&
            c.State != (int)ClockState.Completed &&
            c.AtRiskAt == null &&
            c.AtRiskDueAt <= asOfUtc);
    }
}