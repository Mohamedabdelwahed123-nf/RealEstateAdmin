using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Message (Admin seulement)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Messages
                .OrderBy(m => m.Statut == "Nouveau" ? 0 : 1)
                .ThenByDescending(m => m.DateCreation)
                .ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportCsv()
        {
            var messages = await _context.Messages.OrderByDescending(m => m.DateCreation).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id;Nom;Email;Sujet;Statut;Date");
            foreach (var m in messages)
            {
                sb.AppendLine($"{m.Id};\"{m.NomUtilisateur}\";\"{m.Email}\";\"{m.Sujet}\";{m.Statut};{m.DateCreation:dd/MM/yyyy HH:mm}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "messages.csv");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportPdf()
        {
            var messages = await _context.Messages.OrderByDescending(m => m.DateCreation).ToListAsync();
            return View(messages);
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
                message.Statut = "Nouveau";
                _context.Add(message);
                await _context.SaveChangesAsync();
                await LogActionAsync("Create", "Message", message.Id, $"Nouveau message: {message.Sujet}");
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
            if (message != null)
            {
                await LogActionAsync("Delete", "Message", message.Id, $"Suppression du message: {message.Sujet}");
            }
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
        public async Task<IActionResult> Reply(int messageId, string reponse)
        {
            // Ici, vous pouvez implémenter l'envoi d'email ou stocker la réponse
            // Pour l'instant, on redirige simplement
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.Statut = "Traité";
                message.DateTraitement = DateTime.Now;
                var currentUser = await _userManager.GetUserAsync(User);
                message.TraiteParId = currentUser?.Id;
                _context.Update(message);
                await _context.SaveChangesAsync();
                await LogActionAsync("Reply", "Message", message.Id, $"Réponse envoyée: {message.Sujet}");
            }
            TempData["SuccessMessage"] = "Réponse envoyée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Message/MarkTreated/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkTreated(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            message.Statut = "Traité";
            message.DateTraitement = DateTime.Now;
            var currentUser = await _userManager.GetUserAsync(User);
            message.TraiteParId = currentUser?.Id;

            _context.Update(message);
            await _context.SaveChangesAsync();
            await LogActionAsync("Update", "Message", message.Id, $"Message traité: {message.Sujet}");

            return RedirectToAction(nameof(Index));
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
    }
}

