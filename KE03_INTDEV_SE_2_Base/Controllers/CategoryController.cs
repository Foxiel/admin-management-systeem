// Gemaakt door Fabian

using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KE03_INTDEV_SE_2_Base.Controllers;

public class CategoryController : Controller
{
    private readonly CategoryRespository _repository;
    
    public CategoryController(CategoryRespository repository)
    {
        _repository = repository;
    }
    
    // GET: Category
    public async Task<IActionResult> Index()
    {
        var categories = await _repository.GetAllCategories();
        return View(categories);
    }
    
    // GET: Category/Create
    public IActionResult Create()
    {
        return View();
    }
    
    // POST: Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryId, CategoryName")] Category category)
    {
        if (ModelState.IsValid)
        {
            await _repository.AddCategory(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }
    
    // GET Category/Edit/C01
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null) return NotFound();
        
        var category = await _repository.GetCategoryById(id);
        if (category == null) return NotFound();
        
        return View(category);
    }
    
    // POST: Category/Edit/C01
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("CategoryId, CategoryName")] Category category)
    {
        if (id != category.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _repository.EditCategory(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }
    
    // GET: Category/Delete/C01
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null) return NotFound();

        var category = await _repository.GetCategoryById(id);
        if (category == null) return NotFound();

        return View(category);
    }
    
    // POST: Category/Delete/C01
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        await _repository.DeleteCategory(id);
        return RedirectToAction(nameof(Index));
    }
}