using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace KE03_INTDEV_SE_2_Base.Controllers
{
    public class SendOrderController : Controller
    {
        private readonly SendOrderRepository _repository;

        public SendOrderController(SendOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _repository.GetOrdersToSendAsync();
            return View("~/Views/SendOrder/Index.cshtml", orders);
        }

        public async Task<IActionResult> Create(int id)
        {
            var model = await _repository.GetOrderForSendingAsync(id);

            if (model == null)
                return NotFound();

            return View("~/Views/SendOrder/Create.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SendOrder model)
        {
            if (!ModelState.IsValid)
            {
                model.Bezorgers = await _repository.GetBezorgersAsync();
                model.Statussen = await _repository.GetBezorgStatussenAsync();

                return View("~/Views/SendOrder/Create.cshtml", model);
            }

            try
            {
                await _repository.CreateShipmentAsync(model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.ToString());

                model.Bezorgers = await _repository.GetBezorgersAsync();
                model.Statussen = await _repository.GetBezorgStatussenAsync();

                return View("~/Views/SendOrder/Create.cshtml", model);
            }
        }
    }
}