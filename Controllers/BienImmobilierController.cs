using Microsoft.AspNetCore.Authorization;
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

        public BienImmobilierController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BienImmobilier
        public async Task<IActionResult> Index(string? titre, decimal? prixMin, decimal? prixMax, int? surfaceMin)
        {
            var biens = _context.Biens.AsQueryable();

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
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: BienImmobilier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Titre,Description,Prix,Adresse,Surface,NombrePieces")] BienImmobilier bienImmobilier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bienImmobilier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bienImmobilier);
        }

        // GET: BienImmobilier/Edit/5
        [Authorize(Roles = "Admin")]
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
            return View(bienImmobilier);
        }

        // POST: BienImmobilier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,Prix,Adresse,Surface,NombrePieces")] BienImmobilier bienImmobilier)
        {
            if (id != bienImmobilier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
        [Authorize(Roles = "Admin")]
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

            return View(bienImmobilier);
        }

        // POST: BienImmobilier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bienImmobilier = await _context.Biens.FindAsync(id);
            if (bienImmobilier != null)
            {
                _context.Biens.Remove(bienImmobilier);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BienImmobilierExists(int id)
        {
            return _context.Biens.Any(e => e.Id == id);
        }
    }
}

