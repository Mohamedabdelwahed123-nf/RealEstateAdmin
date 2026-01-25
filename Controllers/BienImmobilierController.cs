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
            
            var biens = _context.Biens.AsQueryable();

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

        // GET: BienImmobilier/Details/5
        public async Task<IActionResult> Details(int? id)
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
        public async Task<IActionResult> Create([Bind("Id,Titre,Description,Prix,Adresse,Surface,NombrePieces,ImageUrl")] BienImmobilier bienImmobilier)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    bienImmobilier.UserId = currentUser.Id;
                }
                _context.Add(bienImmobilier);
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,Prix,Adresse,Surface,NombrePieces,ImageUrl")] BienImmobilier bienImmobilier)
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
                    // Préserver le UserId existant
                    bienImmobilier.UserId = existingBien.UserId;
                    _context.Update(bienImmobilier);
                    await _context.SaveChangesAsync();
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
            return RedirectToAction(nameof(Index));
        }

        private bool BienImmobilierExists(int id)
        {
            return _context.Biens.Any(e => e.Id == id);
        }
    }
}

