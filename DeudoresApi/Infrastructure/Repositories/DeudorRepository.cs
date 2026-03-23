using DeudoresApi.Domain.Models;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeudoresApi.Infrastructure.Repositories;

public class DeudorRepository(AppDbContext db, ILogger<DeudorRepository> logger) : IDeudorRepository
{
    private const int BatchSize = 5000;

    public async Task UpsertRangeAsync(IEnumerable<Deudor> deudores, CancellationToken ct = default)
    {
        var list = deudores.ToList();

        // Procesamos en batches para evitar saturar el Change Tracker de EF Core
        // y reducir la presión de memoria con millones de registros
        for (var offset = 0; offset < list.Count; offset += BatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = list.Skip(offset).Take(BatchSize).ToList();
            var keys = batch.Select(d => d.Cuit).ToHashSet();

            var existingKeys = await db.Deudores
                .Where(d => keys.Contains(d.Cuit))
                .Select(d => d.Cuit)
                .ToHashSetAsync(ct);

            var toInsert = batch.Where(d => !existingKeys.Contains(d.Cuit)).ToList();
            var toUpdate = batch.Where(d => existingKeys.Contains(d.Cuit)).ToList();

            if (toInsert.Count > 0)
                await db.Deudores.AddRangeAsync(toInsert, ct);

            foreach (var d in toUpdate)
            {
                await db.Deudores
                    .Where(x => x.Cuit == d.Cuit)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.SituacionMaxima, d.SituacionMaxima)
                        .SetProperty(x => x.SumaTotalPrestamos, d.SumaTotalPrestamos), ct);
            }

            await db.SaveChangesAsync(ct);

            // Limpiamos el Change Tracker entre batches para liberar memoria
            db.ChangeTracker.Clear();

            if (offset % (BatchSize * 10) == 0 && offset > 0)
                logger.LogInformation("Upsert deudores progreso: {Processed}/{Total}", offset, list.Count);
        }
    }

    public async Task<Deudor?> GetByIdentificacionAsync(string cuit, CancellationToken ct = default)
    {
        return await db.Deudores.FindAsync([cuit], ct);
    }

    public async Task<IEnumerable<Deudor>> GetTopAsync(int count, int? situacion = null, CancellationToken ct = default)
    {
        IQueryable<Deudor> query = db.Deudores;

        if (situacion.HasValue)
            query = query.Where(d => d.SituacionMaxima == situacion.Value);

        return await query
            .OrderByDescending(d => d.SumaTotalPrestamos)
            .Take(count)
            .ToListAsync(ct);
    }
}
