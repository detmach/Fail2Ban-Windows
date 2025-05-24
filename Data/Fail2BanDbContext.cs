using Microsoft.EntityFrameworkCore;
using Fail2Ban.Models;

namespace Fail2Ban.Data;

/// <summary>
/// Fail2Ban veritabanı context'i
/// </summary>
public class Fail2BanDbContext : DbContext
{
    public Fail2BanDbContext(DbContextOptions<Fail2BanDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Ban kayıtları tablosu
    /// </summary>
    public DbSet<BanKaydi> BanKayitlari { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BanKaydi tablosu konfigürasyonu
        modelBuilder.Entity<BanKaydi>(entity =>
        {
            entity.ToTable("BanKayitlari");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.IpAdresi)
                .IsRequired()
                .HasMaxLength(45);
                
            entity.Property(e => e.KuralAdi)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Notlar)
                .HasMaxLength(500);

            // Index'ler
            entity.HasIndex(e => e.IpAdresi)
                .HasDatabaseName("IX_BanKayitlari_IpAdresi");
                
            entity.HasIndex(e => e.Aktif)
                .HasDatabaseName("IX_BanKayitlari_Aktif");
                
            entity.HasIndex(e => new { e.IpAdresi, e.Aktif })
                .HasDatabaseName("IX_BanKayitlari_IpAdresi_Aktif");
                
            entity.HasIndex(e => e.SilmeZamani)
                .HasDatabaseName("IX_BanKayitlari_SilmeZamani");
        });
    }
} 