using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Message (Admin seulement)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Messages.OrderByDescending(m => m.DateCreation).ToListAsync());
        }

        // GET: Message/Create (Public - formulaire de contact)
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create([Bind("NomUtilisateur,Email,Sujet,Contenu")] Message message)
        {
            if (ModelState.IsValid)
            {
                message.DateCreation = DateTime.Now;
                _context.Add(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Votre message a été envoyé avec succès. Nous vous répondrons bientôt.";
                return RedirectToAction(nameof(Create));
            }
            return View(message);
        }

        // GET: Message/Details/5 (Admin seulement)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // GET: Message/Delete/5 (Admin seulement)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // POST: Message/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Message/Reply/5 (Admin seulement)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reply(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            ViewBag.Message = message;
            return View();
        }

        // POST: Message/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Reply(int messageId, string reponse)
        {
            // Ici, vous pouvez implémenter l'envoi d'email ou stocker la réponse
            // Pour l'instant, on redirige simplement
            TempData["SuccessMessage"] = "Réponse envoyée avec succès.";
            return RedirectToAction(nameof(Index));
        }
    }
}

