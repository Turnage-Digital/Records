namespace Records.Clocks.Domain.ValueObjects;

public sealed record ClockThreshold(int Value, ClockThresholdUnit Unit)
{
    public static ClockThreshold From(int value, ClockThresholdUnit unit)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Threshold must be greater than zero.");
        }

        return new ClockThreshold(value, unit);
    }
}