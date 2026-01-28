using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Shop
        public async Task<IActionResult> Index(string? titre, decimal? prixMin, decimal? prixMax, string? adresse)
        {
            var biens = _context.Biens
                .Include(b => b.User)
                .Include(b => b.Images)
                .Where(b => b.PublicationStatus == "Publié")
                .Where(b => string.IsNullOrEmpty(b.TypeTransaction) || b.TypeTransaction == "A Vendre" || b.TypeTransaction == "A Louer")
                .AsQueryable();

            // Filter by title
            if (!string.IsNullOrEmpty(titre))
            {
                biens = biens.Where(b => b.Titre.Contains(titre));
            }

            // Filter by address
            if (!string.IsNullOrEmpty(adresse))
            {
                biens = biens.Where(b => b.Adresse != null && b.Adresse.Contains(adresse));
            }

            // Filter by min price
            if (prixMin.HasValue)
            {
                biens = biens.Where(b => b.Prix >= prixMin.Value);
            }

            // Filter by max price
            if (prixMax.HasValue)
            {
                biens = biens.Where(b => b.Prix <= prixMax.Value);
            }

            ViewBag.Titre = titre;
            ViewBag.Adresse = adresse;
            ViewBag.PrixMin = prixMin;
            ViewBag.PrixMax = prixMax;

            return View(await biens.ToListAsync());
        }

        // POST: Shop/Buy/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int id)
        {
            var bien = await _context.Biens.FindAsync(id);
            if (bien == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (bien.UserId == currentUser.Id)
            {
                TempData["Error"] = "Vous possédez déjà ce bien.";
                return RedirectToAction(nameof(Index));
            }

            if (bien.PublicationStatus != "Publié")
            {
                TempData["Error"] = "Ce bien n'est pas publié.";
                return RedirectToAction(nameof(Index));
            }

            if (bien.TypeTransaction == "Acheté")
            {
                TempData["Error"] = "Ce bien n'est plus en vente.";
                return RedirectToAction(nameof(Index));
            }

            // Transfer ownership
            bien.UserId = currentUser.Id;
            bien.TypeTransaction = "Acheté";
            
            _context.Update(bien);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser.Id,
                Action = "Buy",
                EntityType = "BienImmobilier",
                EntityId = bien.Id,
                Details = $"Achat du bien '{bien.Titre}'",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Félicitations ! Vous avez acheté ce bien. Il est maintenant dans votre liste 'Mes Biens'.";
            return RedirectToAction(nameof(Index));
        }
    }
}
