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
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                
                // Relation avec ApplicationUser (optionnelle)
                // La relation est configurée sans contrainte stricte car ApplicationUser est dans un autre DbContext
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
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
            });
        }
    }
}

