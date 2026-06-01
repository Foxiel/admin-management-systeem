using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;

namespace KE03_INTDEV_SE_2_Base.Controllers
{
    public class CustomersController : Controller
    {
        private readonly CustomerRepository _repository;

        public CustomersController(CustomerRepository repository)
        {
            _repository = repository;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _repository.GetAllAsync();
            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _repository.GetByIdAsync(id.Value);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Telefoonnr")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _repository.AddAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _repository.GetByIdAsync(id.Value);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Telefoonnr")] Customer customer)
        {
            if (id != customer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _repository.UpdateAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _repository.GetByIdAsync(id.Value);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}