using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KE03_INTDEV_SE_2_Base.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductsRepository _repository;

        public ProductsController(ProductsRepository repository)
        {
            _repository = repository;
        }

        // GET: Customers
        //public async Task<IActionResult> Index()
        //{
        //    var products = await _repository.GetAllAsync();
        //    return View(products);
        //}

        // GET: Customers/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var product = await _repository.GetByIdAsync(id.Value);
        //    if (product == null) return NotFound();

        //    return View(product);
        //}

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("Id,Name,Email,Telefoonnr")] Product product)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        await _repository.AddAsync(product);
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(product);
        //}

        // GET: Customers/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var product = await _repository.GetByIdAsync(id.Value);
        //    if (product  == null) return NotFound();

        //    return View(product);
        //}

        // POST: Customers/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Telefoonnr")] Product product)
        //{
        //    if (id != product.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        await _repository.UpdateAsync(product);
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(product);
        //}

        //// GET: Customers/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var product = await _repository.GetByIdAsync(id.Value);
        //    if (product == null) return NotFound();

        //    return View(product );
        //}

        //// POST: Customers/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    await _repository.DeleteAsync(id);
        //    return RedirectToAction(nameof(Index));
        //}


    }
}
