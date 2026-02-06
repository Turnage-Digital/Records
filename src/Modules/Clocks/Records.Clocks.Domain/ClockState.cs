namespace Records.Clocks.Domain;

public enum ClockState
{
    Running = 1,
    Paused = 2,
    AtRisk = 3,
    Breached = 4,
    Completed = 5
}