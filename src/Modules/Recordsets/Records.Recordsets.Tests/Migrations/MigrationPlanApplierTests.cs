using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Tests.Migrations;

public class MigrationPlanApplierTests
{
    [Test]
    public void ApplyToRecord_ChangesColumnType()
    {
        var plan = new MigrationPlan
        {
            ChangeColumnTypes = [new ChangeColumnTypeOp("amount", ColumnType.Number, "test")]
        };

        var context = MigrationPlanApplier.Prepare(
            plan,
            [new Column { Name = "Amount", StorageKey = "amount", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        );

        var bag = new Dictionary<string, object?>
        {
            ["amount"] = "123.45"
        };

        var result = MigrationPlanApplier.ApplyToRecord(context, bag);

        Assert.That(result["amount"], Is.TypeOf<decimal>());
    }

    [Test]
    public void ApplyToRecord_MapsRemovedStatus()
    {
        var plan = new MigrationPlan
        {
            RemoveStatuses = [new RemoveStatusOp("Closed", "Done")]
        };

        var context = MigrationPlanApplier.Prepare(
            plan,
            [new Column { Name = "Name", StorageKey = "name", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }, new Status { Name = "Closed", Color = "gray" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        );

        var bag = new Dictionary<string, object?>
        {
            ["status"] = "Closed"
        };

        var result = MigrationPlanApplier.ApplyToRecord(context, bag);

        Assert.That(result["status"], Is.EqualTo("Done"));
    }
}