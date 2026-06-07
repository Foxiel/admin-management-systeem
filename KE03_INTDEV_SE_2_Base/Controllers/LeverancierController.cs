
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;


public class LeverancierController : Controller
{
    private readonly LeverancierRepository _repository;

    public LeverancierController(LeverancierRepository repository)
    {
        _repository = repository;
    }

    // GET: Leverancier
    public async Task<IActionResult> Index(string searchString)
    {
        var leveranciers = await _repository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            leveranciers = leveranciers.Where(l =>
                (l.Naam != null && l.Naam.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        return View(leveranciers);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        var leveranciers = await _repository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(term))
        {
            leveranciers = leveranciers.Where(l =>
                (l.Naam != null && l.Naam.Contains(term, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        return Json(leveranciers);
    }

    //// GET: Leverancier/Details/5
    //public async Task<IActionResult> Details(int? id)
    //{
    //    if (id == null) return NotFound();

    //    var leverancier = await _repository.GetByIdAsync(id.Value);
    //    if (leverancier == null) return NotFound();

    //    return View(leverancier);
    //}

    // GET: Leverancier/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Leverancier/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Naam")] Leverancier leverancier)
    {
        if (ModelState.IsValid)
        {
            await _repository.AddAsync(leverancier);
            return RedirectToAction(nameof(Index));
        }

        return View(leverancier);
    }

    // GET: Leverancier/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var leverancier = await _repository.GetByIdAsync(id.Value);
        if (leverancier == null) return NotFound();

        return View(leverancier);
    }

    // POST: Leverancier/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Naam")] Leverancier leverancier)
    {
        if (id != leverancier.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _repository.UpdateAsync(leverancier);
            return RedirectToAction(nameof(Index));
        }

        return View(leverancier);
    }

    // GET: Leverancier/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var leverancier = await _repository.GetByIdAsync(id.Value);
        if (leverancier == null) return NotFound();

        return View(leverancier);
    }

    // POST: Leverancier/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _repository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}


