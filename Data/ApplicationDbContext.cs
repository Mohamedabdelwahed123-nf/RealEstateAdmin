using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BienImmobilier> Biens { get; set; }
        public DbSet<BienImage> BienImages { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        // Add Users DbSet to allow FK relationships
        public DbSet<ApplicationUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration pour ApplicationUser (minimal config for FK)
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AspNetUsers"); // Ensure it maps to the correct Identity table
            });

            // Configuration pour BienImmobilier
            modelBuilder.Entity<BienImmobilier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Prix).IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(1000);
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.IsPublished).HasDefaultValue(true);
                entity.Property(e => e.PublicationStatus).HasMaxLength(50).HasDefaultValue("En attente");
                
                // Relation avec ApplicationUser
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            modelBuilder.Entity<BienImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Url).HasMaxLength(1000);
                entity.HasOne(e => e.BienImmobilier)
                    .WithMany(b => b.Images)
                    .HasForeignKey(e => e.BienImmobilierId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration pour Utilisateur
            modelBuilder.Entity<Utilisateur>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MotDePasse).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configuration pour Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomUtilisateur).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Sujet).HasMaxLength(200);
                entity.Property(e => e.Contenu).HasMaxLength(5000);
                entity.Property(e => e.DateCreation).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.Statut).HasMaxLength(50).HasDefaultValue("Nouveau");
                entity.Property(e => e.TraiteParId).HasMaxLength(450);
            });

            // Configuration pour AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.Action).HasMaxLength(200);
                entity.Property(e => e.EntityType).HasMaxLength(200);
                entity.Property(e => e.Details).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });
        }
    }
}

