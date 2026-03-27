namespace Records.Clocks.Infrastructure.Sql.Entities;

public sealed class HolidayDb
{
    public int Id { get; set; }
    public string? TenantId { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = null!;
    public bool IsObserved { get; set; }
}