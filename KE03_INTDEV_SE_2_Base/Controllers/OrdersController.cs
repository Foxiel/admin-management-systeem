using Microsoft.AspNetCore.Mvc;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using System;
using System.Threading.Tasks;

namespace KE03_INTDEV_SE_2_Base.Controllers
{
    public class OrdersController : Controller
    {
        private readonly OrderRepository _repository;

        public OrdersController(OrderRepository repository)
        {
            _repository = repository;
        }

        // GET: ORDERS
        public async Task<IActionResult> Index(
            string? q,
            string? klantNaam,
            string? klantEmail,
            string? klantTelefoon,
            DateTime? bestelDatum,
            string? bestelStatus)
        {
            // If a single search box 'q' is used, search it against name/email/phone
            if (!string.IsNullOrWhiteSpace(q))
            {
                klantNaam = klantNaam ?? q;
                klantEmail = klantEmail ?? q;
                klantTelefoon = klantTelefoon ?? q;
            }

            var list = await _repository.GetFilteredAsync(klantNaam, klantEmail, klantTelefoon, bestelDatum, bestelStatus);
            return View("~/Views/Order/Index.cshtml", list);
        }

        // GET: ORDERS/Details/5    
        public async Task<IActionResult> Details(int? orderid)
        {
            if (orderid == null) return NotFound();

            var order = await _repository.GetByIdAsync(orderid.Value);
            if (order == null) return NotFound();

            return View("~/Views/Order/Details.cshtml", order);
        }

        // GET: ORDERS/Create
        public IActionResult Create()
        {
            return View("~/Views/Order/Create.cshtml");
        }

        // POST: ORDERS/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Klant,Producten,BestelDatum,BestelStatus")] Order order)
        {
            try
            {
                await _repository.AddAsync(order);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here using your preferred logging framework
                ModelState.AddModelError("", "Er is een fout opgetreden tijdens het aanmaken van de bestelling. Probeer het alstublieft later opnieuw.");
                return View("~/Views/Order/Create.cshtml", order);
            }
        }

        // GET: ORDERS/Edit/5
        public async Task<IActionResult> Edit(int? orderid)
        {
            if (orderid == null) return NotFound();

            var order = await _repository.GetByIdAsync(orderid.Value);
            if (order == null) return NotFound();

            return View("~/Views/Order/Edit.cshtml", order);
        }

        // POST: ORDERS/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? orderid, [Bind("Id,Klant,Producten,BestelDatum,BestelStatus")] Order order)
        {
            if (orderid != order.Id) return NotFound();

            try
            {
                await _repository.UpdateAsync(order);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here using your preferred logging framework
                ModelState.AddModelError("", "Er is een fout opgetreden tijdens het bijwerken van de bestelling. Probeer het alstublieft later opnieuw.");
                return View("~/Views/Order/Edit.cshtml", order);
            }
        }

        // GET: ORDERS/Delete/5
        public async Task<IActionResult> Delete(int? orderid)
        {
            if (orderid == null) return NotFound();

            var order = await _repository.GetByIdAsync(orderid.Value);
            if (order == null) return NotFound();

            return View("~/Views/Order/Delete.cshtml", order);
        }

        // POST: ORDERS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? orderid)
        {
            if (orderid == null) return NotFound();
            await _repository.DeleteAsync(orderid.Value);
            return RedirectToAction(nameof(Index));
        }
    }
}