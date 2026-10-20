using Microsoft.EntityFrameworkCore;
using FelipeKadanos.models;

public class AppDataContext : DbContext {
    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

    public DbSet<Folha> Folhas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Folha>()
            .HasIndex(f => new { f.Cpf, f.Mes, f.Ano }).IsUnique();
    }
}
