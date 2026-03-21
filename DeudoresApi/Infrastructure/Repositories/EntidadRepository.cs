using DeudoresApi.Domain.Models;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeudoresApi.Infrastructure.Repositories;

public class EntidadRepository(AppDbContext db, ILogger<EntidadRepository> logger) : IEntidadRepository
{
    private const int BatchSize = 5000;

    public async Task UpsertRangeAsync(IEnumerable<Entidad> entidades, CancellationToken ct = default)
    {
        var list = entidades.ToList();

        for (var offset = 0; offset < list.Count; offset += BatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = list.Skip(offset).Take(BatchSize).ToList();
            var keys = batch.Select(e => e.CodigoEntidad).ToHashSet();

            var existingKeys = await db.Entidades
                .Where(e => keys.Contains(e.CodigoEntidad))
                .Select(e => e.CodigoEntidad)
                .ToHashSetAsync(ct);

            var toInsert = batch.Where(e => !existingKeys.Contains(e.CodigoEntidad)).ToList();
            var toUpdate = batch.Where(e => existingKeys.Contains(e.CodigoEntidad)).ToList();

            if (toInsert.Count > 0)
                await db.Entidades.AddRangeAsync(toInsert, ct);

            foreach (var e in toUpdate)
            {
                await db.Entidades
                    .Where(x => x.CodigoEntidad == e.CodigoEntidad)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.SumaTotalPrestamos, e.SumaTotalPrestamos), ct);
            }

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            if (offset % (BatchSize * 10) == 0 && offset > 0)
                logger.LogInformation("Upsert entidades progreso: {Processed}/{Total}", offset, list.Count);
        }
    }

    public async Task<Entidad?> GetByCodigoAsync(string codigoEntidad, CancellationToken ct = default)
    {
        return await db.Entidades.FindAsync([codigoEntidad], ct);
    }
}
