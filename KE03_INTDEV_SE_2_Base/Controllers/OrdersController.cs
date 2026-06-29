using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KE03_INTDEV_SE_2_Base.Controllers
{
    public class OrdersController : Controller
    {
        private readonly OrderRepository _repository;

        public OrdersController(OrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(
            string? q,
            string? klantNaam,
            string? klantEmail,
            string? klantTelefoon,
            DateTime? bestelDatum,
            string? bestelStatus)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                klantNaam ??= q;
                klantEmail ??= q;
                klantTelefoon ??= q;
            }

            var list = await _repository.GetFilteredAsync(
                klantNaam,
                klantEmail,
                klantTelefoon,
                bestelDatum,
                bestelStatus);

            return View("~/Views/Order/Index.cshtml", list);
        }

        public async Task<IActionResult> Details(int? orderid)
        {
            if (orderid == null)
                return NotFound();

            var order = await _repository.GetByIdAsync(orderid.Value);

            if (order == null)
                return NotFound();

            return View("~/Views/Order/Details.cshtml", order);
        }

        public IActionResult Create()
        {
            return View("~/Views/Order/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            await _repository.AddAsync(order);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? orderid)
        {
            if (orderid == null)
                return NotFound();

            var order = await _repository.GetByIdAsync(orderid.Value);

            if (order == null)
                return NotFound();

            return View("~/Views/Order/Edit.cshtml", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int orderid, Order order)
        {
            if (orderid != order.Id)
                return NotFound();

            await _repository.UpdateAsync(order);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? orderid)
        {
            if (orderid == null)
                return NotFound();

            var order = await _repository.GetByIdAsync(orderid.Value);

            if (order == null)
                return NotFound();

            return View("~/Views/Order/Delete.cshtml", order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int orderid)
        {
            await _repository.DeleteAsync(orderid);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPicking(int orderid)
        {
            await _repository.UpdateStatusAsync(orderid, "Bestelling wordt gepicked");
            return RedirectToAction(nameof(Details), new { orderid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishPicking(
            int orderid,
            List<int> pickedProductIds,
            string actie,
            string? reden)
        {
            if (actie == "klaar")
            {
                await _repository.UpdateStatusAsync(orderid, "Klaar voor verzending");
                return RedirectToAction(nameof(CreateShipment), new { orderid });
            }

            if (actie == "vertragen")
            {
                await _repository.UpdateStatusAsync(orderid, "Vertraagd");
            }

            if (actie == "verzenden-met-opmerking")
            {
                await _repository.UpdateStatusAsync(orderid, "Klaar voor verzending");
                return RedirectToAction(nameof(CreateShipment), new { orderid });
            }

            return RedirectToAction(nameof(Details), new { orderid });
        }

        public async Task<IActionResult> CreateShipment(int orderid)
        {
            var model = await _repository.GetShipmentModelAsync(orderid);

            if (model == null)
                return NotFound();

            return View("~/Views/Order/CreateShipment.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShipment(SendOrder model)
        {
            if (!ModelState.IsValid)
            {
                model.Bezorgers = await _repository.GetBezorgersAsync();

                return View("~/Views/Order/CreateShipment.cshtml", model);
            }

            await _repository.CreateShipmentAsync(model);

            return RedirectToAction(nameof(Details), new { orderid = model.BestellingId });
        }
    }
}