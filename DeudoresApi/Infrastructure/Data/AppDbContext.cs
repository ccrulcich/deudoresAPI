using DeudoresApi.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DeudoresApi.Infrastructure.Data;

/// <summary>
/// Contexto de EF Core. Vive en Infrastructure — es la única capa que conoce el ORM.
/// Los modelos de dominio se configuran aquí via Fluent API para mantenerlos limpios.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Deudor> Deudores => Set<Deudor>();
    public DbSet<Entidad> Entidades => Set<Entidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Deudor>(entity =>
        {
            entity.HasKey(d => d.Cuit);
            entity.Property(d => d.Cuit).HasMaxLength(11);
            entity.Property(d => d.SumaTotalPrestamos).HasColumnType("numeric(18,2)");

            entity.HasIndex(d => d.SituacionMaxima).HasDatabaseName("IX_Deudores_SituacionMaxima");
            entity.HasIndex(d => d.SumaTotalPrestamos).HasDatabaseName("IX_Deudores_SumaTotalPrestamos");
        });

        modelBuilder.Entity<Entidad>(entity =>
        {
            entity.HasKey(e => e.CodigoEntidad);
            entity.Property(e => e.CodigoEntidad).HasMaxLength(5);
            entity.Property(e => e.SumaTotalPrestamos).HasColumnType("numeric(18,2)");
        });
    }
}
