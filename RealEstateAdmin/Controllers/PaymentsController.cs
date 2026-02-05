using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Bien)
                .Include(s => s.Buyer)
                .Include(s => s.Seller)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                return NotFound();
            }

            var payments = await _context.Payments
                .Where(p => p.SaleId == saleId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Sale = sale;
            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Bien)
                .Include(s => s.Buyer)
                .Include(s => s.Seller)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                return NotFound();
            }

            ViewBag.Sale = sale;
            var payment = new Payment
            {
                SaleId = sale.Id,
                Amount = sale.Prix,
                Method = sale.PaymentMethod ?? "Espèces",
                Status = "En attente"
            };
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            var sale = await _context.Sales.FindAsync(payment.SaleId);
            if (sale == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Sale = sale;
                return View(payment);
            }

            payment.CreatedAt = DateTime.Now;
            if (payment.Status == "Payé" && payment.PaidAt == null)
            {
                payment.PaidAt = DateTime.Now;
            }

            _context.Payments.Add(payment);

            if (payment.Status == "Payé")
            {
                sale.PaymentStatus = "Payé";
                sale.PaidAt = payment.PaidAt;
                sale.PaymentMethod = payment.Method;
            }
            else if (payment.Status == "Annulé")
            {
                sale.PaymentStatus = "Annulé";
            }
            else
            {
                sale.PaymentStatus = "En attente";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { saleId = payment.SaleId });
        }
    }
}
