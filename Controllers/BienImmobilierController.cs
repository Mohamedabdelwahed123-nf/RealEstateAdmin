using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    [Authorize]
    public class BienImmobilierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BienImmobilierController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: BienImmobilier
        public async Task<IActionResult> Index(string? titre, decimal? prixMin, decimal? prixMax, int? surfaceMin)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            
            var biens = _context.Biens.Include(b => b.User).Include(b => b.Images).AsQueryable();

            // Filtrer par utilisateur si ce n'est pas un admin
            if (!isAdmin && currentUser != null)
            {
                biens = biens.Where(b => b.UserId == currentUser.Id);
            }

            // Filtrage par titre
            if (!string.IsNullOrEmpty(titre))
            {
                biens = biens.Where(b => b.Titre.Contains(titre));
            }

            // Filtrage par prix minimum
            if (prixMin.HasValue)
            {
                biens = biens.Where(b => b.Prix >= prixMin.Value);
            }

            // Filtrage par prix maximum
            if (prixMax.HasValue)
            {
                biens = biens.Where(b => b.Prix <= prixMax.Value);
            }

            // Filtrage par surface minimum
            if (surfaceMin.HasValue)
            {
                biens = biens.Where(b => b.Surface.HasValue && b.Surface >= surfaceMin.Value);
            }

            ViewBag.Titre = titre;
            ViewBag.PrixMin = prixMin;
            ViewBag.PrixMax = prixMax;
            ViewBag.SurfaceMin = surfaceMin;

            return View(await biens.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportCsv()
        {
            var biens = await _context.Biens.Include(b => b.User).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id;Titre;Prix;Adresse;Surface;Pieces;Statut;Publication;Proprietaire");
            foreach (var b in biens)
            {
                sb.AppendLine($"{b.Id};\"{b.Titre}\";{b.Prix};\"{b.Adresse}\";{b.Surface};{b.NombrePieces};{b.TypeTransaction};{b.PublicationStatus};\"{b.User?.UserName}\"");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "biens.csv");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportPdf()
        {
            var biens = await _context.Biens.Include(b => b.User).ToListAsync();
            return View(biens);
        }

        // GET: BienImmobilier/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bienImmobilier = await _context.Biens
                .Include(b => b.Images)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            return View(bienImmobilier);
        }

        // GET: BienImmobilier/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BienImmobilier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,Description,Prix,Adresse,Surface,NombrePieces,ImageUrl,IsPublished,PublicationStatus,ImageUrlsInput")] BienImmobilier bienImmobilier)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Vous devez être connecté pour créer un bien immobilier.");
                    return View(bienImmobilier);
                }
                
                // Vérifier que l'utilisateur existe bien dans la base de données
                // Use _context.Users directly to ensure it's tracked by the context we are saving to
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id);
                if (dbUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Erreur: Utilisateur introuvable dans la base de données.");
                    return View(bienImmobilier);
                }
                
                bienImmobilier.UserId = dbUser.Id;
                if (!User.IsInRole("Admin"))
                {
                    bienImmobilier.IsPublished = false;
                    bienImmobilier.PublicationStatus = "En attente";
                }
                if (string.IsNullOrWhiteSpace(bienImmobilier.TypeTransaction))
                {
                    bienImmobilier.TypeTransaction = "A Vendre";
                }
                if (string.Equals(bienImmobilier.PublicationStatus, "Publié", System.StringComparison.OrdinalIgnoreCase))
                {
                    bienImmobilier.IsPublished = true;
                }
                else
                {
                    bienImmobilier.IsPublished = false;
                }

                _context.Add(bienImmobilier);
                await _context.SaveChangesAsync();
                await SaveImagesAsync(bienImmobilier);
                await LogActionAsync("Create", "BienImmobilier", bienImmobilier.Id, $"Création du bien '{bienImmobilier.Titre}'");
                return RedirectToAction(nameof(Index));
            }
            return View(bienImmobilier);
        }

        // GET: BienImmobilier/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bienImmobilier = await _context.Biens.FindAsync(id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            // Vérifier les permissions : admin ou propriétaire
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && (currentUser == null || bienImmobilier.UserId != currentUser.Id))
            {
                return Forbid();
            }

            return View(bienImmobilier);
        }

        // POST: BienImmobilier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,Prix,Adresse,Surface,NombrePieces,ImageUrl,TypeTransaction,IsPublished,PublicationStatus,ImageUrlsInput")] BienImmobilier bienImmobilier)
        {
            if (id != bienImmobilier.Id)
            {
                return NotFound();
            }

            // Vérifier les permissions
            var existingBien = await _context.Biens.FindAsync(id);
            if (existingBien == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && (currentUser == null || existingBien.UserId != currentUser.Id))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var normalizedStatus = bienImmobilier.TypeTransaction?.Trim();
                    if (string.IsNullOrWhiteSpace(normalizedStatus))
                    {
                        normalizedStatus = existingBien.TypeTransaction ?? "A Vendre";
                    }
                    if (string.Equals(normalizedStatus, "Achete", System.StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedStatus = "Acheté";
                    }
                    if (!string.Equals(normalizedStatus, "A Vendre", System.StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(normalizedStatus, "Acheté", System.StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedStatus = "A Vendre";
                    }

                    // Préserver le UserId existant
                    bienImmobilier.UserId = existingBien.UserId;
                    bienImmobilier.TypeTransaction = normalizedStatus;
                    if (!isAdmin)
                    {
                        bienImmobilier.IsPublished = existingBien.IsPublished;
                        bienImmobilier.PublicationStatus = existingBien.PublicationStatus;
                    }
                    else
                    {
                        bienImmobilier.IsPublished = string.Equals(bienImmobilier.PublicationStatus, "Publié", System.StringComparison.OrdinalIgnoreCase);
                    }
                    _context.Update(bienImmobilier);
                    await _context.SaveChangesAsync();
                    await SaveImagesAsync(bienImmobilier, replace: true);
                    await LogActionAsync("Edit", "BienImmobilier", bienImmobilier.Id, $"Modification du bien '{bienImmobilier.Titre}'");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BienImmobilierExists(bienImmobilier.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(bienImmobilier);
        }

        // GET: BienImmobilier/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bienImmobilier = await _context.Biens
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            // Vérifier les permissions
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && (currentUser == null || bienImmobilier.UserId != currentUser.Id))
            {
                return Forbid();
            }

            return View(bienImmobilier);
        }

        // POST: BienImmobilier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bienImmobilier = await _context.Biens.FindAsync(id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            // Vérifier les permissions
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && (currentUser == null || bienImmobilier.UserId != currentUser.Id))
            {
                return Forbid();
            }

            _context.Biens.Remove(bienImmobilier);
            await _context.SaveChangesAsync();
            await LogActionAsync("Delete", "BienImmobilier", bienImmobilier.Id, $"Suppression du bien '{bienImmobilier.Titre}'");
            return RedirectToAction(nameof(Index));
        }

        // POST: BienImmobilier/MettreEnVente/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MettreEnVente(int id)
        {
            var bienImmobilier = await _context.Biens.FindAsync(id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && (currentUser == null || bienImmobilier.UserId != currentUser.Id))
            {
                return Forbid();
            }

            bienImmobilier.TypeTransaction = "A Vendre";
            _context.Update(bienImmobilier);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Le bien a été remis en vente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: BienImmobilier/TogglePublish/5 (Admin seulement)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var bienImmobilier = await _context.Biens.FindAsync(id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            bienImmobilier.IsPublished = !bienImmobilier.IsPublished;
            bienImmobilier.PublicationStatus = bienImmobilier.IsPublished ? "Publié" : "Refusé";
            _context.Update(bienImmobilier);
            await _context.SaveChangesAsync();

            var status = bienImmobilier.IsPublished ? "Publié" : "Dépublié";
            await LogActionAsync("Publish", "BienImmobilier", bienImmobilier.Id, $"{status} : '{bienImmobilier.Titre}'");

            return RedirectToAction(nameof(Index));
        }

        // POST: BienImmobilier/SetStatus/5 (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var bienImmobilier = await _context.Biens.FindAsync(id);
            if (bienImmobilier == null)
            {
                return NotFound();
            }

            if (status != "En attente" && status != "Publié" && status != "Refusé")
            {
                return BadRequest();
            }

            bienImmobilier.PublicationStatus = status;
            bienImmobilier.IsPublished = status == "Publié";
            _context.Update(bienImmobilier);
            await _context.SaveChangesAsync();
            await LogActionAsync("Status", "BienImmobilier", bienImmobilier.Id, $"Statut: {status} - '{bienImmobilier.Titre}'");

            return RedirectToAction(nameof(Index));
        }

        private bool BienImmobilierExists(int id)
        {
            return _context.Biens.Any(e => e.Id == id);
        }

        private async Task LogActionAsync(string action, string entityType, int entityId, string details)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var log = new AuditLog
            {
                UserId = currentUser?.Id,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CreatedAt = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task SaveImagesAsync(BienImmobilier bienImmobilier, bool replace = false)
        {
            var urls = ParseImageUrls(bienImmobilier.ImageUrlsInput);
            if (urls.Count == 0)
            {
                return;
            }

            if (replace)
            {
                var existing = _context.BienImages.Where(i => i.BienImmobilierId == bienImmobilier.Id);
                _context.BienImages.RemoveRange(existing);
                await _context.SaveChangesAsync();
            }

            foreach (var url in urls)
            {
                _context.BienImages.Add(new BienImage
                {
                    BienImmobilierId = bienImmobilier.Id,
                    Url = url
                });
            }

            if (string.IsNullOrWhiteSpace(bienImmobilier.ImageUrl))
            {
                bienImmobilier.ImageUrl = urls.FirstOrDefault();
                _context.Update(bienImmobilier);
            }

            await _context.SaveChangesAsync();
        }

        private static List<string> ParseImageUrls(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            return input
                .Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
        }
    }
}

