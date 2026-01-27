using Records.Recordsets.Domain.Entities;

namespace Records.Recordsets.Domain.Services;

public interface IRecordBagValidator
{
    void Validate(Recordset recordset, object bag);
    void ValidateTransition(Recordset recordset, object? previousBag, object nextBag);
}