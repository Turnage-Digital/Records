namespace Records.Recordsets.Domain;

public sealed class MigrationRequiredException : Exception
{
    public MigrationRequiredException(string message)
        : base(message)
    {
    }
}