using Microsoft.EntityFrameworkCore;
using AH.Api.Models;

namespace AH.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Pozo> Pozos => Set<Pozo>();

    public DbSet<Inversion> Inversiones => Set<Inversion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Inversion>()
            .HasOne(i => i.Pozo)
            .WithMany()
            .HasForeignKey(i => i.PozoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inversion>()
            .HasOne(i => i.Usuario)
            .WithMany()
            .HasForeignKey(i => i.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
