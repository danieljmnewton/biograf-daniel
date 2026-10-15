using Biograf.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Biograf.Api.Data;

/// <summary>
/// Databaskontexten. Ärver från IdentityDbContext så att användare och roller
/// ligger i samma databas som filmer och bokningar.
/// </summary>
public class BiografDbContext(DbContextOptions<BiografDbContext> options)
    : IdentityDbContext<Anvandare>(options)
{
    public DbSet<Film> Filmer => Set<Film>();
    public DbSet<Salong> Salonger => Set<Salong>();
    public DbSet<Sate> Saten => Set<Sate>();
    public DbSet<Visning> Visningar => Set<Visning>();
    public DbSet<Bokning> Bokningar => Set<Bokning>();
    public DbSet<BokadPlats> BokadePlatser => Set<BokadPlats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Film>(e =>
        {
            e.Property(f => f.Titel).HasMaxLength(200).IsRequired();
            e.Property(f => f.Genre).HasMaxLength(50).IsRequired();
            e.Property(f => f.Beskrivning).HasMaxLength(2000);
            e.HasIndex(f => f.Titel);
        });

        modelBuilder.Entity<Salong>(e =>
        {
            e.Property(s => s.Namn).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.Namn).IsUnique();
        });

        modelBuilder.Entity<Sate>(e =>
        {
            e.HasOne(s => s.Salong)
                .WithMany(s => s.Saten)
                .HasForeignKey(s => s.SalongId)
                .OnDelete(DeleteBehavior.Cascade);

            // Samma rad + nummer får bara finnas en gång per salong.
            e.HasIndex(s => new { s.SalongId, s.Rad, s.Nummer }).IsUnique();
            e.Property(s => s.Typ).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Visning>(e =>
        {
            e.HasOne(v => v.Film)
                .WithMany(f => f.Visningar)
                .HasForeignKey(v => v.FilmId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.Salong)
                .WithMany(s => s.Visningar)
                .HasForeignKey(v => v.SalongId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(v => v.Starttid);
        });

        modelBuilder.Entity<Bokning>(e =>
        {
            e.Property(b => b.Bokningsnummer).HasMaxLength(20).IsRequired();
            e.HasIndex(b => b.Bokningsnummer).IsUnique();
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(b => b.Visning)
                .WithMany(v => v.Bokningar)
                .HasForeignKey(b => b.VisningId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Kund)
                .WithMany(a => a.Bokningar)
                .HasForeignKey(b => b.KundId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BokadPlats>(e =>
        {
            e.HasOne(p => p.Bokning)
                .WithMany(b => b.Platser)
                .HasForeignKey(p => p.BokningId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(p => p.Biljettyp).HasConversion<string>().HasMaxLength(20);

            e.HasOne(p => p.Sate)
                .WithMany()
                .HasForeignKey(p => p.SateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ett säte kan bara vara aktivt bokat en gång per visning.
            // Avbokade platser (Aktiv = 0) räknas inte, så sätet kan bokas igen.
            e.HasIndex(p => new { p.VisningId, p.SateId })
                .IsUnique()
                .HasFilter("\"Aktiv\" = 1");
        });
    }
}
