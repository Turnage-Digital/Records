using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClockDefinitionProjectionWriter(
    ClocksDbContext dbContext,
    ILogger<ClockDefinitionProjectionWriter> logger
) : IClockDefinitionProjectionWriter
{
    public async Task UpsertAsync(ClockDefinitionProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.ClockDefinitionProjections
            .FirstOrDefaultAsync(x => x.Id == model.DefinitionId.ToString(), cancellationToken);

        if (record is null)
        {
            record = new ClockDefinitionProjectionDb
            {
                Id = model.DefinitionId.ToString(),
                TenantId = model.TenantId.ToString(),
                Name = model.Name,
                AtRiskThresholdValue = model.AtRiskThresholdValue,
                AtRiskThresholdUnit = (int)model.AtRiskThresholdUnit,
                BreachThresholdValue = model.BreachThresholdValue,
                BreachThresholdUnit = (int)model.BreachThresholdUnit,
                IsActive = model.IsActive
            };

            dbContext.ClockDefinitionProjections.Add(record);
        }
        else
        {
            record.Name = model.Name;
            record.AtRiskThresholdValue = model.AtRiskThresholdValue;
            record.AtRiskThresholdUnit = (int)model.AtRiskThresholdUnit;
            record.BreachThresholdValue = model.BreachThresholdValue;
            record.BreachThresholdUnit = (int)model.BreachThresholdUnit;
            record.IsActive = model.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("ClockDefinitionProjection upserted: {DefinitionId}", model.DefinitionId);
    }

    public async Task DisableAsync(UlidId definitionId, CancellationToken cancellationToken)
    {
        var record = await dbContext.ClockDefinitionProjections
            .FirstOrDefaultAsync(x => x.Id == definitionId.ToString(), cancellationToken);

        if (record is null)
        {
            return;
        }

        record.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("ClockDefinitionProjection disabled: {DefinitionId}", definitionId);
    }
}