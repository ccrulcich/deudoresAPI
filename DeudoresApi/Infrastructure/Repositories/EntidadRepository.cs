using DeudoresApi.Domain.Models;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeudoresApi.Infrastructure.Repositories;

public class EntidadRepository(AppDbContext db) : IEntidadRepository
{
    public async Task UpsertRangeAsync(IEnumerable<Entidad> entidades)
    {
        var list = entidades.ToList();
        var keys = list.Select(e => e.CodigoEntidad).ToHashSet();

        var existingKeys = await db.Entidades
            .Where(e => keys.Contains(e.CodigoEntidad))
            .Select(e => e.CodigoEntidad)
            .ToHashSetAsync();

        var toInsert = list.Where(e => !existingKeys.Contains(e.CodigoEntidad)).ToList();
        var toUpdate = list.Where(e => existingKeys.Contains(e.CodigoEntidad)).ToList();

        if (toInsert.Count > 0)
            await db.Entidades.AddRangeAsync(toInsert);

        foreach (var e in toUpdate)
        {
            await db.Entidades
                .Where(x => x.CodigoEntidad == e.CodigoEntidad)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SumaTotalPrestamos, e.SumaTotalPrestamos));
        }

        await db.SaveChangesAsync();
    }
}
