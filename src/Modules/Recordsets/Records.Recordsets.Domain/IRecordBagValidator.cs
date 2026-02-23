namespace Records.Recordsets.Domain;

public interface IRecordBagValidator
{
    void Validate(Recordset recordset, object bag);
    void ValidateTransition(Recordset recordset, object? previousBag, object nextBag);
}