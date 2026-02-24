using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts;

public interface IBusinessCalendarService
{
    DateTimeOffset AddBusinessTime(DateTimeOffset start, int amount, ClockThresholdUnit unit, UlidId tenantId);
    bool IsBusinessDay(DateTimeOffset date, UlidId tenantId);
}