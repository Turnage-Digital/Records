using Records.Clocks.Contracts;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class BusinessCalendarService(ClocksDbContext context) : IBusinessCalendarService
{
    private static readonly HashSet<DateTime> FederalHolidays =
    [
        new(2024, 1, 1),
        new(2024, 1, 15),
        new(2024, 2, 19),
        new(2024, 5, 27),
        new(2024, 6, 19),
        new(2024, 7, 4),
        new(2024, 9, 2),
        new(2024, 10, 14),
        new(2024, 11, 11),
        new(2024, 11, 28),
        new(2024, 12, 25),
        new(2025, 1, 1),
        new(2025, 1, 20),
        new(2025, 2, 17),
        new(2025, 5, 26),
        new(2025, 6, 19),
        new(2025, 7, 4),
        new(2025, 9, 1),
        new(2025, 10, 13),
        new(2025, 11, 11),
        new(2025, 11, 27),
        new(2025, 12, 25),
        new(2026, 1, 1),
        new(2026, 1, 19),
        new(2026, 2, 16),
        new(2026, 5, 25),
        new(2026, 6, 19),
        new(2026, 7, 3),
        new(2026, 9, 7),
        new(2026, 10, 12),
        new(2026, 11, 11),
        new(2026, 11, 26),
        new(2026, 12, 25)
    ];

    public DateTimeOffset AddBusinessTime(DateTimeOffset start, int amount, ClockThresholdUnit unit, UlidId tenantId)
    {
        if (unit != ClockThresholdUnit.BusinessDays)
        {
            return unit switch
            {
                ClockThresholdUnit.Minutes => start.AddMinutes(amount),
                ClockThresholdUnit.Hours => start.AddHours(amount),
                ClockThresholdUnit.Days => start.AddDays(amount),
                _ => start.AddDays(amount)
            };
        }

        var current = start;
        var daysAdded = 0;
        var tenantHolidays = GetTenantHolidays(tenantId);

        while (daysAdded < amount)
        {
            current = current.AddDays(1);
            if (IsBusinessDayInternal(current.Date, tenantHolidays))
            {
                daysAdded++;
            }
        }

        return current;
    }

    public bool IsBusinessDay(DateTimeOffset date, UlidId tenantId)
    {
        var tenantHolidays = GetTenantHolidays(tenantId);
        return IsBusinessDayInternal(date.Date, tenantHolidays);
    }

    private static bool IsBusinessDayInternal(DateTime date, HashSet<DateTime> tenantHolidays)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        if (FederalHolidays.Contains(date.Date))
        {
            return false;
        }

        if (tenantHolidays.Contains(date.Date))
        {
            return false;
        }

        return true;
    }

    private HashSet<DateTime> GetTenantHolidays(UlidId tenantId)
    {
        var tenantIdStr = tenantId.ToString();
        return context.Holidays
            .Where(h => h.TenantId == tenantIdStr || h.TenantId == null)
            .Select(h => h.Date)
            .ToHashSet();
    }
}