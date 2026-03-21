using DeudoresApi.Domain.Models;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeudoresApi.Infrastructure.Repositories;

public class DeudorRepository(AppDbContext db) : IDeudorRepository
{
    public async Task UpsertRangeAsync(IEnumerable<Deudor> deudores)
    {
        var list = deudores.ToList();
        var keys = list.Select(d => d.NroIdentificacion).ToHashSet();

        var existingKeys = await db.Deudores
            .Where(d => keys.Contains(d.NroIdentificacion))
            .Select(d => d.NroIdentificacion)
            .ToHashSetAsync();

        var toInsert = list.Where(d => !existingKeys.Contains(d.NroIdentificacion)).ToList();
        var toUpdate = list.Where(d => existingKeys.Contains(d.NroIdentificacion)).ToList();

        if (toInsert.Count > 0)
            await db.Deudores.AddRangeAsync(toInsert);

        foreach (var d in toUpdate)
        {
            await db.Deudores
                .Where(x => x.NroIdentificacion == d.NroIdentificacion)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SituacionMaxima, d.SituacionMaxima)
                    .SetProperty(x => x.SumaTotalPrestamos, d.SumaTotalPrestamos));
        }

        await db.SaveChangesAsync();
    }
}
