using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.Specifications;

namespace Records.Clocks.Infrastructure.Sql.Specifications;

public sealed class RunningClocksPastThresholdSpec : Specification<ClockDb>
{
    public RunningClocksPastThresholdSpec(DateTime asOfUtc)
    {
        AddCriteria(c =>
            c.State != (int)ClockState.Breached &&
            c.State != (int)ClockState.Completed &&
            c.AtRiskAt == null &&
            c.AtRiskDueAt <= asOfUtc);
    }
}
