namespace Records.Recordsets.Domain.Exceptions;

public sealed class MigrationRequiredException : Exception
{
    public MigrationRequiredException(string message)
        : base(message)
    {
    }
}