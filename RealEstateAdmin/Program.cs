using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la chaîne de connexion MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configuration du DbContext pour les données de l'application
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore)));

// Configuration d'Identity avec MySQL
builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore)));

// Configuration d'Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Configuration des options de mot de passe
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Configuration des options utilisateur
    options.User.RequireUniqueEmail = true;
    
    // Configuration de la connexion
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationIdentityDbContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Création des rôles, utilisateur admin et seeding des données au démarrage
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Création des rôles
    var roles = new[] { "SuperAdmin", "Admin", "Utilisateur" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    
    // Création de l'utilisateur admin
    var adminEmail = "user@admin.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Nom = "Administrateur"
        };
        var result = await userManager.CreateAsync(adminUser, "Admin123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }
    }
    else
    {
        var adminRoles = await userManager.GetRolesAsync(adminUser);
        if (!adminRoles.Contains("SuperAdmin"))
        {
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }
    }
    
    // Seeding des biens immobiliers
    if (!dbContext.Biens.Any())
    {
        // Récupérer l'ID de l'admin pour assigner les biens (réutiliser la variable existante)
        string? adminUserId = adminUser?.Id;
        
        var biens = new List<BienImmobilier>
        {
            new BienImmobilier
            {
                Titre = "Villa moderne avec piscine",
                Description = "Magnifique villa contemporaine de 250m² avec piscine privée, jardin paysager et vue panoramique. Située dans un quartier résidentiel calme, cette propriété offre un espace de vie exceptionnel avec 5 chambres, 3 salles de bain, cuisine équipée et salon spacieux.",
                Prix = 850000,
                Adresse = "Avenue Habib Bourguiba, La Marsa, Tunis",
                Surface = 250,
                NombrePieces = 5,
                ImageUrl = "https://images.unsplash.com/photo-1613977257363-707ba9348227?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Appartement luxueux centre-ville",
                Description = "Superbe appartement de standing au cœur de la ville, entièrement rénové. Grandes baies vitrées, balcon avec vue sur la mer, parking privé. Idéal pour investissement locatif ou résidence principale.",
                Prix = 420000,
                Adresse = "Avenue de la République, Sfax",
                Surface = 95,
                NombrePieces = 3,
                ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Maison de campagne avec terrain",
                Description = "Charmante maison de campagne sur terrain de 2000m². Maison rénovée avec charme, 4 chambres, 2 salles de bain, grande cuisine, salon avec cheminée. Parfait pour une famille recherchant le calme et l'espace.",
                Prix = 320000,
                Adresse = "Route de Zaghouan, Bizerte",
                Surface = 180,
                NombrePieces = 4,
                ImageUrl = "https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Studio moderne proche plage",
                Description = "Studio récent et lumineux à 200m de la plage. Idéal pour investissement locatif saisonnier ou résidence secondaire. Meublé, climatisé, avec balcon et parking.",
                Prix = 185000,
                Adresse = "Corniche de Sousse, Sousse",
                Surface = 35,
                NombrePieces = 1,
                ImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Penthouse avec terrasse panoramique",
                Description = "Exceptionnel penthouse au dernier étage avec terrasse de 80m² offrant une vue à 360° sur la ville et la mer. 3 chambres, 2 salles de bain, cuisine ouverte design, salon double hauteur. Parking et cave inclus.",
                Prix = 1200000,
                Adresse = "Avenue Habib Bourguiba, Hammamet",
                Surface = 180,
                NombrePieces = 3,
                ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Duplex rénové avec jardin",
                Description = "Magnifique duplex entièrement rénové avec jardin privatif. Rez-de-chaussée avec cuisine ouverte et salon, étage avec 3 chambres et 2 salles de bain. Proche commerces et transports.",
                Prix = 550000,
                Adresse = "Rue de la Kasbah, Tunis",
                Surface = 140,
                NombrePieces = 3,
                ImageUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Appartement T3 avec balcon",
                Description = "Bel appartement de 75m² avec balcon, dans immeuble récent. 2 chambres, 1 salle de bain, cuisine équipée, salon lumineux. Proche métro et commerces. Idéal premier achat ou investissement.",
                Prix = 280000,
                Adresse = "Avenue Mohamed V, Monastir",
                Surface = 75,
                NombrePieces = 2,
                ImageUrl = "https://images.unsplash.com/photo-1600607687644-c7171b42498f?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            },
            new BienImmobilier
            {
                Titre = "Maison familiale avec garage",
                Description = "Spacieuse maison de 200m² sur terrain de 500m². 5 chambres, 3 salles de bain, cuisine aménagée, salon, bureau, garage double. Quartier résidentiel calme, proche écoles.",
                Prix = 650000,
                Adresse = "Cité Ennasr, Ariana, Tunis",
                Surface = 200,
                NombrePieces = 5,
                ImageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800&h=600&fit=crop",
                UserId = adminUserId,
                PublicationStatus = "Publié",
                IsPublished = true
            }
        };
        
        dbContext.Biens.AddRange(biens);
        await dbContext.SaveChangesAsync();
    }
    

}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
